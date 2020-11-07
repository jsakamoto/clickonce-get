using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClickOnceGet.Server.Services;
using ClickOnceGet.Shared.Models;
using ClickOnceGet.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickOnceGet.Server.Controllers
{
    [ApiController]
    public class ApplicationsController : Controller
    {
        private IClickOnceAppInfoProvider ClickOnceAppInfoProvider { get; }

        private IClickOnceFileRepository ClickOnceFileRepository { get; }

        public ApplicationsController(
            IClickOnceAppInfoProvider clickOnceAppInfoProvider,
            IClickOnceFileRepository clickOnceFileRepository)
        {
            this.ClickOnceAppInfoProvider = clickOnceAppInfoProvider;
            this.ClickOnceFileRepository = clickOnceFileRepository;
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
    }
}