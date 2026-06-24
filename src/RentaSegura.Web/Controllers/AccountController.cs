using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentaSegura.Web.Domain;
using RentaSegura.Web.Models;

namespace RentaSegura.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly UserManager<ApplicationUser>  _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // -- Registro 
    [HttpGet]
    public IActionResult Register(string? returnUrl = null) =>
        View(new RegisterViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var role = model.Role == Roles.Anfitrion ? Roles.Anfitrion : Roles.Huesped;
        var user = new ApplicationUser
        {
            UserName = model.Email, Email = model.Email,
            FullName = model.FullName, EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            var mensajes = result.Errors
                .Select(e => TraducirError(e.Code))
                .Distinct()
                .ToList();

            foreach (var msg in mensajes)
                ModelState.AddModelError(string.Empty, msg);

            return View(model);
        }

        await _userManager.AddToRoleAsync(user, role);
        await _signInManager.SignInAsync(user, isPersistent: true);
        return LocalRedirect(model.ReturnUrl ?? "/");
    }

    // -- Login 
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, isPersistent: true, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty,
                "Cuenta bloqueada temporalmente por demasiados intentos fallidos. Intenta en unos minutos.");
            return View(model);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
            return View(model);
        }

        return LocalRedirect(model.ReturnUrl ?? "/");
    }

    // -- Logout 
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // -- Traductor de errores de Identity a español 
    private static string TraducirError(string code) => code switch
    {
        "DuplicateUserName" or
        "DuplicateEmail"        => "Ya existe una cuenta con ese correo electrónico.",
        "PasswordTooShort"      => "La contraseña debe tener al menos 8 caracteres.",
        "PasswordRequiresDigit" => "La contraseña debe incluir al menos un número.",
        "PasswordRequiresUpper" => "La contraseña debe incluir al menos una letra mayúscula.",
        "PasswordRequiresLower" => "La contraseña debe incluir al menos una letra minúscula.",
        "PasswordRequiresNonAlphanumeric" => "La contraseña debe incluir al menos un símbolo (!, @, #…).",
        "InvalidEmail"          => "El correo electrónico no es válido.",
        _ => "Ocurrió un error al crear la cuenta. Intenta de nuevo."
    };
}