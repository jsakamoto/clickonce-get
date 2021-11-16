using System.Net.Http.Json;
using ClickOnceGet.Shared.Models;
using ClickOnceGet.Shared.Services;

namespace ClickOnceGet.Client.Services;

internal class ClientSideClickOnceAppInfoProvider : IClickOnceAppInfoProvider
{
    private HttpClient HttpClient { get; }

    public ClientSideClickOnceAppInfoProvider(HttpClient httpClient)
    {
        this.HttpClient = httpClient;
    }

    public async Task<IEnumerable<ClickOnceAppInfo>> GetAllAppsAsync()
    {
        var apps = await this.HttpClient.GetFromJsonAsync<ClickOnceAppInfo[]>("/api/apps");
        return apps;
    }

    public async Task<ClickOnceAppInfo> GetAppAsync(string appName)
    {
        var response = await this.HttpClient.GetAsync($"/api/apps/{Uri.EscapeDataString(appName)}");
        await response.EnsureSuccessStatusCodeAsync();
        return await response.Content.ReadFromJsonAsync<ClickOnceAppInfo>();
    }

    public async Task<ClickOnceAppInfo> GetOwnedAppAsync(string appName)
    {
        var response = await this.HttpClient.GetAsync($"/api/myapps/{Uri.EscapeDataString(appName)}");
        await response.EnsureSuccessStatusCodeAsync();
        return await response.Content.ReadFromJsonAsync<ClickOnceAppInfo>();
    }

    public async Task<IEnumerable<ClickOnceAppInfo>> GetOwnedAppsAsync()
    {
        var apps = await this.HttpClient.GetFromJsonAsync<ClickOnceAppInfo[]>("/api/myapps");
        return apps;
    }
}
