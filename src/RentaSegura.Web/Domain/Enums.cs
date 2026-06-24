namespace RentaSegura.Web.Domain;

/// <summary>Estado de una reserva.</summary>
public enum ReservationStatus
{
    Confirmed = 0,
    Cancelled = 1,
    Completed = 2
}

/// <summary>Veredicto de la validación de identidad (KYC).</summary>
public enum KycStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
