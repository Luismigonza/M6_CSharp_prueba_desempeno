using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentaSegura.Web.Domain;
using RentaSegura.Web.Infrastructure;
using RentaSegura.Web.Infrastructure.Security;
using RentaSegura.Web.Services;

namespace RentaSegura.Web.Controllers;

[Authorize]
public sealed class KycController : Controller
{
    private readonly AppDbContext _db;
    private readonly IIdentityVerificationService _verifier;
    private readonly IDocumentVault _vault;
    private readonly NotificationService _notifications;

    public KycController(AppDbContext db, IIdentityVerificationService verifier,
        IDocumentVault vault, NotificationService notifications)
    {
        _db = db;
        _verifier = verifier;
        _vault = vault;
        _notifications = notifications;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    
    private string UserEmail => User.Identity?.Name ?? string.Empty;

    // estado actual + formulario de carga.
    public async Task<IActionResult> Index()
    {
        var verification = await _db.KycVerifications.AsNoTracking()
            .Where(k => k.UserId == UserId)
            .OrderByDescending(k => k.CreatedAtUtc)
            .FirstOrDefaultAsync();
        return View(verification);
    }

    // procesa el documento.
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(10_000_000)] 
    public async Task<IActionResult> Upload(IFormFile? document)
    {
        if (document is null || document.Length == 0)
        {
            TempData["Error"] = "Selecciona una imagen de tu documento.";
            return RedirectToAction(nameof(Index));
        }

        // 1) Leer bytes.
        using var ms = new MemoryStream();
        await document.CopyToAsync(ms);
        var bytes = ms.ToArray();

        // 2) Cifrar y guardar en la bóveda.
        var handle = await _vault.StoreAsync(bytes);

        // 3) Validar con IA (extracción + veredicto).
        var result = await _verifier.VerifyAsync(bytes, document.ContentType);

        // 4) Eliminar de forma segura el documento (no se conserva la imagen).
        await _vault.SecureDeleteAsync(handle);

        // 5) Guardar SOLO el resultado.
        var verification = new KycVerification
        {
            UserId = UserId,
            FirstName = result.FirstName,
            LastName = result.LastName,
            DocumentNumber = result.DocumentNumber,
            BirthDate = result.BirthDate,
            Status = result.Approved ? KycStatus.Approved : KycStatus.Rejected,
            RejectionReason = result.RejectionReason,
            DocumentSecurelyDeletedAtUtc = DateTime.UtcNow
        };
        _db.KycVerifications.Add(verification);
        await _db.SaveChangesAsync();

        // Notificación del veredicto.
        await _notifications.NotifyAsync(UserId, UserEmail,
            result.Approved ? "Identidad verificada" : "Validación de identidad rechazada",
            result.Approved
                ? "Tu identidad fue verificada. Ya puedes completar reservas."
                : $"No pudimos validar tu identidad: {result.RejectionReason}");

        TempData[result.Approved ? "Success" : "Error"] = result.Approved
            ? "¡Identidad verificada con éxito!"
            : result.RejectionReason;

        return RedirectToAction(nameof(Index));
    }
}
