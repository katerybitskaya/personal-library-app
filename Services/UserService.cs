using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using PersonalLibrary.Models;
using PersonalLibrary.Repositories;

namespace PersonalLibrary.Services
{
    public class UserService
    {
        public const int MinPasswordLength = 6;
        private const int MaxNameLength = 40;
        private static readonly Regex LoginPattern = new(@"^[\p{L}\p{N}._-]{3,32}$");
        private static readonly PasswordHasher<object> Hasher = new();
        private static readonly string DummyHash = Hasher.HashPassword(new object(), Guid.NewGuid().ToString());

        private readonly JsonUserRepository _repository;

        public UserService(JsonUserRepository repository) => _repository = repository;

        public List<AppUser> GetAll() =>
            _repository.GetAll().OrderByDescending(u => u.IsAdmin).ThenBy(u => u.CreatedAt).ToList();

        public AppUser? GetById(string? id) => _repository.GetById(id);

        public AppUser? Admin => _repository.GetById(AppUser.AdminId);

        public bool AdminHasPassword => !string.IsNullOrEmpty(Admin?.PasswordHash);

        public void EnsureAdmin(ProtectionSettings settings)
        {
            var hash = !string.IsNullOrWhiteSpace(settings.PasswordHash) ? settings.PasswordHash.Trim()
                : !string.IsNullOrEmpty(settings.Password) ? Hash(settings.Password)
                : string.Empty;

            var admin = Admin;
            if (admin == null)
            {
                _repository.Add(new AppUser
                {
                    Id = AppUser.AdminId,
                    Login = string.IsNullOrWhiteSpace(settings.Username) ? "admin" : settings.Username.Trim(),
                    IsAdmin = true,
                    Color = UserColors.Default,
                    PasswordHash = hash
                });
                return;
            }
            if (string.IsNullOrEmpty(admin.PasswordHash) && hash.Length > 0)
            {
                admin.PasswordHash = hash;
                _repository.Save();
            }
        }

