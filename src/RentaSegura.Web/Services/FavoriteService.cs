using Microsoft.EntityFrameworkCore;
using RentaSegura.Web.Domain;
using RentaSegura.Web.Infrastructure;

namespace RentaSegura.Web.Services;

public sealed class FavoriteService
{
    private readonly AppDbContext _db;
    public FavoriteService(AppDbContext db) => _db = db;

    /// <summary>Marca/desmarca un inmueble. Devuelve true si quedó como favorito.</summary>
    public async Task<bool> ToggleAsync(string userId, Guid propertyId, CancellationToken ct = default)
    {
        var existing = await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.PropertyId == propertyId, ct);

        if (existing is not null)
        {
            _db.Favorites.Remove(existing);
            await _db.SaveChangesAsync(ct);
            return false;
        }

        _db.Favorites.Add(new Favorite { UserId = userId, PropertyId = propertyId });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<Property>> ListAsync(string userId, CancellationToken ct = default) =>
        await _db.Favorites.AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .Select(f => f.Property!)
            .ToListAsync(ct);

    public async Task<HashSet<Guid>> GetFavoriteIdsAsync(string userId, CancellationToken ct = default) =>
        (await _db.Favorites.AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => f.PropertyId)
            .ToListAsync(ct)).ToHashSet();
}
