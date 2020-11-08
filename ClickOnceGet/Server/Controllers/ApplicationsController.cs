using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using ClickOnceGet.Server.Services;
using ClickOnceGet.Shared.Models;
using ClickOnceGet.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClickOnceGet.Server.Controllers
{
    [ApiController]
    public class ApplicationsController : ControllerBase
    {
        private IWebHostEnvironment WebHostEnv { get; }

        private IClickOnceAppInfoProvider ClickOnceAppInfoProvider { get; }

        private IClickOnceFileRepository ClickOnceFileRepository { get; }

        private ClickOnceAppContentManager AppContentManager { get; }

        public ApplicationsController(
            IWebHostEnvironment webHostEnv,
            IClickOnceAppInfoProvider clickOnceAppInfoProvider,
            IClickOnceFileRepository clickOnceFileRepository,
            ClickOnceAppContentManager appContentManager)
        {
            this.WebHostEnv = webHostEnv;
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
            if (appInfo == null) return NotFound();
            return appInfo;
        }

        // GET api/myapps
        [Authorize, HttpGet("api/myapps")]
        public Task<IEnumerable<ClickOnceAppInfo>> GetOwnedAppsAsync()
        {
            return this.ClickOnceAppInfoProvider.GetOwnedAppsAsync();
        }

        // DELETE api/apps/appname or api/myapps/appname
        [Authorize, HttpDelete, Route("api/apps/{appName}"), Route("api/myapps/{appName}")]
        public ActionResult DeleteApp(string appName)
        {
            //this.Request.RequestUri.
            var userId = User.GetHashedUserId();
            if (userId == null) throw new Exception("hashed user id is null.");

            var appInfo = this.ClickOnceFileRepository.GetAppInfo(appName);
            if (appInfo == null) return NotFound();
            if (appInfo.OwnerId != userId) return Forbid();

            this.ClickOnceFileRepository.DeleteApp(appName);
            return NoContent();
        }

        // POST api/myapps/zipedpackage
        [Authorize, HttpPost("api/myapps/zipedpackage")]
        public async Task<ActionResult> Register(IFormFile zipedPackage)
        {
            var userId = User.GetHashedUserId();
            if (userId == null) throw new Exception("hashed user id is null.");
            if (zipedPackage == null) return BadRequest();

            var success = false;
            var tmpPath = Path.Combine(WebHostEnv.ContentRootPath, "App_Data", $"{userId}-{Guid.NewGuid():N}.zip");
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
                if (appFile == null) return BadRequest("The .zip file you uploaded did not contain .application file.");
                if (Path.GetDirectoryName(appFile.FullName) != "") return BadRequest("The .zip file you uploaded contain .application file, but it was not in root of the .zip file.");

                // Validate app name does not conflict.
                var appName = Path.GetFileNameWithoutExtension(appFile.FullName);
                var hasOwnerRight = this.ClickOnceFileRepository.GetOwnerRight(userId, appName);
                if (!hasOwnerRight) return BadRequest($"Sorry, the application name \"{appName}\" was already registered by somebody else.");

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
                this.ClickOnceFileRepository.ClearUpFiles(appName);

                foreach (var item in zip.Entries.Where(_ => _.Name != ""))
                {
                    var buff = new byte[item.Length];
                    using var reader = item.Open();
                    reader.Read(buff, 0, buff.Length);
                    this.ClickOnceFileRepository.SaveFileContent(appName, item.FullName, buff);
#if !DEBUG
                    if (Path.GetExtension(item.FullName).ToLower() == ".application")
                    {
                        var error = CheckCodeBaseUrl(appName, buff);
                        if (error != null) return error;
                    }
#endif
                }

                // Update certificate information.
                await this.AppContentManager.UpdateCertificateInfoAsync(appInfo);

                this.ClickOnceFileRepository.SaveAppInfo(appName, appInfo);

                success = true;
                return Ok(appName);
            }
            catch (InvalidDataException)
            {
                return BadRequest("The file you uploaded looks like invalid Zip format.");
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
    }
}