        public AppUser? Verify(string? login, string? password)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password)) return null;
            var user = _repository.GetByLogin(login);
            var hash = string.IsNullOrEmpty(user?.PasswordHash) ? DummyHash : user!.PasswordHash;
            bool ok;
            try { ok = Hasher.VerifyHashedPassword(new object(), hash, password) != PasswordVerificationResult.Failed; }
            catch (FormatException) { ok = false; }
            return ok && user != null && !string.IsNullOrEmpty(user.PasswordHash) ? user : null;
        }

        public string? UpdateOwnProfile(string userId, string? name, string? color, string? login)
        {
            var user = _repository.GetById(userId);
            if (user == null) return "Error_NotFound";
            var cleanName = CleanName(name);
            if (cleanName?.Length > MaxNameLength) return "User_NameTooLong";
            var loginChanged = user.IsAdmin && login != null && login.Trim() != user.Login;
            var error = ValidateColor(color, user.Id) ?? (loginChanged ? ValidateLogin(login, user.Id) : null);
            if (error != null) return error;

            var log = new List<ProfileLogEntry>();
            if (cleanName != user.DisplayName)
                log.Add(new ProfileLogEntry { UserId = user.Id, Field = "name", OldValue = user.DisplayName, NewValue = cleanName });
            if (color != user.Color)
                log.Add(new ProfileLogEntry { UserId = user.Id, Field = "color", OldValue = user.Color, NewValue = color });

            user.DisplayName = cleanName;
            user.Color = color!;
            if (loginChanged)
            {
                user.Login = login!.Trim();
                user.SecurityStamp = NewStamp();
            }
            if (!user.IsAdmin) _repository.AddLog(log);
            _repository.Save();
            return null;
        }

        public string? ChangeOwnPassword(string userId, string? current, string? next, string? repeat)
        {
            var user = _repository.GetById(userId);
            if (user == null) return "Error_NotFound";
            if (Verify(user.Login, current) == null) return "User_PasswordWrong";
            var error = ValidatePassword(next, repeat);
            if (error != null) return error;

            SetPassword(user, next!);
            if (!user.IsAdmin)
                _repository.AddLog(new[] { new ProfileLogEntry { UserId = user.Id, Field = "password" } });
            _repository.Save();
            return null;
        }

        public string? CreateUser(string? name, string? login, string? password, string? color)
        {
            var cleanName = CleanName(name);
            if (cleanName?.Length > MaxNameLength) return "User_NameTooLong";
            var error = ValidateLogin(login, null) ?? ValidatePassword(password, password) ?? ValidateColor(color, null);
            if (error != null) return error;

            var user = new AppUser { Login = login!.Trim(), DisplayName = cleanName, Color = color! };
            SetPassword(user, password!);
            _repository.Add(user);
            return null;
        }

        public string? UpdateUser(string id, string? name, string? login, string? color, string? newPassword)
        {
            var user = _repository.GetById(id);
            if (user == null) return "Error_NotFound";
            var cleanName = CleanName(name);
            if (cleanName?.Length > MaxNameLength) return "User_NameTooLong";
            var error = ValidateLogin(login, user.Id) ?? ValidateColor(color, user.Id)
                ?? (string.IsNullOrEmpty(newPassword) ? null : ValidatePassword(newPassword, newPassword));
            if (error != null) return error;

            user.DisplayName = cleanName;
            user.Color = color!;
            if (login!.Trim() != user.Login)
            {
                user.Login = login.Trim();
                user.SecurityStamp = NewStamp();
            }
            if (!string.IsNullOrEmpty(newPassword)) SetPassword(user, newPassword);
            _repository.Save();
            return null;
        }

        public string? DeleteUser(string id)
        {
            var user = _repository.GetById(id);
            if (user == null) return "Error_NotFound";
            if (user.IsAdmin) return "Error_Forbidden";
            _repository.Remove(id);
            return null;
        }

        public void ResetAdminPassword(string password)
        {
            var admin = Admin ?? throw new InvalidOperationException("Admin account not found.");
            SetPassword(admin, password);
            _repository.Save();
        }

        public List<ProfileLogEntry> GetLog(int take) => _repository.GetLog().Take(take).ToList();

        public DateTime? AdminLogSeenAt => _repository.AdminLogSeenAt;

        public int UnseenLogCount
        {
            get
            {
                var seen = _repository.AdminLogSeenAt;
                return _repository.GetLog().Count(e => seen == null || e.Timestamp > seen);
            }
        }

        public void MarkLogSeen()
        {
            if (UnseenLogCount == 0) return;
            _repository.AdminLogSeenAt = DateTime.Now;
            _repository.Save();
        }

        public string? ColorOwner(string color, string? exceptId) =>
            _repository.GetAll().FirstOrDefault(u => u.Color == color && u.Id != exceptId)?.Id;

        private string? ValidateLogin(string? login, string? exceptId)
        {
            var trimmed = login?.Trim() ?? string.Empty;
            if (trimmed.Length == 0) return "User_LoginRequired";
            if (!LoginPattern.IsMatch(trimmed)) return "User_LoginInvalid";
            var other = _repository.GetByLogin(trimmed);
            return other != null && other.Id != exceptId ? "User_LoginTaken" : null;
        }

        private static string? ValidatePassword(string? password, string? repeat)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength) return "User_PasswordShort";
            return password != repeat ? "User_PasswordMismatch" : null;
        }

        public bool HasFreeColor => UserColors.Palette.Any(p => ColorOwner(p.Key, null) == null);

        private string? ValidateColor(string? color, string? exceptId)
        {
            if (string.IsNullOrEmpty(color) && exceptId == null && !HasFreeColor) return "User_NoFreeColor";
            if (!UserColors.IsValid(color)) return "Error_Validation";
            return ColorOwner(color!, exceptId) != null ? "User_ColorTaken" : null;
        }

        private static void SetPassword(AppUser user, string password)
        {
            user.PasswordHash = Hash(password);
            user.PasswordChangedAt = DateTime.Now;
            user.SecurityStamp = NewStamp();
        }

        private static string Hash(string password) => Hasher.HashPassword(new object(), password);

        private static string NewStamp() => Guid.NewGuid().ToString("N");

        private static string? CleanName(string? name)
        {
            var trimmed = name?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }
    }
}
