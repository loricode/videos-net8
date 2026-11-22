using DomainCore.Constants;
using InfrastructureCore.Persistence.EF;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebApiCore
{
    /// <summary>
    /// Arranque de la base de datos:
    /// 1. Aplica migraciones con reintentos (Database:AutoMigrate, por defecto true) o
    ///    EnsureCreated (Database:EnsureCreated, usado en pruebas con SQLite).
    /// 2. Crea los roles Instructor y Student.
    /// 3. Asigna Instructor a usuarios previos sin rol (compatibilidad con datos existentes).
    /// 4. Crea el instructor semilla (Seed:AdminEmail / Seed:AdminPassword / Seed:AdminName).
    /// </summary>
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services, IConfiguration config, ILogger logger)
        {
            var autoMigrate = config.GetValue("Database:AutoMigrate", true);
            var ensureCreated = config.GetValue("Database:EnsureCreated", false);
            if (!autoMigrate && !ensureCreated) return;

            const int maxAttempts = 20;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using var scope = services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    if (ensureCreated) await db.Database.EnsureCreatedAsync();
                    else await db.Database.MigrateAsync();
                    logger.LogInformation("Base de datos lista.");

                    await EnsureRolesAsync(scope.ServiceProvider, logger);
                    await SeedAdminAsync(scope.ServiceProvider, config, logger);
                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    logger.LogWarning("BD no disponible (intento {Attempt}/{Max}): {Message}", attempt, maxAttempts, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }

        private static async Task EnsureRolesAsync(IServiceProvider sp, ILogger logger)
        {
            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            foreach (var role in new[] { Roles.Instructor, Roles.Student })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }

            // Usuarios creados antes de existir los roles eran instructores
            var db = sp.GetRequiredService<AppDbContext>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var withoutRole = await db.Users
                .Where(u => !db.UserRoles.Any(r => r.UserId == u.Id))
                .ToListAsync();

            foreach (var user in withoutRole)
                await userManager.AddToRoleAsync(user, Roles.Instructor);

            if (withoutRole.Count > 0)
                logger.LogInformation("{Count} usuarios existentes recibieron el rol Instructor.", withoutRole.Count);
        }

        private static async Task SeedAdminAsync(IServiceProvider sp, IConfiguration config, ILogger logger)
        {
            var email = config["Seed:AdminEmail"];
            var password = config["Seed:AdminPassword"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            if (await userManager.FindByEmailAsync(email) is not null) return;

            var user = new ApplicationUser
            {
                Email = email,
                UserName = email,
                FullName = config["Seed:AdminName"] ?? "Administrador"
            };

            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, Roles.Instructor);
                logger.LogInformation("Instructor semilla {Email} creado.", email);
            }
            else
            {
                logger.LogWarning("No se pudo crear el usuario semilla: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
