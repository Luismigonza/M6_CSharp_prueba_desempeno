// ============================================================================
//  Models / ViewModels.cs
// ============================================================================

using System.ComponentModel.DataAnnotations;
using RentaSegura.Web.Domain;

namespace RentaSegura.Web.Models;

public sealed class CatalogViewModel
{
    public List<Property> Properties { get; set; } = new();
    public HashSet<Guid> FavoriteIds { get; set; } = new();
    public string? City { get; set; }
}

public sealed class PropertyDetailsViewModel
{
    public Property Property { get; set; } = default!;
    public bool IsFavorite { get; set; }
    public bool UserHasApprovedKyc { get; set; }
    [DataType(DataType.Date)] public DateOnly? CheckIn { get; set; }
    [DataType(DataType.Date)] public DateOnly? CheckOut { get; set; }
    public string? Message { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, StringLength(120)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = Roles.Huesped;
    public string? ReturnUrl { get; set; }
}

public sealed class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public sealed class PropertyFormViewModel
{
    public Guid? Id { get; set; }
    [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Description { get; set; } = string.Empty;
    [Required, StringLength(100)] public string City { get; set; } = string.Empty;
    [Required, StringLength(250)] public string Address { get; set; } = string.Empty;
    [Range(1000, 100_000_000)] public decimal PricePerNight { get; set; }
    [Range(0, 50)] public int Bedrooms { get; set; }
    [Range(1, 100)] public int Capacity { get; set; }
    [Url] public string? ImageUrl { get; set; }
    public bool IsPublished { get; set; } = true;
}

public sealed class ProfileViewModel
{
    [Required, StringLength(120, MinimumLength = 2)]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;
}

public sealed class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Contraseña actual")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password), Display(Name = "Nueva contraseña")]
    public string NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword)), DataType(DataType.Password), Display(Name = "Confirmar nueva contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}