using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
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
            return requestUrl.GetLeftPart(UriPartial.Scheme | UriPartial.Authority);
        }

        public static string AppUrl(this UrlHelper urlHelper)
        {
            var request = urlHelper.RequestContext.HttpContext.Request;
            return request.Url.GetLeftPart(UriPartial.Scheme | UriPartial.Authority);
        }

        public static ClickOnceAppInfo GetAppInfoById(this IClickOnceFileRepository repository, string appId)
        {
            var appInfo = repository.EnumAllApps().GetAppInfoById(appId);
            return appInfo;
        }

        public static ClickOnceAppInfo GetAppInfoById(this IEnumerable<ClickOnceAppInfo> apps, string appId)
        {
            var appInfo = apps.FirstOrDefault(app => app.Name.ToLower() == appId.ToLower());
            return appInfo;
        }
    }
}