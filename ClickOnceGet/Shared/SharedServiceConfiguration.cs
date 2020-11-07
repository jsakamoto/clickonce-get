using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace ClickOnceGet.Shared
{
    public static class SharedServiceConfiguration
    {
        public static void AddSharedServices(this IServiceCollection services)
        {
            services.AddMudBlazorDialog();
            services.AddMudBlazorSnackbar();
            services.AddMudBlazorResizeListener();
        }
    }
}
