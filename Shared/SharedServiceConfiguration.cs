using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace ClickOnceGet.Shared;

public static class SharedServiceConfiguration
{
    public static void AddSharedServices(this IServiceCollection services)
    {
        services.AddMudServices();
    }
}
