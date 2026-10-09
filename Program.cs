using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using PersonalLibrary.Interfaces;
using PersonalLibrary.Middleware;
using PersonalLibrary.Models;
using PersonalLibrary.Patterns;
using PersonalLibrary.Repositories;
using PersonalLibrary.Security;
using PersonalLibrary.Services;

namespace PersonalLibrary
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Contains(PasswordTool.Argument))
            {
                PasswordTool.Run();
                return;
            }

            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "protection.json"), optional: true, reloadOnChange: true);
            builder.Configuration.AddJsonFile("protection.json", optional: true, reloadOnChange: true);
            builder.Services.Configure<ProtectionSettings>(
                builder.Configuration.GetSection(ProtectionSettings.SectionName));
            builder.Services.AddSingleton<LoginAttemptTracker>();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddControllersWithViews();

            builder.Services.AddScoped<LocalizationService>();

            var librarySettings = builder.Configuration.GetSection("LibrarySettings").Get<LibrarySettings>() ?? new LibrarySettings();
            var fullDataPath = Path.Combine(builder.Environment.ContentRootPath, librarySettings.DataDirectory);

            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(fullDataPath, "keys")));

            var sessionDays = builder.Configuration.GetValue<int?>("Protection:SessionDays") ?? 30;
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = ".PersonalLibrary.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.ExpireTimeSpan = TimeSpan.FromDays(Math.Max(1, sessionDays));
                    options.SlidingExpiration = true;
                });

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
            });

            builder.Services.AddSingleton<ILibraryRepository>(_ => new JsonLibraryRepository(fullDataPath));
            builder.Services.AddSingleton(_ => new JsonTrashRepository(fullDataPath));
            builder.Services.AddSingleton(_ => new JsonHistoryRepository(fullDataPath));

            builder.Services.AddSingleton<LibraryEventPublisher>();
            builder.Services.AddSingleton<HistoryService>();
            builder.Services.AddSingleton<UploadCleanupService>();
            builder.Services.AddSingleton<TrashService>();
            builder.Services.AddSingleton<LibraryService>();

            var app = builder.Build();

            var publisher = app.Services.GetRequiredService<LibraryEventPublisher>();
            var historyService = app.Services.GetRequiredService<HistoryService>();
            publisher.Subscribe(historyService);

            app.Logger.LogInformation("Password protection: {State}",
                app.Configuration.GetValue<bool>("Protection:Enabled") ? "ON" : "OFF");

            var storageErrors = new[]
            {
                app.Services.GetRequiredService<ILibraryRepository>().LoadError,
                app.Services.GetRequiredService<JsonTrashRepository>().LoadError,
                app.Services.GetRequiredService<JsonHistoryRepository>().LoadError
            }.Where(e => e != null);
            foreach (var error in storageErrors)
                app.Logger.LogCritical("Data storage error: {Error}. The site shows an error page and saving is disabled.", error);


            app.UseForwardedHeaders();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/Home/StatusPage", "?code={0}");

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseMiddleware<ProtectionMiddleware>();
            app.UseStaticFiles();
            app.UseMiddleware<StorageGuardMiddleware>();
            app.UseMiddleware<RequestLockMiddleware>();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();

        }
    }
}
