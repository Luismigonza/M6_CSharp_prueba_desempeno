using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentaSegura.Web.Models;
using RentaSegura.Web.Services;

namespace RentaSegura.Web.Controllers;

[AllowAnonymous]
public sealed class HomeController : Controller
{
    private readonly PropertyService    _properties;
    private readonly FavoriteService    _favorites;
    private readonly ReservationService _reservations;

    public HomeController(PropertyService properties, FavoriteService favorites, ReservationService reservations)
    {
        _properties   = properties;
        _favorites    = favorites;
        _reservations = reservations;
    }

    // catálogo con filtro opcional de ciudad
    public async Task<IActionResult> Index(string? city)
    {
        var properties = await _properties.SearchAsync(city);

        var favoriteIds = new HashSet<Guid>();
        if (User.Identity?.IsAuthenticated == true)
            favoriteIds = await _favorites.GetFavoriteIdsAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        return View(new CatalogViewModel
        {
            Properties  = properties,
            FavoriteIds = favoriteIds,
            City        = city
        });
    }

    // GET /Home/Details/{id}
    public async Task<IActionResult> Details(Guid id, DateOnly? checkIn, DateOnly? checkOut, string? message)
    {
        var property = await _properties.GetAsync(id);
        if (property is null) return NotFound();

        var vm = new PropertyDetailsViewModel
        {
            Property = property, CheckIn = checkIn, CheckOut = checkOut, Message = message
        };

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            vm.IsFavorite         = (await _favorites.GetFavoriteIdsAsync(userId)).Contains(id);
            vm.UserHasApprovedKyc = await _reservations.HasApprovedKycAsync(userId);
        }

        return View(vm);
    }

    public IActionResult Error() => View();
}