using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using PersonalLibrary.Filters;
using PersonalLibrary.Interfaces;
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
            builder.Services.AddScoped<TrashCountFilter>();
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.AddService<TrashCountFilter>();
            });

            builder.Services.AddScoped<LocalizationService>();

            var dataDirectory = builder.Configuration["LibrarySettings:DataDirectory"] ?? "Data";
            var fullDataPath = Path.Combine(builder.Environment.ContentRootPath, dataDirectory);

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
            builder.Services.AddSingleton<TrashService>();
            builder.Services.AddSingleton<LibraryService>();

            var app = builder.Build();

            var publisher = app.Services.GetRequiredService<LibraryEventPublisher>();
            var historyService = app.Services.GetRequiredService<HistoryService>();
            publisher.Subscribe(historyService);

            app.Logger.LogInformation("Password protection: {State}",
                app.Configuration.GetValue<bool>("Protection:Enabled") ? "ON" : "OFF");


            app.UseForwardedHeaders();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseMiddleware<ProtectionMiddleware>();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();

        }
    }
}
