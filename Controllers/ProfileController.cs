using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PersonalLibrary.Models;
using PersonalLibrary.Security;
using PersonalLibrary.Services;
using PersonalLibrary.ViewModels;
using System.Security.Claims;

namespace PersonalLibrary.Controllers
{
    public class ProfileController : Controller
    {
        private const int LogSize = 50;

        private readonly UserService _users;
        private readonly CurrentUserService _me;
        private readonly LibraryService _library;
        private readonly LocalizationService _loc;

        public ProfileController(UserService users, CurrentUserService me, LibraryService library, LocalizationService loc)
        {
            _users = users;
            _me = me;
            _library = library;
            _loc = loc;
        }

        public IActionResult Index()
        {
            var user = _me.User;
            if (!_me.ProtectionEnabled || user == null) return RedirectToAction("Index", "Home");

            var vm = new ProfileViewModel
            {
                User = user,
                IsAdmin = user.IsAdmin,
                Users = user.IsAdmin ? _users.GetAll() : new List<AppUser>(),
                Log = user.IsAdmin ? _users.GetLog(LogSize) : new List<ProfileLogEntry>(),
                LogSeenAt = _users.AdminLogSeenAt
            };
            if (user.IsAdmin) _users.MarkLogSeen();
            return View(vm);
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string? displayName, string? color, string? login)
        {
            var user = _me.User;
            if (user == null) return Error("Error_NotFound");
            var stamp = user.SecurityStamp;
            var error = _users.UpdateOwnProfile(user.Id, displayName, color, user.IsAdmin ? login : null);
            if (error != null) return Error(error);
            if (user.SecurityStamp != stamp) await RefreshSignIn(user);
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string? currentPassword, string? newPassword, string? repeatPassword)
        {
            var user = _me.User;
            if (user == null) return Error("Error_NotFound");
            var error = _users.ChangeOwnPassword(user.Id, currentPassword, newPassword, repeatPassword);
            if (error != null) return Error(error);
            await RefreshSignIn(user);
            return Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult AddUser(string? displayName, string? login, string? password, string? color)
        {
            if (!_me.IsAdmin) return Error("Error_Forbidden");
            var error = _users.CreateUser(displayName, login, password, color);
            return error != null ? Error(error) : Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult EditUser(string id, string? displayName, string? login, string? color, string? newPassword)
        {
            if (!_me.IsAdmin || id == _me.Id) return Error("Error_Forbidden");
            var error = _users.UpdateUser(id, displayName, login, color, newPassword);
            return error != null ? Error(error) : Json(new { success = true });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public IActionResult DeleteUser(string id)
        {
            if (!_me.IsAdmin) return Error("Error_Forbidden");
            var error = _users.DeleteUser(id);
            if (error != null) return Error(error);
            _library.RemoveFavoritesOf(id);
            return Json(new { success = true });
        }

        private async Task RefreshSignIn(AppUser user)
        {
            var current = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var identity = new ClaimsIdentity(ProtectionHelper.ClaimsFor(user), CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity), current.Properties ?? new AuthenticationProperties());
        }

        private JsonResult Error(string key) => Json(new { success = false, message = _loc[key] });
    }
}
