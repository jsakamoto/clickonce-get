using ClickOnceGet.Client.Services;
using ClickOnceGet.Shared;
using ClickOnceGet.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

namespace ClickOnceGet.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        builder.Services.AddScoped(services =>
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
            var jsRuntime = services.GetService<IJSRuntime>() as IJSInProcessRuntime;
            httpClient.DefaultRequestHeaders.Add(
                "X-ANTIFORGERY-TOKEN",
                jsRuntime.Invoke<string>("ClickOnceGet.Client.Helper.getCookie", "X-ANTIFORGERY-TOKEN"));
            return httpClient;
        });

        builder.Services.AddSharedServices();
        builder.Services.AddAuthorizationCore();
        builder.Services.AddScoped<AuthenticationStateProvider, ClientSideAuthenticationStateProvider>();
        builder.Services.AddScoped<IClickOnceAppInfoProvider, ClientSideClickOnceAppInfoProvider>();

        builder.Logging.AddFilter((category, level) =>
        {
            if (category.StartsWith("Microsoft.AspNetCore.Authorization") && level <= LogLevel.Information)
                return false;
            return true;
        });

        await builder.Build().RunAsync();
    }
}
