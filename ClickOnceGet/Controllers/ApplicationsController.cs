using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ClickOnceGet.Models;

namespace ClickOnceGet.Controllers
{
    [RoutePrefix("api")]
    public class ApplicationsController : ApiController
    {
        public IClickOnceFileRepository ClickOnceFileRepository { get; set; }

        public ApplicationsController()
        {
            this.ClickOnceFileRepository = new AppDataDirRepository();
        }

        // GET api/apps
        [HttpGet, Route("apps")]
        public IEnumerable<ClickOnceAppInfo> GetApps()
        {
            return this.ClickOnceFileRepository.EnumAllApplications();
        }

        // GET api/apps/appname
        [HttpGet, Route("apps/{appName}")]
        public ClickOnceAppInfo GetApp(string appName)
        {
            var appInfo = this.GetApps().FirstOrDefault(inf => inf.Name == appName);
            if (appInfo == null) throw new HttpResponseException(HttpStatusCode.NotFound);
            return appInfo;
        }

        // GET api/myapps
        [Authorize, HttpGet, Route("myapps")]
        public IEnumerable<ClickOnceAppInfo> GetMyApps()
        {
            var userId = User.GetHashedUserId();
            if (userId == null) throw new Exception("hashed user id is null.");

            return this.GetApps().Where(app => app.OwnerId == userId);
        }


        // DELETE api/apps/appname or api/myapps/appname
        [Authorize, HttpDelete, Route("apps/{appName}"), Route("myapps/{appName}")]
        public void DeleteApp(string appName)
        {
            var userId = User.GetHashedUserId();
            if (userId == null) throw new Exception("hashed user id is null.");

            var appInfo = this.GetApp(appName);
            if (appInfo == null) throw new HttpResponseException(HttpStatusCode.NotFound);
            if (appInfo.OwnerId != userId) throw new HttpResponseException(HttpStatusCode.Forbidden);

            this.ClickOnceFileRepository.DeleteApp(appName);
        }
    }
}