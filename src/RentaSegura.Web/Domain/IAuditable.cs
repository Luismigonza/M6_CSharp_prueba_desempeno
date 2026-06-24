namespace RentaSegura.Web.Domain;

public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    DateTime UpdatedAtUtc { get; set; }
    string? LastModifiedBy { get; set; }
}