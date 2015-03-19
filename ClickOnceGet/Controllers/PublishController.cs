using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ClickOnceGet.Models;

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
            if (pathInfo == "")
                return Redirect(Url.RouteUrl("Publish", new { appId, pathInfo = appId + ".application" }));
            var fileBytes = this.ClickOnceFileRepository.GetFileContent(appId, pathInfo);
            if (fileBytes == null) return HttpNotFound();

            var ext = Path.GetExtension(pathInfo).ToLower();
            var contentType = pathInfo == "" || ext == ".application" ?
                "application/x-ms-application" :
                MimeMapping.GetMimeMapping(pathInfo);
            return File(fileBytes, contentType);
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(HttpPostedFileBase zipedPackage, ClickOnceAppInfo appInfo)
        {
            var userId = User.GetHashedUserId();
            if (userId == null) throw new Exception("hased user id is null.");

            if (ModelState.IsValid == false) return View(appInfo);

            try
            {
                using (var zip = new ZipArchive(zipedPackage.InputStream))
                {
                    var appFile = zip.Entries
                        .Where(e => Path.GetExtension(e.FullName).ToLower() == ".application")
                        .OrderBy(e => e.FullName.Length)
                        .FirstOrDefault();
                    if (appFile == null) return Error("The .zip file you uploaded did not contain .application file.");
                    if (Path.GetDirectoryName(appFile.FullName) != "") return Error("The .zip file you uploaded contain .application file, but it was not in root of the .zip file.");

                    var appName = Path.GetFileNameWithoutExtension(appFile.FullName);
                    var success = this.ClickOnceFileRepository.GetOwnerRight(userId, appName);
                    if (success == false) return Error("Sorry, the application name \"{0}\" was already registered by somebody else.", appName);

                    this.ClickOnceFileRepository.ClearUpFiles(appName);

                    appInfo.Name = appName;
                    appInfo.OwnerId = userId;
                    appInfo.RegisteredAt = DateTime.UtcNow;
                    this.ClickOnceFileRepository.SaveAppInfo(appName, appInfo);

                    foreach (var item in zip.Entries.Where(_ => _.Name != ""))
                    {
                        var buff = new byte[item.Length];
                        using (var reader = item.Open())
                        {
                            reader.Read(buff, 0, buff.Length);
                            this.ClickOnceFileRepository.SaveFileContent(appName, item.FullName, buff);
                            if (Path.GetExtension(item.FullName).ToLower() == ".application")
                            {
                                var error = CheckCodeBaseUrl(appName, buff);
                                if (error != null) return error;
                            }
                        }
                    }
                }
            }
            catch (System.IO.InvalidDataException)
            {
                return Error("The file you uploaded did not looks like valid Zip format.");
            }

            return RedirectToAction("MyApps", "Home");
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
            if (codeBaseAttr == null)
                return Error("The .application file that contained in .zip file you uploaded is may not be valid format. " +
                    "(assembly/deployment/deploymentProvider@codebase node colud not found.)");
            var codebase = codeBaseAttr.Value.ToLower();
            if (Regex.IsMatch(codebase, "^http(s)?://") == false)
                return Error("The .application file that contained in .zip file you uploaded has invalid format codebase url as HTTP(s) protocol.");

            var appUrl = this.Request.Url.AppUrl();
            var actionUrl = this.Url.RouteUrl("Publish", new { appId = appName });
            var baseUrl = appUrl + actionUrl;
            if (codebase != (baseUrl + "/" + appName + ".application").ToLower())
                return Error("The install URL is invalid. You should re-publish the application with fix the install URL as \"{0}\".", baseUrl);

            return null; // Valid/Success.
        }

        private ActionResult Error(string message, params string[] args)
        {
            this.ModelState.AddModelError("Error", string.Format(message, args));
            return View();
        }
    }
}