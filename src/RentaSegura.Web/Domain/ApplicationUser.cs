using Microsoft.AspNetCore.Identity;

namespace RentaSegura.Web.Domain;

public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}

public static class Roles
{
    public const string Huesped = nameof(Huesped);   
    public const string Anfitrion = nameof(Anfitrion); 

    public static readonly string[] All = { Huesped, Anfitrion };
}
