using System;
using System.Net;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ClickOnceGet.Startup))]
namespace ClickOnceGet
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // GitHub supports only TLS v1.2.
            // https://githubengineering.com/crypto-removal-notice/
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            ConfigureAuth(app);
        }
    }
}
