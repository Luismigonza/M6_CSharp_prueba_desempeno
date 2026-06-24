using Microsoft.EntityFrameworkCore;
using Npgsql;
using RentaSegura.Web.Domain;
using RentaSegura.Web.Infrastructure;

namespace RentaSegura.Web.Services;

public sealed class ReservationService
{
    private readonly AppDbContext        _db;
    private readonly NotificationService _notifications;

    public ReservationService(AppDbContext db, NotificationService notifications)
    {
        _db            = db;
        _notifications = notifications;
    }

    public Task<bool> HasApprovedKycAsync(string userId, CancellationToken ct = default) =>
        _db.KycVerifications.AnyAsync(k => k.UserId == userId && k.Status == KycStatus.Approved, ct);
    
    //  Crear reserva
    public async Task<(Reservation? Reservation, string? Error)> CreateAsync(
        Guid propertyId, string guestUserId, string guestEmail,
        DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default)
    {
        if (!await HasApprovedKycAsync(guestUserId, ct))
            return (null, "Debes completar la validación de identidad (KYC) antes de reservar.");

        var property = await _db.Properties.FirstOrDefaultAsync(p => p.Id == propertyId, ct);
        if (property is null) return (null, "El inmueble no existe.");

        var (reservation, error) = Reservation.Create(property, guestUserId, checkIn, checkOut);
        if (error is not null) return (null, error);
        
        var overlaps = await _db.Reservations.AnyAsync(r =>
            r.PropertyId == propertyId &&
            r.Status != ReservationStatus.Cancelled &&
            r.CheckInDate < checkOut && r.CheckOutDate > checkIn, ct);
        if (overlaps)
            return (null, "Esas fechas ya no están disponibles para este inmueble.");

        _db.Reservations.Add(reservation!);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23P01" })
        {
            _db.Entry(reservation!).State = EntityState.Detached;
            return (null, "Esas fechas acaban de ser reservadas por otra persona. Intenta con otras.");
        }

        await _notifications.NotifyAsync(guestUserId, guestEmail,
            "Reserva confirmada ✓",
            $"Tu reserva en '{property.Title}' está confirmada del " +
            $"{checkIn:dd/MM/yyyy} al {checkOut:dd/MM/yyyy}. " +
            $"Check-in 2:00 PM · Check-out 12:00 PM · Total: {reservation!.PricePaid:C0}.", ct);

        return (reservation, null);
    }
    
    //  Cancelar reserva
    public async Task<(bool Ok, string? Error)> CancelAsync(
        Guid reservationId, string guestUserId, string guestEmail, CancellationToken ct = default)
    {
        var reservation = await _db.Reservations
            .Include(r => r.Property)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.GuestUserId == guestUserId, ct);

        if (reservation is null)
            return (false, "Reserva no encontrada.");

        var (allowed, reason) = reservation.CanCancel();
        if (!allowed)
            return (false, reason);

        reservation.Status = ReservationStatus.Cancelled;
        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(guestUserId, guestEmail,
            "Reserva cancelada",
            $"Tu reserva en '{reservation.Property?.Title}' " +
            $"del {reservation.CheckInDate:dd/MM/yyyy} ha sido cancelada.", ct);

        return (true, null);
    }
    
    //  Consultas
    public async Task<List<Reservation>> ListByGuestAsync(string guestUserId, CancellationToken ct = default) =>
        await _db.Reservations.AsNoTracking()
            .Include(r => r.Property)
            .Where(r => r.GuestUserId == guestUserId)
            .OrderByDescending(r => r.CheckInDate)
            .ToListAsync(ct);
}