using System;
using System.Collections;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ClickOnceGet.Models;
using Microsoft.AspNet.Identity;
using Toolbelt.Drawing;
using Toolbelt.Web;

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

        // GET: /app/{appId}/{*pathInfo}
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

            // Increment downloads counter.
            if (ext == ".deploy")
            {
                var commandPath = GetEntryPointCommandPath(appId);
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
        [AllowAnonymous]
        public ActionResult GetIcon(string appId, int pxSize)
        {
            var appInfo = this.ClickOnceFileRepository.EnumAllApps().FirstOrDefault(app => app.Name.ToLower() == appId.ToLower());
            if (appInfo == null) return HttpNotFound();

            var etag = appInfo.RegisteredAt.Ticks.ToString() + "." + pxSize;
            return new CacheableContentResult(
                    cacheability: HttpCacheability.ServerAndPrivate,
                    lastModified: appInfo.RegisteredAt,
                    etag: etag,
                    contentType: "image/png",
                    getContent: () => InternalGetIcon(appId, pxSize)
                );
        }

        private byte[] InternalGetIcon(string appId, int pxSize = 48)
        {
            var commandPath = GetEntryPointCommandPath(appId);
            if (commandPath == null) return NoImagePng();

            var commandBytes = this.ClickOnceFileRepository.GetFileContent(appId, commandPath);
            if (commandBytes == null) return NoImagePng();

            // extract icon from .exe
            // note: `IconExtractor` use LoadLibrary Win32API, so I need save the command binary into file.
            var tmpPath = Server.MapPath("~/App_Data/" + Guid.NewGuid().ToString("N") + ".exe");
            System.IO.File.WriteAllBytes(tmpPath, commandBytes);
            try
            {
                using (var msIco = new MemoryStream())
                using (var msPng = new MemoryStream())
                {
                    IconExtractor.Extract1stIconTo(tmpPath, msIco);
                    if (msIco.Length == 0) return NoImagePng();

                    msIco.Seek(0, SeekOrigin.Begin);
                    var icon = new FromMono.System.Drawing.Icon(msIco, pxSize, pxSize);

                    icon.ToBitmap().Save(msPng, ImageFormat.Png);
                    return msPng.ToArray();
                }
            }
            finally
            {
                System.IO.File.Delete(tmpPath);
            }
        }

        private string GetEntryPointCommandPath(string appId)
        {
            var dotAppBytes = this.ClickOnceFileRepository.GetFileContent(appId, appId + ".application");
            if (dotAppBytes == null) return null;

            // parse .application
            var ns_asmv2 = "urn:schemas-microsoft-com:asm.v2";
            var dotApp = XDocument.Load(new MemoryStream(dotAppBytes));
            var codebasePath =
                (from node in dotApp.Descendants(XName.Get("dependentAssembly", ns_asmv2))
                 let dependencyType = node.Attribute("dependencyType")
                 where dependencyType != null
                 where dependencyType.Value == "install"
                 select node.Attribute("codebase").Value).FirstOrDefault();
            if (codebasePath == null) return null;

            // parse .manifest to detect .exe file path
            var mnifestBytes = this.ClickOnceFileRepository.GetFileContent(appId, codebasePath);
            if (mnifestBytes == null) return null;
            var manifest = XDocument.Load(new MemoryStream(mnifestBytes));
            var commandName =
                (from entryPoint in manifest.Descendants(XName.Get("entryPoint", ns_asmv2))
                 from commandLine in entryPoint.Descendants(XName.Get("commandLine", ns_asmv2))
                 let file = commandLine.Attribute("file")
                 where file != null
                 select file.Value).FirstOrDefault();
            if (commandName == null) return null;

            // load command(.exe) content binary.
            var pathParts = codebasePath.Split('\\');
            var commandPath = string.Join("\\", pathParts.Take(pathParts.Length - 1).Concat(new[] { commandName + ".deploy" }));

            return commandPath;
        }

        private byte[] NoImagePng()
        {
            return System.IO.File.ReadAllBytes(Server.MapPath("~/Content/images/no-image.png"));
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(HttpPostedFileBase zipedPackage)
        {
            var userId = User.GetHashedUserId();
            if (userId == null) throw new Exception("hashed user id is null.");

            if (ModelState.IsValid == false) return View();

            try
            {
                using (var zip = new ZipArchive(zipedPackage.InputStream))
                {
                    // Validate files structure that are included in a .zip file.
                    var appFile = zip.Entries
                        .Where(e => Path.GetExtension(e.FullName).ToLower() == ".application")
                        .OrderBy(e => e.FullName.Length)
                        .FirstOrDefault();
                    if (appFile == null) return Error("The .zip file you uploaded did not contain .application file.");
                    if (Path.GetDirectoryName(appFile.FullName) != "") return Error("The .zip file you uploaded contain .application file, but it was not in root of the .zip file.");

                    // Validate app name does not conflict.
                    var appName = Path.GetFileNameWithoutExtension(appFile.FullName);
                    var success = this.ClickOnceFileRepository.GetOwnerRight(userId, appName);
                    if (success == false) return Error("Sorry, the application name \"{0}\" was already registered by somebody else.", appName);

                    var appInfo = this.ClickOnceFileRepository.EnumAllApps().FirstOrDefault(a => a.Name == appName);
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
                    this.ClickOnceFileRepository.SaveAppInfo(appName, appInfo);

                    foreach (var item in zip.Entries.Where(_ => _.Name != ""))
                    {
                        var buff = new byte[item.Length];
                        using (var reader = item.Open())
                        {
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
                    }

                    return RedirectToAction("Edit", new { id = appName });
                }
            }
            catch (System.IO.InvalidDataException)
            {
                return Error("The file you uploaded did not looks like valid Zip format.");
            }
        }

        [HttpGet]
        public ActionResult Edit(string id)
        {
            var result = GetMyAppInfo(id);
            if (result is ActionResult) return result as ActionResult;

            var theApp = result as ClickOnceAppInfo;
            return View(theApp);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(string id, ClickOnceAppInfo model, bool disclosePublisher)
        {
            var result = GetMyAppInfo(id);
            if (result is ActionResult) return result as ActionResult;

            if (ModelState.IsValid == false) return View(model);

            var appInfo = result as ClickOnceAppInfo;
            appInfo.Title = model.Title;
            appInfo.Description = model.Description;
            appInfo.ProjectURL = model.ProjectURL;
            SetupPublisherInformtion(disclosePublisher, appInfo);

            this.ClickOnceFileRepository.SaveAppInfo(id, appInfo);

            var from = Request.QueryString["from"];
            return from == "detail" ? RedirectToRoute("Detail", new { appId = appInfo.Name }) : RedirectToAction("MyApps", "Home");
        }

        private void SetupPublisherInformtion(bool disclosePublisher, ClickOnceAppInfo appInfo)
        {
            if (disclosePublisher)
            {
                var gitHubUserName = User.Identity.GetUserName();
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

        private object GetMyAppInfo(string id)
        {
            var userId = User.GetHashedUserId();
            if (userId == null) throw new Exception("hashed user id is null.");

            var theApp = this.ClickOnceFileRepository.EnumAllApps().FirstOrDefault(app => app.Name == id);
            if (theApp == null) return new HttpStatusCodeResult(HttpStatusCode.NotFound);
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

                var appUrl = this.Request.Url.AppUrl();
                var actionUrl = this.Url.RouteUrl("Publish", new { appId = appName });
                var baseUrl = appUrl + actionUrl;
                if (codebase != (baseUrl + "/" + appName + ".application").ToLower())
                    return Error("The install URL is invalid. You should re-publish the application with fix the install URL as \"{0}\".", baseUrl);
            }

            return null; // Valid/Success.
        }

        private ActionResult Error(string message, params string[] args)
        {
            this.ModelState.AddModelError("Error", string.Format(message, args));
            return View();
        }

        [HttpGet, AllowAnonymous]
        public ActionResult Detail(string appId)
        {
            var appInfo = this.ClickOnceFileRepository.EnumAllApps().FirstOrDefault(app => app.Name == appId);
            if (appInfo == null) return HttpNotFound();

            return View(appInfo);
        }
    }
}