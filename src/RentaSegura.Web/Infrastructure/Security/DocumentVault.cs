using System.Security.Cryptography;

namespace RentaSegura.Web.Infrastructure.Security;

public interface IDocumentVault
{
    /// <summary>Cifra y guarda el contenido. Devuelve un identificador opaco.</summary>
    Task<string> StoreAsync(byte[] content, CancellationToken cancellationToken = default);

    /// <summary>Recupera y descifra el contenido por su identificador.</summary>
    Task<byte[]> RetrieveAsync(string handle, CancellationToken cancellationToken = default);

    /// <summary>Elimina de forma segura el contenido (sobrescribe y borra).</summary>
    Task SecureDeleteAsync(string handle, CancellationToken cancellationToken = default);
}

public sealed class DocumentVaultOptions
{
    public const string SectionName = "DocumentVault";

    /// <summary>Carpeta donde se guardan los blobs cifrados.</summary>
    public string StoragePath { get; set; } = "/app/vault";

    /// <summary>Clave maestra (32 bytes en Base64 o texto de 32+ caracteres).</summary>
    public string MasterKey { get; set; } = string.Empty;
}

public sealed class AesDocumentVault : IDocumentVault
{
    private readonly DocumentVaultOptions _options;
    private readonly byte[] _key;

    public AesDocumentVault(DocumentVaultOptions options)
    {
        _options = options;
        // Derivamos 32 bytes de clave de forma estable a partir del secreto configurado.
        _key = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            string.IsNullOrWhiteSpace(options.MasterKey) ? "clave-por-defecto-cambiar" : options.MasterKey));
        Directory.CreateDirectory(_options.StoragePath);
    }

    public async Task<string> StoreAsync(byte[] content, CancellationToken cancellationToken = default)
    {
        // AES-GCM: nonce(12) + tag(16) + ciphertext. Cifrado autenticado.
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var cipher = new byte[content.Length];

        using (var aes = new AesGcm(_key, 16))
            aes.Encrypt(nonce, content, cipher, tag);

        var payload = new byte[nonce.Length + tag.Length + cipher.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipher, 0, payload, nonce.Length + tag.Length, cipher.Length);

        var handle = Guid.NewGuid().ToString("N");
        await File.WriteAllBytesAsync(Path.Combine(_options.StoragePath, handle), payload, cancellationToken);
        return handle;
    }

    public async Task<byte[]> RetrieveAsync(string handle, CancellationToken cancellationToken = default)
    {
        var payload = await File.ReadAllBytesAsync(Path.Combine(_options.StoragePath, handle), cancellationToken);

        var nonce = payload[..12];
        var tag = payload[12..28];
        var cipher = payload[28..];
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return plain;
    }

    public Task SecureDeleteAsync(string handle, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_options.StoragePath, handle);
        if (File.Exists(path))
        {
            // Sobrescribir con ruido antes de borrar (mejor esfuerzo).
            var length = new FileInfo(path).Length;
            var noise = RandomNumberGenerator.GetBytes((int)Math.Max(length, 1));
            File.WriteAllBytes(path, noise);
            File.Delete(path);
        }
        return Task.CompletedTask;
    }
}
