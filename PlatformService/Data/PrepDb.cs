

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PlatformService.Models;

namespace PlatformService.Data
{
    public static class PrepDb
    {
        public static void PrepopulateDb(IApplicationBuilder app, bool isProd)
        {
          
            using var scope = app.ApplicationServices.CreateScope();
            var service = scope.ServiceProvider.GetService<IPlatformRepo>();

              if(isProd)
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                try
                {
                    Console.WriteLine("--> Attempting to apply migrations");
                    context.Database.Migrate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"--> Error occured Attempting to apply migrations {ex.Message}");
                }
                
            }

            if (service == null)
            {
                throw new InvalidOperationException("IPlatformRepo service is not registered or failed to resolve.");
            }
            var platforms = service.GetAllPlatforms();
            if (!platforms.Any())
            {
                Console.WriteLine("Seeding data...");
                service.CreatePlatform(new Platform { Name = "Dotnet", Publisher = "Microsoft", Cost = "Free" });
                service.CreatePlatform(new Platform { Name = "SQL Server", Publisher = "Microsoft", Cost = "Free" });
                service.CreatePlatform(new Platform { Name = "Redis", Publisher = "Microsoft", Cost = "Free" });
            }
            service.SaveChanges();
        }
    }
}

