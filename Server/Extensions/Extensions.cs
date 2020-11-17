#nullable enable
using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace ClickOnceGet
{
    public static class Extensions
    {
        public static string ToMD5(this string text)
        {
            using var md5 = MD5.Create();
            var md5Bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(md5Bytes).Replace("-", "").ToLower();
        }

        public static string? GetHashedUserId(this IPrincipal principal)
        {
            if (principal == null) return null;
            var claimsIdentty = principal.Identity as ClaimsIdentity;
            if (claimsIdentty == null) return null;
            return claimsIdentty.Claims.First(c => c.Type == CustomClaimTypes.HasedUserId).Value;
        }

        public static string AppUrl(this Uri requestUrl)
        {
            return requestUrl.AppUrl(forceSecure: false);
        }

        public static string AppUrl(this Uri requestUrl, bool forceSecure)
        {
            var appUrl = requestUrl.GetLeftPart(UriPartial.Scheme | UriPartial.Authority);
            if (forceSecure) appUrl = Regex.Replace(appUrl, "^http:", "https:");
            return appUrl;
        }
    }
}