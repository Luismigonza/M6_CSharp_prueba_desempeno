using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentaSegura.Web.Services;

namespace RentaSegura.Web.Controllers;

[Authorize]
public sealed class FavoritesController : Controller
{
    private readonly FavoriteService _favorites;
    public FavoritesController(FavoriteService favorites) => _favorites = favorites;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // lista de favoritos.
    public async Task<IActionResult> Index()
        => View(await _favorites.ListAsync(UserId));

    // Marcar y Desmarcar favoritos.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid propertyId)
    {
        var isFavorite = await _favorites.ToggleAsync(UserId, propertyId);
        return Json(new { isFavorite });
    }
}
