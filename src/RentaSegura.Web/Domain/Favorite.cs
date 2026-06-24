namespace RentaSegura.Web.Domain;

public sealed class Favorite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}