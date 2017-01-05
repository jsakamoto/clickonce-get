using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using ClickOnceGet.Models;
using Microsoft.AspNet.Identity;

namespace ClickOnceGet
{
    public static class Extensions
    {
        public static string ToMD5(this string text)
        {
            var md5Bytes = new MD5Cng().ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(md5Bytes).Replace("-", "").ToLower();
        }

        public static string GetHashedUserId(this IPrincipal principal)
        {
            if (principal == null) return null;
            var claimsIdentty = principal.Identity as ClaimsIdentity;
            if (claimsIdentty == null) return null;
            return claimsIdentty.FindFirstValue(CustomClaimTypes.HasedUserId);
        }

        public static string AppUrl(this Uri requestUrl)
        {
            return requestUrl.AppUrl(forceSecure: false);
        }

        public static string AppUrl(this UrlHelper urlHelper)
        {
            return urlHelper.AppUrl(forceSecure: false);
        }

        public static string AppUrl(this Uri requestUrl, bool forceSecure)
        {
            var appUrl = requestUrl.GetLeftPart(UriPartial.Scheme | UriPartial.Authority);
            if (forceSecure) appUrl = Regex.Replace(appUrl, "^http:", "https:");
            return appUrl;
        }

        public static string AppUrl(this UrlHelper urlHelper, bool forceSecure)
        {
            var request = urlHelper.RequestContext.HttpContext.Request;
            return request.Url.AppUrl(forceSecure);
        }
    }
}