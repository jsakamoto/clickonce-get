using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddAntiforgery(options => options.HeaderName = "X-ANTIFORGERY-TOKEN");

            services.AddSharedServices();

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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
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
