using System.Security.Cryptography;

namespace RentaSegura.Web.Services;

/// <summary>Datos extraídos del documento + veredicto de la validación.</summary>
public sealed record KycResult(
    bool Approved,
    string FirstName,
    string LastName,
    string DocumentNumber,
    DateOnly? BirthDate,
    string? RejectionReason);

public interface IIdentityVerificationService
{
    Task<KycResult> VerifyAsync(byte[] imageBytes, string contentType, CancellationToken cancellationToken = default);
}

public sealed class StubIdentityVerificationService : IIdentityVerificationService
{
    public Task<KycResult> VerifyAsync(byte[] imageBytes, string contentType, CancellationToken cancellationToken = default)
    {
        // Validación básica de "calidad" de la imagen.
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || imageBytes.Length < 2048)
        {
            return Task.FromResult(new KycResult(
                Approved: false, FirstName: "", LastName: "", DocumentNumber: "",
                BirthDate: null, RejectionReason: "La imagen no es válida o es de baja calidad. Suba una foto clara de su documento."));
        }

        // "Extracción" simulada pero determinista (depende del contenido de la imagen).
        var hash = Convert.ToHexString(SHA256.HashData(imageBytes));
        var docNumber = "10" + (Math.Abs(BitConverter.ToInt32(SHA256.HashData(imageBytes), 0)) % 100000000).ToString("D8");

        return Task.FromResult(new KycResult(
            Approved: true,
            FirstName: "Titular",
            LastName: "Verificado",
            DocumentNumber: docNumber,
            BirthDate: new DateOnly(1990, 1, 1),
            RejectionReason: null));
    }
}
