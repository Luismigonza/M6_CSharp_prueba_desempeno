using Microsoft.EntityFrameworkCore;
using RentaSegura.Web.Domain;
using RentaSegura.Web.Infrastructure;

namespace RentaSegura.Web.Services;

public sealed record DashboardMetrics(
    decimal TotalRevenue,
    int ReservationCount,
    int BookedNights,
    double OccupancyRate,
    List<MonthlyRevenue> RevenueByMonth,
    List<PropertyPerformance> ByProperty);

public sealed record MonthlyRevenue(string Month, decimal Revenue);
public sealed record PropertyPerformance(Guid PropertyId, string Title, decimal Revenue, int Reservations, int BookedNights);

public sealed class DashboardService
{
    private readonly AppDbContext _db;
    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardMetrics> GetMetricsAsync(
        string ownerId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var properties = await _db.Properties.AsNoTracking()
            .Where(p => p.OwnerId == ownerId)
            .ToListAsync(ct);
        var propertyIds = properties.Select(p => p.Id).ToHashSet();

        // Reservas del portafolio que caen dentro del rango (no canceladas).
        var reservations = await _db.Reservations.AsNoTracking()
            .Where(r => propertyIds.Contains(r.PropertyId)
                        && r.Status != ReservationStatus.Cancelled
                        && r.CheckInDate >= from && r.CheckInDate <= to)
            .ToListAsync(ct);

        var totalRevenue = reservations.Sum(r => r.PricePaid);
        var bookedNights = reservations.Sum(r => r.Nights);

        // Ocupación = noches reservadas / (inmuebles × días del rango).
        var rangeDays = Math.Max(1, to.DayNumber - from.DayNumber + 1);
        var availableNights = properties.Count * rangeDays;
        var occupancy = availableNights == 0 ? 0 : (double)bookedNights / availableNights;

        var revenueByMonth = reservations
            .GroupBy(r => new { r.CheckInDate.Year, r.CheckInDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyRevenue($"{g.Key.Year}-{g.Key.Month:D2}", g.Sum(r => r.PricePaid)))
            .ToList();

        var byProperty = properties
            .Select(p =>
            {
                var rs = reservations.Where(r => r.PropertyId == p.Id).ToList();
                return new PropertyPerformance(p.Id, p.Title, rs.Sum(r => r.PricePaid), rs.Count, rs.Sum(r => r.Nights));
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return new DashboardMetrics(totalRevenue, reservations.Count, bookedNights, occupancy, revenueByMonth, byProperty);
    }
}
