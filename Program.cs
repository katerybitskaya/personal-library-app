using PersonalLibrary.Filters;
using PersonalLibrary.Interfaces;
using PersonalLibrary.Patterns;
using PersonalLibrary.Repositories;
using PersonalLibrary.Services;

namespace PersonalLibrary
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<TrashCountFilter>();
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.AddService<TrashCountFilter>();
            });

            builder.Services.AddScoped<LocalizationService>();

            var dataDirectory = builder.Configuration["LibrarySettings:DataDirectory"] ?? "Data";
            var fullDataPath = Path.Combine(builder.Environment.ContentRootPath, dataDirectory);

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


            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
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
