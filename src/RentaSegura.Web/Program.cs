// ============================================================================
//  Program.cs  —  Composición de la aplicación.
//
//  Mejoras de criterio técnico incorporadas:
//    - Rate limiting en endpoints críticos (login, KYC, favoritos).
//    - Bloqueo de cuenta por intentos fallidos (en AccountController).
//    - Health checks en /health (BD + bóveda de documentos).
//    - Interceptor de auditoría automática (timestamps + usuario).
//    - Headers de seguridad HTTP (X-Frame-Options, X-Content-Type, etc.).
// ============================================================================

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RentaSegura.Web.Domain;
using RentaSegura.Web.Infrastructure;
using RentaSegura.Web.Infrastructure.HealthChecks;
using RentaSegura.Web.Infrastructure.Interceptors;
using RentaSegura.Web.Infrastructure.Security;
using RentaSegura.Web.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// -- MVC + Razor 
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddHttpContextAccessor();

// -- Rate Limiting 
// Protege endpoints sensibles contra fuerza bruta y abuso.
// "auth": 5 intentos/minuto por IP en Login y Register.
// "api": 20 req/minuto en endpoints AJAX (Favorites, Notifications).
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit       = 0;
    });

    options.AddFixedWindowLimiter("api", o =>
    {
        o.PermitLimit      = 20;
        o.Window           = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit       = 2;
    });

    // Respuesta estándar cuando se supera el límite.
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await ctx.HttpContext.Response.WriteAsync(
            "Demasiadas solicitudes. Espera un momento e intenta de nuevo.", ct);
    };
});

// -- Base de datos 
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Default'.");

// El interceptor de auditoría se registra ANTES de AddDbContext.
builder.Services.AddScoped<AuditInterceptor>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseNpgsql(connectionString)
           .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

// -- Identity 
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount  = false;
        options.Password.RequiredLength         = 8;
        options.User.RequireUniqueEmail         = true;

        // Bloqueo de cuenta: 5 intentos fallidos → 5 minutos sin acceso.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(5);
        options.Lockout.AllowedForNewUsers      = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath       = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan  = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// -- Opciones 
var smtpOptions  = builder.Configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>()  ?? new SmtpOptions();
var vaultOptions = builder.Configuration.GetSection(DocumentVaultOptions.SectionName).Get<DocumentVaultOptions>() ?? new DocumentVaultOptions();
builder.Services.AddSingleton(smtpOptions);
builder.Services.AddSingleton(vaultOptions);

// -- Health Checks 
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql", tags: new[] { "db" })
    .AddCheck<VaultHealthCheck>("document_vault", tags: new[] { "storage" });

// -- Servicios de aplicación 
builder.Services.AddSingleton<IEmailSender,                 SmtpEmailSender>();
builder.Services.AddSingleton<IIdentityVerificationService, StubIdentityVerificationService>();
builder.Services.AddSingleton<IDocumentVault,               AesDocumentVault>();

builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<PropertyService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<FavoriteService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<ProfileService>();

var app = builder.Build();

// -- Pipeline HTTP 
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Headers de seguridad HTTP: protegen contra XSS, clickjacking y sniffing.
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options",        "SAMEORIGIN");
    ctx.Response.Headers.Append("Referrer-Policy",        "strict-origin-when-cross-origin");
    ctx.Response.Headers.Append("Permissions-Policy",     "camera=(), microphone=(), geolocation=()");
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Health check accesible en /health (para Docker, Kubernetes, monitoreo).
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status   = report.Status.ToString(),
            checks   = report.Entries.Select(e => new
            {
                name        = e.Key,
                status      = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
        await ctx.Response.WriteAsync(result);
    }
});

// Rutas con límite de tasa en los endpoints sensibles.
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// Rate limiting en rutas de autenticación y KYC.
app.MapControllerRoute("login", "Account/Login",new { controller = "Account",       action = "Login"  })
   .RequireRateLimiting("auth");
app.MapControllerRoute("register", "Account/Register", new { controller = "Account",       action = "Register" })
   .RequireRateLimiting("auth");
app.MapControllerRoute("kyc", "Kyc/Upload", new { controller = "Kyc",           action = "Upload" })
   .RequireRateLimiting("auth");
app.MapControllerRoute("favtoggle","Favorites/Toggle",       new { controller = "Favorites",     action = "Toggle" })
   .RequireRateLimiting("api");
app.MapControllerRoute("notif",    "Notifications/MarkAllRead", new { controller = "Notifications", action = "MarkAllRead" })
   .RequireRateLimiting("api");

// -- Inicialización de la BD 
await DbInitializer.InitializeAsync(app);

app.Run();