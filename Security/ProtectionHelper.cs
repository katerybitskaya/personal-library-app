using Microsoft.AspNetCore.Identity;
using PersonalLibrary.Models;
using System.Security.Cryptography;
using System.Text;

namespace PersonalLibrary.Security
{
    public static class ProtectionHelper
    {
        public const string StampClaim = "protection_stamp";

        public static string GetStamp(ProtectionSettings s)
        {
            var raw = $"{s.Username}\n{s.PasswordHash}\n{s.Password}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToBase64String(bytes, 0, 12);
        }

        public static bool VerifyCredentials(ProtectionSettings s, string? username, string? password)
        {
            if (!s.HasCredentials || username == null || password == null) return false;

            var userOk = FixedEquals(username.Trim(), s.Username.Trim());
            bool passOk;

            if (!string.IsNullOrWhiteSpace(s.PasswordHash))
            {
                try
                {
                    var result = new PasswordHasher<object>()
                        .VerifyHashedPassword(new object(), s.PasswordHash.Trim(), password);
                    passOk = result != PasswordVerificationResult.Failed;
                }
                catch (FormatException)
                {
                    passOk = false;
                }
            }
            else
            {
                passOk = FixedEquals(password, s.Password);
            }

            return userOk && passOk;
        }

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

        private static bool FixedEquals(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }
}
