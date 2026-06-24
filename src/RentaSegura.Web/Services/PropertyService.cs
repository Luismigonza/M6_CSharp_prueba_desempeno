using Microsoft.EntityFrameworkCore;
using RentaSegura.Web.Domain;
using RentaSegura.Web.Infrastructure;

namespace RentaSegura.Web.Services;

public sealed class PropertyService
{
    private readonly AppDbContext _db;
    public PropertyService(AppDbContext db) => _db = db;

    /// <summary>Catálogo público filtrado por ciudad (opcional).</summary>
    public async Task<List<Property>> SearchAsync(string? city, CancellationToken ct = default)
    {
        var query = _db.Properties.AsNoTracking().Where(p => p.IsPublished);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(p => EF.Functions.ILike(p.City, $"%{city}%"));

        return await query.OrderBy(p => p.PricePerNight).ToListAsync(ct);
    }

    public Task<Property?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.Properties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<List<Property>> ListByOwnerAsync(string ownerId, CancellationToken ct = default) =>
        await _db.Properties.AsNoTracking()
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<Property> CreateAsync(Property property, CancellationToken ct = default)
    {
        _db.Properties.Add(property);
        await _db.SaveChangesAsync(ct);
        return property;
    }

    public async Task<bool> UpdateAsync(Property updated, string ownerId, CancellationToken ct = default)
    {
        var entity = await _db.Properties.FirstOrDefaultAsync(p => p.Id == updated.Id, ct);
        if (entity is null || entity.OwnerId != ownerId) return false;

        entity.Title        = updated.Title;
        entity.Description  = updated.Description;
        entity.City         = updated.City;
        entity.Address      = updated.Address;
        entity.PricePerNight = updated.PricePerNight;
        entity.Bedrooms     = updated.Bedrooms;
        entity.Capacity     = updated.Capacity;
        entity.ImageUrl     = updated.ImageUrl;
        entity.IsPublished  = updated.IsPublished;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}