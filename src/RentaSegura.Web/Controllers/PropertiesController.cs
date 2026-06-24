using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentaSegura.Web.Domain;
using RentaSegura.Web.Models;
using RentaSegura.Web.Services;

namespace RentaSegura.Web.Controllers;

[Authorize(Roles = Roles.Anfitrion)]
[Route("Owner/Properties")]
public sealed class PropertiesController : Controller
{
    private readonly PropertyService _properties;
    public PropertiesController(PropertyService properties) => _properties = properties;

    private string OwnerId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /Owner/Properties
    [HttpGet("")]
    public async Task<IActionResult> Index()
        => View(await _properties.ListByOwnerAsync(OwnerId));

    // GET /Owner/Properties/Create
    [HttpGet("Create")]
    public IActionResult Create() => View("Form", new PropertyFormViewModel());

    // POST /Owner/Properties/Create
    [HttpPost("Create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        await _properties.CreateAsync(new Property
        {
            OwnerId = OwnerId,
            Title = model.Title, Description = model.Description, City = model.City, Address = model.Address,
            PricePerNight = model.PricePerNight, Bedrooms = model.Bedrooms, Capacity = model.Capacity,
            ImageUrl = model.ImageUrl, IsPublished = model.IsPublished
        });

        TempData["Success"] = "Inmueble publicado.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Owner/Properties/Edit/{id}
    [HttpGet("Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var p = await _properties.GetAsync(id);
        if (p is null || p.OwnerId != OwnerId) return NotFound();

        return View("Form", new PropertyFormViewModel
        {
            Id = p.Id, Title = p.Title, Description = p.Description, City = p.City, Address = p.Address,
            PricePerNight = p.PricePerNight, Bedrooms = p.Bedrooms, Capacity = p.Capacity,
            ImageUrl = p.ImageUrl, IsPublished = p.IsPublished
        });
    }

    // POST /Owner/Properties/Edit/{id}
    [HttpPost("Edit/{id:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PropertyFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var ok = await _properties.UpdateAsync(new Property
        {
            Id = id,
            Title = model.Title, Description = model.Description, City = model.City, Address = model.Address,
            PricePerNight = model.PricePerNight, Bedrooms = model.Bedrooms, Capacity = model.Capacity,
            ImageUrl = model.ImageUrl, IsPublished = model.IsPublished
        }, OwnerId);

        if (!ok) return NotFound();

        TempData["Success"] = "Inmueble actualizado.";
        return RedirectToAction(nameof(Index));
    }
}
