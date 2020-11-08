using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ClickOnceGet.Server.Services;
using ClickOnceGet.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Toolbelt.Web;

namespace ClickOnceGet.Server.Controllers
{
    [Authorize, ApiController]
    public class PublishController : Controller
    {
        private IWebHostEnvironment WebHostEnv { get; }

        public IClickOnceFileRepository ClickOnceFileRepository { get; }

        private CertificateValidater CertificateValidater { get; }

        private ClickOnceAppContentManager AppContentManager { get; }

        public PublishController(
            IWebHostEnvironment webHostEnv,
            IClickOnceFileRepository clickOnceFileRepository,
            CertificateValidater certificateValidater,
            ClickOnceAppContentManager appContentManager)
        {
            this.WebHostEnv = webHostEnv;
            this.ClickOnceFileRepository = clickOnceFileRepository;
            this.CertificateValidater = certificateValidater;
            this.AppContentManager = appContentManager;
        }

        // GET: /app/{appId}/{*pathInfo}
        [AllowAnonymous, HttpGet("/app/{appId}/{*pathInfo}")]
        public ActionResult Get(string appId, string pathInfo)
        {
            pathInfo = (pathInfo ?? "").Replace('/', '\\');
            if (pathInfo == "") return Redirect($"/app/{Uri.EscapeUriString(appId)}/{Uri.EscapeUriString(appId)}.application");

            var fileBytes = this.ClickOnceFileRepository.GetFileContent(appId, pathInfo);
            if (fileBytes == null) return NotFound();

            var ext = Path.GetExtension(pathInfo).ToLower();
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            var contentType = pathInfo == "" || ext == ".application" ?
                "application/x-ms-application" :
                contentTypeProvider.Mappings.TryGetValue(ext, out var type) ? type : "application/octet-stream";

            // Increment downloads counter.
            if (ext == ".deploy")
            {
                var commandPath = this.AppContentManager.GetEntryPointCommandPath(appId);
                if (pathInfo == commandPath)
                {
                    var appInfo = this.ClickOnceFileRepository.EnumAllApps().First(a => a.Name == appId);
                    appInfo.NumberOfDownloads++;
                    this.ClickOnceFileRepository.SaveAppInfo(appId, appInfo);
                }
            }

            return File(fileBytes, contentType);
        }

        // GET: /app/{appId}/icon/[{pxSize}]
        [AllowAnonymous, HttpGet("/app/{appId}/icon/{pxSize?}")]
        public ActionResult GetIcon(string appId, int pxSize = 48)
        {
            var appInfo = this.ClickOnceFileRepository.GetAppInfo(appId);
            if (appInfo == null) return NotFound();

            var etag = appInfo.RegisteredAt.Ticks.ToString() + "." + pxSize;
            return new CacheableContentResult(
                    cacheability: ResponseCacheLocation.Any,
                    lastModified: appInfo.RegisteredAt,
                    etag: etag,
                    contentType: "image/png",
                    getContent: () => this.AppContentManager.GetIcon(appId, pxSize) ?? NoImagePng()
                );
        }

        // GET: /app/{appId}/cert/{*pathInfo}
        [AllowAnonymous, HttpGet("/app/{appId}/cert")]
        public ActionResult GetCertificate(string appId)
        {
            var appInfo = this.ClickOnceFileRepository.GetAppInfo(appId);
            if (appInfo == null) return NotFound();
            var etag = appInfo.RegisteredAt.Ticks.ToString() + ".cer";

            return new CacheableContentResult(
                    cacheability: ResponseCacheLocation.Any,
                    lastModified: appInfo.RegisteredAt,
                    etag: etag,
                    contentType: "application/x-x509-ca-cert",
                    getContent: () => GetCertificateCore(appInfo).ConfigureAwait(false).GetAwaiter().GetResult()
                );
        }

        private async Task<byte[]> GetCertificateCore(ClickOnceAppInfo appInfo)
        {
            if (appInfo == null) return null;

            if (appInfo.HasCodeSigning == null)
            {
                var certBin = await this.AppContentManager.UpdateCertificateInfoAsync(appInfo);
                this.ClickOnceFileRepository.SaveAppInfo(appInfo.Name, appInfo);
                return certBin;
            }

            return appInfo.HasCodeSigning == true ?
                this.ClickOnceFileRepository.GetFileContent(appInfo.Name, ".cer") :
                null;
        }

        private byte[] NoImagePng()
        {
            return System.IO.File.ReadAllBytes(Path.Combine(WebHostEnv.WebRootPath, "images", "no-image.png"));
        }

        [HttpGet("publish/register")]
        public ActionResult Register()
        {
            return View();
        }

