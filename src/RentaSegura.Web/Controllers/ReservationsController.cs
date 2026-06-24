using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentaSegura.Web.Services;

namespace RentaSegura.Web.Controllers;

[Authorize]
public sealed class ReservationsController : Controller
{
    private readonly ReservationService _reservations;
    public ReservationsController(ReservationService reservations) => _reservations = reservations;

    private string UserId    => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string UserEmail => User.Identity?.Name ?? string.Empty;

    // POST /Reservations/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid propertyId, DateOnly checkIn, DateOnly checkOut)
    {
        if (!await _reservations.HasApprovedKycAsync(UserId))
        {
            TempData["Error"] = "Primero debes validar tu identidad (KYC).";
            return RedirectToAction("Index", "Kyc");
        }

        var (reservation, error) = await _reservations.CreateAsync(
            propertyId, UserId, UserEmail, checkIn, checkOut);

        if (error is not null)
            return RedirectToAction("Details", "Home", new { id = propertyId, checkIn, checkOut, message = error });

        TempData["Success"] = "¡Reserva confirmada! Revisa tu correo y tus notificaciones.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Reservations/Cancel/{id}
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var (ok, error) = await _reservations.CancelAsync(id, UserId, UserEmail);

        TempData[ok ? "Success" : "Error"] = ok
            ? "Reserva cancelada correctamente."
            : error;

        return RedirectToAction(nameof(Index));
    }

    // GET /Reservations
    public async Task<IActionResult> Index()
        => View(await _reservations.ListByGuestAsync(UserId));
}