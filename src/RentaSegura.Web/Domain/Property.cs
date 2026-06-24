namespace RentaSegura.Web.Domain;

public sealed class Property : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string City { get; set; } = default!;
    public string Address { get; set; } = default!;
    public decimal PricePerNight { get; set; }
    public int Bedrooms { get; set; }
    public int Capacity { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsPublished { get; set; } = true;
    
    // Auditoría
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string?  LastModifiedBy { get; set; }

    public List<Reservation> Reservations { get; set; } = new();
}

