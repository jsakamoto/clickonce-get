using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClickOnceGet.Server.Services
{
    public static class ServerAddressesFeatureAccessorExtension
    {
        public static IServiceCollection AddServerAddressesFeatureAccessor(this IServiceCollection services)
        {
            services.TryAddSingleton<ServerAddressesFeatureAccessor>();
            return services;
        }

        public static IApplicationBuilder UseServerAddressesFeatureAccessor(this IApplicationBuilder app)
        {
            var serverAddressesFeatureAccessor = app.ApplicationServices.GetRequiredService<ServerAddressesFeatureAccessor>();
            serverAddressesFeatureAccessor.ServerAddressesFeature = app.ServerFeatures.Get<IServerAddressesFeature>();
            return app;
        }
    }
}
