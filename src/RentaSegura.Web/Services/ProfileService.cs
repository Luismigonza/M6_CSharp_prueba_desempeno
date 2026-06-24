using Microsoft.AspNetCore.Identity;
using RentaSegura.Web.Domain;

namespace RentaSegura.Web.Services;

public sealed class ProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileService(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    public async Task<(bool Ok, string? Error)> UpdateNameAsync(string userId, string newName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return (false, "Usuario no encontrado.");

        user.FullName = newName;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? (true, null)
            : (false, string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    public async Task<(bool Ok, string? Error)> ChangePasswordAsync(
        string userId, string current, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return (false, "Usuario no encontrado.");

        var result = await _userManager.ChangePasswordAsync(user, current, newPassword);
        return result.Succeeded
            ? (true, null)
            : (false, string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}