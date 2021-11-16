using System.Collections;
using System.IO.Compression;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ClickOnceGet.Server.Options;
using ClickOnceGet.Server.Services;
using ClickOnceGet.Shared.Models;
using ClickOnceGet.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ClickOnceGet.Server.Controllers;

[ApiController, AutoValidateAntiforgeryToken]
public class ApplicationsController : ControllerBase
{
    private IWebHostEnvironment WebHostEnv { get; }

    private IOptionsMonitor<ClickOnceGetOptions> Options { get; }

    private IClickOnceAppInfoProvider ClickOnceAppInfoProvider { get; }

    private IClickOnceFileRepository ClickOnceFileRepository { get; }

    private ClickOnceAppContentManager AppContentManager { get; }

    public ApplicationsController(
        IWebHostEnvironment webHostEnv,
        IOptionsMonitor<ClickOnceGetOptions> options,
        IClickOnceAppInfoProvider clickOnceAppInfoProvider,
        IClickOnceFileRepository clickOnceFileRepository,
        ClickOnceAppContentManager appContentManager)
    {
        this.WebHostEnv = webHostEnv;
        this.Options = options;
        this.ClickOnceAppInfoProvider = clickOnceAppInfoProvider;
        this.ClickOnceFileRepository = clickOnceFileRepository;
        this.AppContentManager = appContentManager;
    }

    // GET api/apps
    [HttpGet("api/apps")]
    public Task<IEnumerable<ClickOnceAppInfo>> GetAllAppsAsync()
    {
        return this.ClickOnceAppInfoProvider.GetAllAppsAsync();
    }

    // GET api/apps/appname
    [HttpGet("api/apps/{appName}")]
    public ActionResult<ClickOnceAppInfo> GetApp(string appName)
    {
        var appInfo = this.ClickOnceFileRepository.GetAppInfo(appName);
        if (appInfo == null) return this.NotFound($"The application \"{appName}\"not found.");
        return appInfo;
    }

    // GET api/myapps
    [Authorize, HttpGet("api/myapps")]
    public Task<IEnumerable<ClickOnceAppInfo>> GetOwnedAppsAsync()
    {
        return this.ClickOnceAppInfoProvider.GetOwnedAppsAsync();
    }

    // GET api/myapps/{appName}
    [Authorize, HttpGet("api/myapps/{appName}")]
    public async Task<ActionResult<ClickOnceAppInfo>> GetOwnedAppAsync(string appName)
    {
        var appInfo = await this.ClickOnceAppInfoProvider.GetOwnedAppAsync(appName);
        if (appInfo == null) return this.NotFound($"The application \"{appName}\"not found.");
        return appInfo;
    }

    // DELETE api/apps/appname or api/myapps/appname
    [Authorize, HttpDelete("api/myapps/{appName}")]
    public ActionResult DeleteApp(string appName)
    {
        var userId = this.User.GetHashedUserId();
        if (userId == null) throw new Exception("hashed user id is null.");

        var appInfo = this.ClickOnceFileRepository.GetAppInfo(appName);
        if (appInfo == null) return this.NotFound($"The application \"{appName}\"not found.");
        if (appInfo.OwnerId != userId) return this.StatusCode(HttpStatusCode.Forbidden, $"You don't have ownership rights of the \"{appName}\" application.");

        this.ClickOnceFileRepository.DeleteApp(appName);
        return this.NoContent();
    }

    // POST api/myapps/zipedpackage
    [Authorize, HttpPost("api/myapps/zipedpackage")]
    public async Task<ActionResult> UploadAppAsync(IFormFile zipedPackage)
    {
        var userId = this.User.GetHashedUserId();
        if (userId == null) throw new Exception("hashed user id is null.");
        if (zipedPackage == null) return this.BadRequest();

        var success = false;
        var tmpPath = Path.Combine(this.WebHostEnv.ContentRootPath, "App_Data", $"{userId}-{Guid.NewGuid():N}.zip");
        try
        {
            using var fs = new FileStream(tmpPath, FileMode.CreateNew, FileAccess.ReadWrite);
            await zipedPackage.CopyToAsync(fs);

            fs.Seek(0, SeekOrigin.Begin);
            using var zip = new ZipArchive(fs);

            // Validate files structure that are included in a .zip file.
            var appFile = zip.Entries
                .Where(e => Path.GetExtension(e.FullName).ToLower() == ".application")
                .OrderBy(e => e.FullName.Length)
                .FirstOrDefault();
            if (appFile == null) return this.BadRequest("The .zip file you uploaded did not contain .application file.");
            if (Path.GetDirectoryName(appFile.FullName) != "") return this.BadRequest("The .zip file you uploaded contain .application file, but it was not in root of the .zip file.");

            // Validate app name does not conflict.
            var appName = Path.GetFileNameWithoutExtension(appFile.FullName);
            var hasOwnerRight = this.ClickOnceFileRepository.GetOwnerRight(userId, appName);
            if (!hasOwnerRight) return this.BadRequest($"Sorry, the application name \"{appName}\" was already registered by somebody else.");

            var appInfo = this.ClickOnceFileRepository.GetAppInfo(appName);
            if (appInfo == null)
            {
                appInfo = new ClickOnceAppInfo
                {
                    Name = appName,
                    OwnerId = userId
                };
            }
            appInfo.RegisteredAt = DateTime.UtcNow;

            // Check codebase in .application file
            if (this.Options.CurrentValue.SkipCodeBaseValidation == false)
            {
                foreach (var item in zip.Entries.Where(_ => Path.GetExtension(_.FullName).ToLower() == ".application"))
                {
                    using var reader = item.Open();
                    var error = this.CheckCodeBaseUrl(appName, reader);
                    if (error != null) return error;
                }
            }

            // Save to the repository.
            this.ClickOnceFileRepository.ClearUpFiles(appName);
            foreach (var item in zip.Entries.Where(_ => _.Name != ""))
            {
                var buff = new byte[item.Length];
                using var reader = item.Open();
                reader.Read(buff, 0, buff.Length);
                this.ClickOnceFileRepository.SaveFileContent(appName, item.FullName, buff);
            }

            // Update certificate information.
            await this.AppContentManager.UpdateCertificateInfoAsync(appInfo);

            this.ClickOnceFileRepository.SaveAppInfo(appName, appInfo);

            success = true;
            return this.Ok(appName);
        }
        catch (InvalidDataException)
        {
            return this.BadRequest("The file you uploaded looks like invalid Zip format.");
        }
        finally
        {
            // Sweep temporary file if success.
            if (success)
            {
                try { System.IO.File.Delete(tmpPath); }
                catch (Exception) { }
            }
        }
    }