        [HttpGet("publish/edit")]
        public ActionResult Edit(string id)
        {
            var result = GetMyAppInfo(id);
            if (result.Value == null) return result.Result;

            var theApp = result.Value;
            return View(theApp);
        }

        [HttpPost("publish/edit"), ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, ClickOnceAppInfo model, bool disclosePublisher, [FromQuery] string from = "")
        {
            var result = GetMyAppInfo(id);
            if (result.Value == null) return result.Result;

            if (ModelState.IsValid == false) return View(model);

            var appInfo = result.Value;
            appInfo.Title = model.Title;
            appInfo.Description = model.Description;
            appInfo.ProjectURL = model.ProjectURL;
            SetupPublisherInformtion(disclosePublisher, appInfo);

            appInfo.SignedByPublisher = false;
            if (appInfo.HasCodeSigning == null)
                await this.AppContentManager.UpdateCertificateInfoAsync(appInfo);
            else if (appInfo.HasCodeSigning == true && appInfo.PublisherName != null)
            {
                var tmpPath = Path.Combine(WebHostEnv.ContentRootPath, "App_Data", $"{Guid.NewGuid():N}.cer");
                try
                {
                    var certBin = this.ClickOnceFileRepository.GetFileContent(id, ".cer");
                    System.IO.File.WriteAllBytes(tmpPath, certBin);
                    var sshPubKeyStr = await CertificateValidater.GetSSHPubKeyStrOfGitHubAccountAsync(appInfo.PublisherName);
                    appInfo.SignedByPublisher = CertificateValidater.EqualsPublicKey(sshPubKeyStr, tmpPath);
                }
                finally { System.IO.File.Delete(tmpPath); }
            }

            this.ClickOnceFileRepository.SaveAppInfo(id, appInfo);

            return from == "detail" ? RedirectToRoute("Detail", new { appId = appInfo.Name }) : RedirectToAction("MyApps", "Home");
        }

        private void SetupPublisherInformtion(bool disclosePublisher, ClickOnceAppInfo appInfo)
        {
            if (disclosePublisher)
            {
                var gitHubUserName = User.Identity.Name;
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

        private ActionResult<ClickOnceAppInfo> GetMyAppInfo(string id)
        {
            var userId = User.GetHashedUserId();
            if (userId == null) throw new Exception("hashed user id is null.");

            var theApp = this.ClickOnceFileRepository.GetAppInfo(id);
            if (theApp == null) return NotFound();
            if (theApp.OwnerId != userId) return Error("Sorry, the application name \"{0}\" was already registered by somebody else.", id);

            return theApp;
        }

        private ActionResult CheckCodeBaseUrl(string appName, byte[] buff)
        {
            var appManifest = default(XDocument);
            try
            {
                using (var ms = new MemoryStream(buff))
                    appManifest = XDocument.Load(ms);
            }
            catch (XmlException)
            {
                return Error("The .application file that contained in .zip file you uploaded is may not be valid XML format.");
            }

            var xnm = new XmlNamespaceManager(new NameTable());
            xnm.AddNamespace("asmv1", "urn:schemas-microsoft-com:asm.v1");
            xnm.AddNamespace("asmv2", "urn:schemas-microsoft-com:asm.v2");
            var codeBaseAttr = (appManifest.XPathEvaluate("/asmv1:assembly/asmv2:deployment/asmv2:deploymentProvider/@codebase", xnm) as IEnumerable).Cast<XAttribute>().FirstOrDefault();
            if (codeBaseAttr != null)
            {
                var codebase = codeBaseAttr.Value.ToLower();
                if (Regex.IsMatch(codebase, "^http(s)?://") == false)
                    return Error("The .application file that contained in .zip file you uploaded has invalid format codebase url as HTTP(s) protocol.");

                Func<string, string> stripSchema = url => Regex.Replace(url, "^http(s)?:", "");

                var appUrl = new Uri(this.Request.GetDisplayUrl()).AppUrl(forceSecure: true);
                var actionUrl = this.Url.RouteUrl("Publish", new { appId = appName });
                var baseUrl = appUrl + actionUrl;
                if (stripSchema(codebase) != (stripSchema(baseUrl) + "/" + appName + ".application").ToLower())
                    return Error("The install URL is invalid. You should re-publish the application with fix the install URL as \"{0}\".", baseUrl);
            }

            return null; // Valid/Success.
        }

        private ActionResult Error(string message, params string[] args)
        {
            this.ModelState.AddModelError("Error", string.Format(message, args));
            return View();
        }

        [HttpGet("publish/detail"), AllowAnonymous]
        public ActionResult Detail(string appId)
        {
            var appInfo = this.ClickOnceFileRepository.GetAppInfo(appId);
            if (appInfo == null) return NotFound();

            return View(appInfo);
        }
    }
}