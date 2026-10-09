using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using PersonalLibrary.Models;

namespace PersonalLibrary.Security
{
    public class ProtectionMiddleware
    {
        private static readonly string[] PublicPrefixes =
        {
            "/account/login",
            "/home/setlanguage",
            "/css/",
            "/js/",
            "/favicon.ico",
        };

        private readonly RequestDelegate _next;

        public ProtectionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, IOptionsMonitor<ProtectionSettings> options)
        {
            var settings = options.CurrentValue;
            if (!settings.Enabled)
            {
                await _next(context);
                return;
            }

            var user = context.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var stamp = user.FindFirst(ProtectionHelper.StampClaim)?.Value;
                if (stamp == ProtectionHelper.GetStamp(settings))
                {
                    await _next(context);
                    return;
                }
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "/";
            if (PublicPrefixes.Any(p => path.StartsWith(p)))
            {
                await _next(context);
                return;
            }

            if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
            {
                var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                context.Response.Redirect("/Account/Login?returnUrl=" + Uri.EscapeDataString(returnUrl));
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
    }
}
