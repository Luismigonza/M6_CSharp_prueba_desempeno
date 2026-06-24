using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentaSegura.Web.Domain;

namespace RentaSegura.Web.Infrastructure;

public static class DbInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var db = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        // Reintento simple: la BD del contenedor puede tardar en aceptar conexiones.
        for (var intento = 1; intento <= 10; intento++)
        {
            try { await db.Database.EnsureCreatedAsync(); break; }
            catch (Exception ex) when (intento < 10)
            {
                logger.LogWarning("BD no lista (intento {Intento}): {Mensaje}", intento, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        // EXCLUDE constraint anti-double-booking (idempotente).
        await db.Database.ExecuteSqlRawAsync("""
            CREATE EXTENSION IF NOT EXISTS btree_gist;
            DO $$
            BEGIN
              IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_no_double_booking') THEN
                ALTER TABLE "Reservations"
                  ADD CONSTRAINT ck_no_double_booking
                  EXCLUDE USING gist (
                    "PropertyId" WITH =,
                    daterange("CheckInDate", "CheckOutDate", '[)') WITH &&
                  )
                  WHERE ("Status" <> 'Cancelled');
              END IF;
            END $$;
            """);

        // Roles.
        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // Usuarios de ejemplo.
        var anfitrion = await EnsureUserAsync(userManager, logger,
            "anfitrion@rentasegura.local", "Ana Anfitriona", "Anfitrion123*", Roles.Anfitrion);
        await EnsureUserAsync(userManager, logger,
            "huesped@rentasegura.local", "Hugo Huésped", "Huesped123*", Roles.Huesped);

        // Inmuebles demo (solo si la tabla está vacía).
        if (anfitrion is not null && !await db.Properties.AnyAsync())
        {
            db.Properties.AddRange(
                new Property
                {
                    OwnerId = anfitrion.Id, Title = "Apartamento con vista al Poblado",
                    Description = "Acogedor apartamento en el corazón de El Poblado, ideal para viajes de trabajo o placer.",
                    City = "Medellín", Address = "Cra 35 #7-50", PricePerNight = 220000, Bedrooms = 2, Capacity = 4,
                    ImageUrl = "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800"
                },
                new Property
                {
                    OwnerId = anfitrion.Id, Title = "Loft moderno en Laureles",
                    Description = "Loft luminoso a pocos pasos de la Primera de Laureles, con todas las comodidades.",
                    City = "Medellín", Address = "Cll 9 #8-7", PricePerNight = 180000, Bedrooms = 1, Capacity = 2,
                    ImageUrl = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800"
                },
                new Property
                {
                    OwnerId = anfitrion.Id, Title = "Casa campestre en Llanogrande",
                    Description = "Amplia casa con jardín y zona BBQ, perfecta para grupos y familias.",
                    City = "Rionegro", Address = "Vereda Llanogrande km 5", PricePerNight = 450000, Bedrooms = 4, Capacity = 8,
                    ImageUrl = "https://images.unsplash.com/photo-1568605114967-8130f3a36994?w=800"
                });
            await db.SaveChangesAsync();
            logger.LogInformation("Inmuebles demo sembrados.");
        }

        logger.LogInformation("Inicialización completada.");
    }

    private static async Task<ApplicationUser?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager, ILogger logger,
        string email, string fullName, string password, string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null) return existing;

        var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
            logger.LogInformation("Usuario semilla: {Email} ({Role})", email, role);
            return user;
        }

        logger.LogError("No se pudo crear {Email}: {Errores}", email,
            string.Join(" ", result.Errors.Select(e => e.Description)));
        return null;
    }
}
