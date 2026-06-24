using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentaSegura.Web.Models;
using RentaSegura.Web.Services;

namespace RentaSegura.Web.Controllers;

[Authorize]
public sealed class ProfileController : Controller
{
    private readonly ProfileService _profile;
    public ProfileController(ProfileService profile) => _profile = profile;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public IActionResult Index() => View(new ProfileViewModel
    {
        FullName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty
    });

    [HttpPost("Profile/UpdateName"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateName(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View("Index", model);

        var (ok, error) = await _profile.UpdateNameAsync(UserId, model.FullName);
        TempData[ok ? "Success" : "Error"] = ok ? "Nombre actualizado." : error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Profile/ChangePassword"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revisa los campos de contraseña.";
            return RedirectToAction(nameof(Index));
        }

        var (ok, error) = await _profile.ChangePasswordAsync(UserId, model.CurrentPassword, model.NewPassword);
        TempData[ok ? "Success" : "Error"] = ok ? "Contraseña actualizada." : error;
        return RedirectToAction(nameof(Index));
    }
}