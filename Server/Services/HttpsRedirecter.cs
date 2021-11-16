using System.Diagnostics.CodeAnalysis;
using ClickOnceGet.Server.Options;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ClickOnceGet.Server.Services;

public class HttpsRedirecter
{
    private IConfiguration Configuration { get; }

    private IServerAddressesFeature ServerAddresses { get; }

    private IOptionsMonitor<ClickOnceGetOptions> Options { get; }

    public HttpsRedirecter(
        IConfiguration configuration,
        ServerAddressesFeatureAccessor serverAddressesFeatureAccessor,
        IOptionsMonitor<ClickOnceGetOptions> options)
    {
        this.Configuration = configuration;
        this.ServerAddresses = serverAddressesFeatureAccessor.ServerAddressesFeature;
        this.Options = options;
    }

    public bool ShouldRedirect(HttpRequest request, [NotNullWhen(true)] out IActionResult actionResult)
    {
        if (!request.IsHttps && this.Options.CurrentValue.DontRedirectToHttps == false)
        {
            var httpsPort = this.TryGetHttpsPort();
            if (httpsPort != -1)
            {
                var host = request.Host;
                host = ((httpsPort != 443) ? new HostString(host.Host, httpsPort) : new HostString(host.Host));
                var httpsUrl = UriHelper.BuildAbsolute("https", host, request.PathBase, request.Path, request.QueryString);

                actionResult = new RedirectResult(httpsUrl, permanent: true);
                return true;
            }
        }

        actionResult = null;
        return false;
    }

    private int TryGetHttpsPort()
    {
        var httpsPort = this.Configuration.GetValue<int?>("HTTPS_PORT") ?? this.Configuration.GetValue<int?>("ANCM_HTTPS_PORT");
        if (httpsPort.HasValue) return httpsPort.Value;

        if (this.ServerAddresses == null) return -1;

        foreach (var address in this.ServerAddresses.Addresses)
        {
            var bindingAddress = BindingAddress.Parse(address);
            if (bindingAddress.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                if (httpsPort.HasValue && httpsPort != bindingAddress.Port) return -1;
                httpsPort = bindingAddress.Port;
            }
        }
        if (httpsPort.HasValue) return httpsPort.Value;
        return -1;
    }
}
