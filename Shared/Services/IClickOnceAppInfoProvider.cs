#nullable enable
using ClickOnceGet.Shared.Models;

namespace ClickOnceGet.Shared.Services;

public interface IClickOnceAppInfoProvider
{
    Task<IEnumerable<ClickOnceAppInfo>> GetAllAppsAsync();

    Task<ClickOnceAppInfo?> GetAppAsync(string appName);

    Task<ClickOnceAppInfo?> GetOwnedAppAsync(string appName);

    Task<IEnumerable<ClickOnceAppInfo>> GetOwnedAppsAsync();
}