    // PUT api/myapps/{appName}
    [Authorize, HttpPut("api/myapps/{appName}")]
    public async Task<ActionResult> PutAppAsync(
        string appName,
        [FromQuery] bool disclosePublisher,
        [FromBody] ClickOnceAppInfo appInfo)
    {
        var targerAppInfo = await this.ClickOnceAppInfoProvider.GetAppAsync(appName);
        if (targerAppInfo == null) return this.NotFound($"The application \"{appName}\"not found.");
        if (targerAppInfo.OwnerId != this.User.GetHashedUserId()) return this.StatusCode(HttpStatusCode.Forbidden, $"You don't have ownership rights of the \"{appName}\" application.");

        targerAppInfo.Title = appInfo.Title;
        targerAppInfo.Description = appInfo.Description;
        targerAppInfo.ProjectURL = appInfo.ProjectURL;
        this.SetupPublisherInformtion(disclosePublisher, targerAppInfo);

        await this.AppContentManager.UpdateSignedByPublisherAsync(targerAppInfo);

        this.ClickOnceFileRepository.SaveAppInfo(appName, targerAppInfo);

        return this.NoContent();
    }

    private void SetupPublisherInformtion(bool disclosePublisher, ClickOnceAppInfo appInfo)
    {
        if (disclosePublisher)
        {
            var gitHubUserName = this.User.Identity.Name;
            appInfo.PublisherName = gitHubUserName;
            appInfo.PublisherURL = "https://github.com/" + gitHubUserName;
            appInfo.PublisherAvatorImageURL = "https://avatars.githubusercontent.com/" + gitHubUserName;
        }
        else
        {
            appInfo.PublisherName = null;
            appInfo.PublisherURL = null;
            appInfo.PublisherAvatorImageURL = null;
        }
    }

    private ActionResult CheckCodeBaseUrl(string appName, Stream stream)
    {
        var appManifest = default(XDocument);
        try
        {
            appManifest = XDocument.Load(stream);
        }
        catch (XmlException)
        {
            return this.BadRequest("The .application file that contained in .zip file you uploaded is may not be valid XML format.");
        }

        var xnm = new XmlNamespaceManager(new NameTable());
        xnm.AddNamespace("asmv1", "urn:schemas-microsoft-com:asm.v1");
        xnm.AddNamespace("asmv2", "urn:schemas-microsoft-com:asm.v2");
        var codeBaseAttr = (appManifest.XPathEvaluate("/asmv1:assembly/asmv2:deployment/asmv2:deploymentProvider/@codebase", xnm) as IEnumerable).Cast<XAttribute>().FirstOrDefault();
        if (codeBaseAttr == null) return null; // No <deploymentProvider> node is also valid/success.

        var codebaseIsValidUrl = Uri.TryCreate(codeBaseAttr.Value, UriKind.Absolute, out var codeBaseUri);
        if (!codebaseIsValidUrl || (codeBaseUri.Scheme != "http" && codeBaseUri.Scheme != "https"))
            return this.BadRequest("The .application file that contained in .zip file you uploaded has invalid format codebase url as HTTP(s) protocol.");

        var appUrl = new Uri(this.Request.GetDisplayUrl()).AppUrl(forceSecure: true);
        var installUrl = appUrl + $"/app/{appName}";
        var expectedCodeBase = $"{installUrl}/{appName}.application";

        if (codeBaseUri.AbsoluteUri != expectedCodeBase)
            return this.BadRequest($"The install URL is invalid. You should re-publish the application with fix the install URL as \"{installUrl}\".");

        return null; // Valid/Success.
    }
}
