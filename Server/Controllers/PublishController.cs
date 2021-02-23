using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClickOnceGet.Server.Services;
using ClickOnceGet.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Toolbelt.Web;

namespace ClickOnceGet.Server.Controllers
{
    [Authorize, ApiController]
    public class PublishController : Controller
    {
        private IClickOnceFileRepository ClickOnceFileRepository { get; }

        private ClickOnceAppContentManager AppContentManager { get; }

        private HttpsRedirecter HttpsRedirecter { get; }

        public PublishController(
            IClickOnceFileRepository clickOnceFileRepository,
            ClickOnceAppContentManager appContentManager,
            HttpsRedirecter httpsRedirecter)
        {
            this.ClickOnceFileRepository = clickOnceFileRepository;
            this.AppContentManager = appContentManager;
            this.HttpsRedirecter = httpsRedirecter;
        }

        // GET: /app/{appId}/{*pathInfo}
        [AllowAnonymous, HttpGet("/app/{appId}/{*pathInfo}")]
        public IActionResult Get(string appId, string pathInfo)
        {
            pathInfo = (pathInfo ?? "").Replace('/', '\\');
            if (pathInfo == "") return Redirect($"/app/{Uri.EscapeUriString(appId)}/{Uri.EscapeUriString(appId)}.application");

            if (pathInfo == "detail") return FallbackView();

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

        private IActionResult FallbackView()
        {
            if (this.HttpsRedirecter.ShouldRedirect(this.Request, out var actionResult)) return actionResult;
            return View("Index");
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
        [AllowAnonymous, HttpGet("/app/{appId}/cert/{*pathInfo}")]
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

        [HttpGet("images/no-image")]
        public ActionResult GetNoImage()
        {
            var timeStamp = System.IO.File.GetLastWriteTimeUtc(new Uri(this.GetType().Assembly.GetName().CodeBase).LocalPath);
            return new CacheableContentResult(
                cacheability: ResponseCacheLocation.Any,
                lastModified: timeStamp,
                etag: timeStamp.Ticks.ToString(),
                contentType: "image/png",
                getContent: () => NoImagePng()
            );
        }

        private byte[] NoImagePng()
        {
            using var stream = this.GetType().Assembly.GetManifestResourceStream("ClickOnceGet.Server.Resources.no-image.png");
            var buff = new byte[stream.Length];
            stream.Read(buff, 0, buff.Length);
            return buff;
        }
    }
}