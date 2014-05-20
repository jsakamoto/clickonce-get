using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using ClickOnceGet.Models;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;

namespace ClickOnceGet.Controllers
{
    [Authorize]
    public class PublishController : Controller
    {
        public IClickOnceFileRepository ClickOnceFileRepository { get; set; }

        public PublishController()
        {
            this.ClickOnceFileRepository = new AppDataDirRepository();
        }

        // GET: Publish
        [AllowAnonymous]
        public ActionResult Get(string appId, string pathInfo)
        {
            pathInfo = (pathInfo ?? "").Replace('/', '\\');
            var fileBytes = pathInfo == "" ?
                this.ClickOnceFileRepository.GetDefaultFile(appId) :
                this.ClickOnceFileRepository.GetFile(appId, pathInfo);
            if (fileBytes == null) return HttpNotFound();

            var ext = Path.GetExtension(pathInfo).ToLower();
            var contentType = pathInfo == "" || ext == ".application" ?
                "application/x-ms-application" :
                MimeMapping.GetMimeMapping(pathInfo);
            return File(fileBytes, contentType);
        }

        [HttpGet]
        public ActionResult Regist()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Regist(HttpPostedFileBase zipedPackage)
        {
            var userId = User.GetHashedUserId();
            if (userId == null) throw new Exception("hased user id is null.");

            using (var zip = new ZipArchive(zipedPackage.InputStream))
            {
                var appFile = zip.Entries.First(e => Path.GetExtension(e.FullName).ToLower() == ".application");
                var appName = Path.GetFileNameWithoutExtension(appFile.FullName);
                var success = this.ClickOnceFileRepository.GetOwnerRight(userId, appName);
                if (success)
                {
                    this.ClickOnceFileRepository.ClearAllFiles(appName);

                    foreach (var item in zip.Entries)
                    {
                        var buff = new byte[item.Length];
                        using (var reader = item.Open())
                        {
                            reader.Read(buff, 0, buff.Length);
                            this.ClickOnceFileRepository.SetFile(appName, item.FullName, buff);
                        }
                    }
                }
            }

            return RedirectToAction("MyApps", "Home");
        }
    }
}