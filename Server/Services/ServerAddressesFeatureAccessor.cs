using Microsoft.AspNetCore.Hosting.Server.Features;

namespace ClickOnceGet.Server.Services;

public class ServerAddressesFeatureAccessor
{
    public IServerAddressesFeature ServerAddressesFeature { get; set; }
}
