using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ClickOnceGet.Shared.Models;
using ClickOnceGet.Shared.Services;

namespace ClickOnceGet.Client.Services
{
    internal class ClientSideClickOnceAppInfoProvider : IClickOnceAppInfoProvider
    {
        private HttpClient HttpClient { get; }

        public ClientSideClickOnceAppInfoProvider(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public async Task<IEnumerable<ClickOnceAppInfo>> GetClickOnceAppsAsync()
        {
            var apps = await HttpClient.GetFromJsonAsync<ClickOnceAppInfo[]>("/api/apps");
            return apps;
        }
    }
}