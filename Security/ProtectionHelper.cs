using System.Security.Claims;
using PersonalLibrary.Models;

namespace PersonalLibrary.Security
{
    public static class ProtectionHelper
    {
        public const string StampClaim = "protection_stamp";

        public static List<Claim> ClaimsFor(AppUser user) => new()
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Login),
            new(StampClaim, user.SecurityStamp),
        };

        public static string GetClientIp(HttpContext context)
        {
            var remote = context.Connection.RemoteIpAddress;
            if (remote != null && System.Net.IPAddress.IsLoopback(remote))
            {
                var cf = context.Request.Headers["CF-Connecting-IP"].ToString();
                if (!string.IsNullOrWhiteSpace(cf)) return cf.Trim();

                var xff = context.Request.Headers["X-Forwarded-For"].ToString();
                if (!string.IsNullOrWhiteSpace(xff)) return xff.Split(',')[0].Trim();
            }
            return remote?.ToString() ?? "unknown";
        }

    }
}
