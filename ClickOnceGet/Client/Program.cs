using System;
using System.Net.Http;
using System.Threading.Tasks;
using ClickOnceGet.Client.Services;
using ClickOnceGet.Shared;
using ClickOnceGet.Shared.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ClickOnceGet.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddSharedServices();
            builder.Services.AddScoped<IClickOnceAppInfoProvider, ClientSideClickOnceAppInfoProvider>();

            await builder.Build().RunAsync();
        }
    }
}
