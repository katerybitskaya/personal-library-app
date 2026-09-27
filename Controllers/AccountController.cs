using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PersonalLibrary.Models;
using PersonalLibrary.Security;
using PersonalLibrary.Services;
using PersonalLibrary.ViewModels;
using System.Security.Claims;

namespace PersonalLibrary.Controllers
{
    public class AccountController : Controller
    {
        private readonly IOptionsMonitor<ProtectionSettings> _options;
        private readonly LoginAttemptTracker _tracker;
        private readonly LocalizationService _l;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IOptionsMonitor<ProtectionSettings> options,
            LoginAttemptTracker tracker,
            LocalizationService l,
            ILogger<AccountController> logger)
        {
            _options = options;
            _tracker = tracker;
            _l = l;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            var settings = _options.CurrentValue;
            if (!settings.Enabled || User.Identity?.IsAuthenticated == true)
                return RedirectToLocal(returnUrl);

            var vm = new LoginViewModel { ReturnUrl = returnUrl };
            if (!settings.HasCredentials)
                vm.Error = _l["Login_NotConfigured"];
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            var settings = _options.CurrentValue;
            if (!settings.Enabled)
                return RedirectToLocal(vm.ReturnUrl);

            if (!settings.HasCredentials)
            {
                vm.Error = _l["Login_NotConfigured"];
                return View(vm);
            }

            var ip = ProtectionHelper.GetClientIp(HttpContext);

            var lockout = _tracker.GetLockout(ip);
            if (lockout != null)
            {
                vm.Error = string.Format(_l["Login_LockedOut"], Math.Max(1, (int)Math.Ceiling(lockout.Value.TotalMinutes)));
                vm.Password = null;
                return View(vm);
            }

            if (!ProtectionHelper.VerifyCredentials(settings, vm.Username, vm.Password))
            {
                _tracker.RegisterFailure(ip, settings.MaxFailedAttempts, settings.GlobalMaxFailedAttempts, settings.LockoutMinutes);
                _logger.LogWarning("Failed login attempt from {Ip}", ip);

                lockout = _tracker.GetLockout(ip);
                vm.Error = lockout != null
                    ? string.Format(_l["Login_LockedOut"], Math.Max(1, (int)Math.Ceiling(lockout.Value.TotalMinutes)))
                    : _l["Login_Invalid"];
                vm.Password = null;
                return View(vm);
            }

            _tracker.RegisterSuccess(ip);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, settings.Username.Trim()),
                new(ProtectionHelper.StampClaim, ProtectionHelper.GetStamp(settings)),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = vm.RememberMe,
                    ExpiresUtc = vm.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(Math.Max(1, settings.SessionDays))
                        : null,
                });

            return RedirectToLocal(vm.ReturnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return _options.CurrentValue.Enabled
                ? RedirectToAction(nameof(Login))
                : RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToLocal(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction("Index", "Home");
    }
}
