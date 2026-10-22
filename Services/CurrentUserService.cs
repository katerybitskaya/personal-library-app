using System.Security.Claims;
using Microsoft.Extensions.Options;
using PersonalLibrary.Models;

namespace PersonalLibrary.Services
{
    public class CurrentUserService
    {
        private readonly IHttpContextAccessor _http;
        private readonly IOptionsMonitor<ProtectionSettings> _protection;
        private readonly UserService _users;
        private readonly LocalizationService _l;

        public CurrentUserService(IHttpContextAccessor http, IOptionsMonitor<ProtectionSettings> protection,
            UserService users, LocalizationService l)
        {
            _http = http;
            _protection = protection;
            _users = users;
            _l = l;
        }

        public bool ProtectionEnabled => _protection.CurrentValue.Enabled;

        public bool IsSignedIn => ProtectionEnabled && _http.HttpContext?.User.Identity?.IsAuthenticated == true;

        private static readonly AppUser LocalAdmin = new() { Id = AppUser.AdminId, IsAdmin = true, Color = UserColors.Default };

        public AppUser? User => ProtectionEnabled
            ? _users.GetById(_http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            : LocalAdmin;

        public string Id => User?.Id ?? AppUser.AdminId;

        public bool IsAdmin => User?.IsAdmin == true;

        public string ColorHex => UserColors.Hex(User?.Color ?? UserColors.Default);

        public string NameOf(AppUser user) =>
            !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName!
            : user.IsAdmin ? _l["Users_Admin"] : user.Login;

        public string NameOf(string? userId)
        {
            var user = _users.GetById(userId);
            return user == null ? _l["Users_DeletedUser"] : NameOf(user);
        }

        public string ColorOf(string? userId)
        {
            var user = _users.GetById(userId);
            return user == null ? "#6b7688" : UserColors.Hex(user.Color);
        }

        public string Initial(AppUser user)
        {
            var name = NameOf(user);
            return name.Length > 0 ? char.ToUpper(name[0]).ToString() : "?";
        }

        public List<AppUser> KnownUsers(IEnumerable<string> ids)
        {
            var list = new List<AppUser>();
            foreach (var id in ids.Distinct())
            {
                var user = _users.GetById(id);
                if (user != null) list.Add(user);
            }
            return list;
        }

        public bool HasUnseenProfileChanges => ProtectionEnabled && IsAdmin && _users.UnseenLogCount > 0;
    }
}
