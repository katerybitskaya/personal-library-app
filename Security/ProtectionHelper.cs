using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using PersonalLibrary.Models;

namespace PersonalLibrary.Security
{
    public static class ProtectionHelper
    {
        public const string StampClaim = "protection_stamp";

        public static bool InContainer =>
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        public static List<Claim> ClaimsFor(AppUser user) => new()
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Login),
            new(StampClaim, user.SecurityStamp),
        };

        public static string GetClientIp(HttpContext context)
        {
            var remote = context.Connection.RemoteIpAddress;
            if (remote != null && (IPAddress.IsLoopback(remote) || (InContainer && IsPrivate(remote))))
            {
                var cf = context.Request.Headers["CF-Connecting-IP"].ToString();
                if (!string.IsNullOrWhiteSpace(cf)) return cf.Trim();

                var xff = context.Request.Headers["X-Forwarded-For"].ToString();
                if (!string.IsNullOrWhiteSpace(xff)) return xff.Split(',')[0].Trim();
            }
            return remote?.ToString() ?? "unknown";
        }

        private static bool IsPrivate(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
            if (address.AddressFamily != AddressFamily.InterNetwork) return address.IsIPv6UniqueLocal;
            var b = address.GetAddressBytes();
            return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
        }

    }
}
