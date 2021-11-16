#nullable enable
using ClickOnceGet.Shared.Models;
using ClickOnceGet.Shared.Services;

namespace ClickOnceGet.Server.Services;

public class ServerSideClickOnceAppInfoProvider : IClickOnceAppInfoProvider
{
    private IClickOnceFileRepository ClickOnceFileRepository { get; }

    private IHttpContextAccessor HttpContextAccessor { get; }

    public ServerSideClickOnceAppInfoProvider(
        IClickOnceFileRepository clickOnceFileRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        this.ClickOnceFileRepository = clickOnceFileRepository;
        this.HttpContextAccessor = httpContextAccessor;
    }

    public Task<IEnumerable<ClickOnceAppInfo>> GetAllAppsAsync()
    {
        return Task.FromResult(this.ClickOnceFileRepository.EnumAllApps().OrderByDescending(appInfo => appInfo.RegisteredAt).AsEnumerable());
    }

    public Task<ClickOnceAppInfo?> GetAppAsync(string appName)
    {
        var appInfo = this.ClickOnceFileRepository.GetAppInfo(appName);
        return Task.FromResult<ClickOnceAppInfo?>(appInfo);
    }

    public Task<ClickOnceAppInfo?> GetOwnedAppAsync(string appName)
    {
        var user = this.HttpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return Task.FromResult<ClickOnceAppInfo?>(null);

        var hashedUserId = user.GetHashedUserId();
        if (hashedUserId == null) throw new Exception("hashed user id is null.");

        var appInfo = this.ClickOnceFileRepository.GetAppInfo(appName);
        if (appInfo == null) return Task.FromResult<ClickOnceAppInfo?>(null);
        if (appInfo.OwnerId != hashedUserId) return Task.FromResult<ClickOnceAppInfo?>(null);

        return Task.FromResult<ClickOnceAppInfo?>(appInfo);
    }

    public async Task<IEnumerable<ClickOnceAppInfo>> GetOwnedAppsAsync()
    {
        var user = this.HttpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return Enumerable.Empty<ClickOnceAppInfo>();

        var hashedUserId = user.GetHashedUserId();
        if (hashedUserId == null) throw new Exception("hashed user id is null.");

        var allApps = await this.GetAllAppsAsync();
        return allApps.Where(app => app.OwnerId == hashedUserId);
    }
}
