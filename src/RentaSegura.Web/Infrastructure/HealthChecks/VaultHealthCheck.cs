using Microsoft.Extensions.Diagnostics.HealthChecks;
using RentaSegura.Web.Infrastructure.Security;

namespace RentaSegura.Web.Infrastructure.HealthChecks;

public sealed class VaultHealthCheck : IHealthCheck
{
    private readonly DocumentVaultOptions _options;

    public VaultHealthCheck(DocumentVaultOptions options) => _options = options;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(_options.StoragePath);
            var probe = Path.Combine(_options.StoragePath, ".healthcheck");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return Task.FromResult(HealthCheckResult.Healthy("Bóveda accesible."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Bóveda no accesible.", ex));
        }
    }
}