#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClickOnceGet.Shared.Models;
using ClickOnceGet.Shared.Services;
using Microsoft.AspNetCore.Http;

namespace ClickOnceGet.Server.Services
{
    public class ServerSideClickOnceAppInfoProvider : IClickOnceAppInfoProvider
    {
        private IClickOnceFileRepository ClickOnceFileRepository { get; }

        private IHttpContextAccessor HttpContextAccessor { get; }

        public ServerSideClickOnceAppInfoProvider(
            IClickOnceFileRepository clickOnceFileRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            ClickOnceFileRepository = clickOnceFileRepository;
            HttpContextAccessor = httpContextAccessor;
        }

        public Task<IEnumerable<ClickOnceAppInfo>> GetAllAppsAsync()
        {
            return Task.FromResult(this.ClickOnceFileRepository.EnumAllApps());
        }

        public async Task<ClickOnceAppInfo?> GetAppAsync(string appName)
        {
            var appInfo = this.ClickOnceFileRepository.GetAppInfo(appName);
            return await Task.FromResult(appInfo);
        }

        public async Task<IEnumerable<ClickOnceAppInfo>> GetOwnedAppsAsync()
        {
            var user = HttpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true) return Enumerable.Empty<ClickOnceAppInfo>();

            var hashedUserId = user.GetHashedUserId();
            if (hashedUserId == null) throw new Exception("hashed user id is null.");

            var allApps = await this.GetAllAppsAsync();
            return allApps.Where(app => app.OwnerId == hashedUserId);
        }
    }
}
