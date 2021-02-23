using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ClickOnceGet.Server.Options;
using ClickOnceGet.Server.Services;
using ClickOnceGet.Shared;
using ClickOnceGet.Shared.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Toolbelt.Extensions.DependencyInjection;

namespace ClickOnceGet.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ClickOnceGetOptions>(Configuration.GetSection("ClickOnceGet"));

            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddAntiforgery(options => options.HeaderName = "X-ANTIFORGERY-TOKEN");

            services.AddSharedServices();

            services.AddServerAddressesFeatureAccessor();
            services.AddTransient<HttpsRedirecter>();
            services.AddSingleton<CertificateValidater>();
            services.AddSingleton<IClickOnceFileRepository, AppDataDirRepository>();
            services.AddScoped<IClickOnceAppInfoProvider, ServerSideClickOnceAppInfoProvider>();
            services.AddScoped<ClickOnceAppContentManager>();

            services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddGitHub(options =>
                {
                    options.ClientId = Configuration.GetValue<string>("Authentication:GitHub:ClientId");
                    options.ClientSecret = Configuration.GetValue<string>("Authentication:GitHub:ClientSecret");
                })
                .AddCookie(options =>
                {
                    options.Events.OnSigningIn = (context) =>
                    {
                        var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
                        var providerKey = claimsIdentity.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
                        claimsIdentity.AddClaim(new Claim(CustomClaimTypes.IdentityProvider, "GitHub"));
                        claimsIdentity.AddClaim(new Claim(CustomClaimTypes.HasedUserId, $"GitHub|{providerKey}|{Configuration["Authentication:Salt"]}".ToMD5()));
                        return Task.CompletedTask;
                    };
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
                app.UseCssLiveReload();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            // DO NOT USE "UseHsts()" and "UseHttpsRedirection()"
            // because the scheme of the codebase URL of old packages are "http".
            // Instead, fall back action (FallbackViewsController.Index()) will redirects to https.
            app.UseServerAddressesFeatureAccessor();

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToController(action: "Index", controller: "FallbackViews");
            });
        }
    }
}
