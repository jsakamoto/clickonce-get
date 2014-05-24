using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;

namespace ClickOnceGet.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private IAuthenticationManager AuthenticationManager { get { return HttpContext.GetOwinContext().Authentication; } }

        //
        // GET: /Account/SignIn
        [HttpGet, AllowAnonymous]
        public ActionResult SignIn(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/ExternalSignIn
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public ActionResult ExternalSignIn(string provider, string returnUrl)
        {
#if DEBUG
            if (provider == "demo")
            {
                var signInInfo = new ExternalLoginInfo
                {
                    DefaultUserName = "demo",
                    Email = "demo@example.com",
                    ExternalIdentity = new ClaimsIdentity(new Claim[] { 
                        new Claim(ClaimTypes.NameIdentifier, "abc123")
                    }),
                    Login = new UserLoginInfo("demo", "abc123")
                };
                return ExternalSignInCore(returnUrl, signInInfo);
            }
#endif
            // Request a redirect to the external sign in  provider
            return new ChallengeResult(provider, Url.Action("ExternalSignInCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/ExternalSignInCallback
        [HttpGet, AllowAnonymous]
        public async Task<ActionResult> ExternalSignInCallback(string returnUrl)
        {
            var signInInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            return ExternalSignInCore(returnUrl, signInInfo);
        }

        private ActionResult ExternalSignInCore(string returnUrl, ExternalLoginInfo signInInfo)
        {
            if (signInInfo == null) return RedirectToAction("SignIn");

            var login = signInInfo.Login;
            var claimsIdentity = new ClaimsIdentity(signInInfo.ExternalIdentity.Claims, DefaultAuthenticationTypes.ApplicationCookie.ToString());
            claimsIdentity.AddClaim(new Claim(CustomClaimTypes.IdentityProvider, login.LoginProvider));
            claimsIdentity.AddClaim(new Claim(CustomClaimTypes.HasedUserId, (login.LoginProvider + "|" + login.ProviderKey + "|" + AppSettings.Salt).ToMD5()));

            this.AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            #region Summary:
            //     Add information to the response environment that will cause the appropriate
            //     authentication middleware to grant a claims-based identity to the recipient
            //     of the response. The exact mechanism of this may vary.  Examples include
            //     setting a cookie, to adding a fragment on the redirect url, or producing
            //     an OAuth2 access code or token response.
            //
            // Parameters:
            //   properties:
            //     Contains additional properties the middleware are expected to persist along
            //     with the claims. These values will be returned as the AuthenticateResult.properties
            //     collection when AuthenticateAsync is called on subsequent requests.
            //
            //   identities:
            //     Determines which claims are granted to the signed in user. The ClaimsIdentity.AuthenticationType
            //     property is compared to the middleware's Options.AuthenticationType value
            //     to determine which claims are granted by which middleware. The recommended
            //     use is to have a single ClaimsIdentity which has the AuthenticationType matching
            //     a specific middleware.
            #endregion
            this.AuthenticationManager.SignIn(new AuthenticationProperties
            {
                IsPersistent = false
            },
            claimsIdentity);

            return RedirectToLocal(returnUrl);
        }

        //
        // POST: /Account/SignOut
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SignOut()
        {
            AuthenticationManager.SignOut();
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalSignInFailure
        [HttpGet, AllowAnonymous]
        public ActionResult ExternalSignInFailure()
        {
            return View();
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        private class ChallengeResult : HttpUnauthorizedResult
        {
            public string SignInProvider { get; set; }

            public string RedirectUri { get; set; }

            public ChallengeResult(string provider, string redirectUri)
            {
                SignInProvider = provider;
                RedirectUri = redirectUri;
            }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, SignInProvider);
            }
        }
    }
}