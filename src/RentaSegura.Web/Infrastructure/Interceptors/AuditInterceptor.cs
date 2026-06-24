using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RentaSegura.Web.Domain;
using System.Security.Claims;

namespace RentaSegura.Web.Infrastructure.Interceptors;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContext;

    public AuditInterceptor(IHttpContextAccessor httpContext)
        => _httpContext = httpContext;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var userId = _httpContext.HttpContext?.User
            .FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        var now = DateTime.UtcNow;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc   = now;
                    entry.Entity.UpdatedAtUtc   = now;
                    entry.Entity.LastModifiedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc   = now;
                    entry.Entity.LastModifiedBy = userId;
                    entry.Property(nameof(IAuditable.CreatedAtUtc)).IsModified = false;
                    break;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}