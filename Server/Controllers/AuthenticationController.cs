#nullable enable
using System.Linq;
using AspNet.Security.OAuth.GitHub;
using ClickOnceGet.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace ClickOnceGet.Server.Controllers
{
    public class AuthenticationController : Controller
    {
        [HttpGet("/auth/signin")]
        public IActionResult OnGetSignIn([FromQuery] string? returnUri)
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = returnUri ?? "/" },
                GitHubAuthenticationDefaults.AuthenticationScheme);
        }

        [HttpPost("/auth/signout")]
        public IActionResult OnPostSignOut()
        {
            return SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                CookieAuthenticationDefaults.AuthenticationScheme);
        }

        [HttpGet("/api/auth/currentuser")]
        public AuthUserInfo GetCurrentUser()
        {
            var name = this.User.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? this.User.Identity?.Name ?? "";
            return new AuthUserInfo { Name = name };
        }
    }
}
