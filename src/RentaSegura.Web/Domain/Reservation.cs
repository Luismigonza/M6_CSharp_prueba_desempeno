namespace RentaSegura.Web.Domain;

public sealed class Reservation : IAuditable
{
    public static readonly TimeOnly StandardCheckIn  = new(14, 0);
    public static readonly TimeOnly StandardCheckOut = new(12, 0);

    // Política de cancelación: mínimo 48 h de antelación.
    private static readonly TimeSpan CancellationWindow = TimeSpan.FromHours(48);

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }
    public string GuestUserId { get; set; } = default!;

    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int Nights { get; set; }
    public decimal PricePaid { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? LastModifiedBy { get; set; }

    public DateTime CheckInDateTime => CheckInDate.ToDateTime(StandardCheckIn);
    public DateTime CheckOutDateTime => CheckOutDate.ToDateTime(StandardCheckOut);
    
    public static (Reservation? Reservation, string? Error) Create(
        Property property, string guestUserId, DateOnly checkIn, DateOnly checkOut)
    {
        if (checkOut <= checkIn)
            return (null, "La fecha de salida debe ser posterior a la de llegada.");
        if (checkIn < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            return (null, "La fecha de llegada no puede estar en el pasado.");

        var nights = checkOut.DayNumber - checkIn.DayNumber;
        return (new Reservation
        {
            PropertyId  = property.Id,
            GuestUserId = guestUserId,
            CheckInDate  = checkIn,
            CheckOutDate = checkOut,
            Nights       = nights,
            PricePaid    = nights * property.PricePerNight
        }, null);
    }
    
    //  Política de cancelación (regla de negocio en el dominio)
    
    public (bool Allowed, string? Reason) CanCancel()
    {
        if (Status == ReservationStatus.Cancelled)
            return (false, "La reserva ya está cancelada.");
        if (Status == ReservationStatus.Completed)
            return (false, "No se puede cancelar una reserva ya completada.");

        var hoursUntilCheckIn = (CheckInDateTime - DateTime.UtcNow).TotalHours;
        if (hoursUntilCheckIn < CancellationWindow.TotalHours)
            return (false, $"Solo se puede cancelar con al menos {(int)CancellationWindow.TotalHours} horas de antelación.");

        return (true, null);
    }
}