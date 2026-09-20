using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace IgnisEducationSuite.ServerServices.Security;

public sealed class EncryptionService : IEncryptionService
{
    private const int KeySizeBytes = 32; // AES-256
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly EncryptionConfiguration _configuration;

    public EncryptionService(
        IOptions<EncryptionConfiguration> configuration)
    {
        _configuration = configuration.Value;

        ValidateConfiguration();
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            throw new ArgumentException(
                "The value to encrypt cannot be empty.",
                nameof(plainText));
        }

        var keyVersion = _configuration.CurrentKeyVersion;

        var key = GetKey(keyVersion);

        var nonce = RandomNumberGenerator.GetBytes(
            NonceSizeBytes);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        var cipherBytes = new byte[plainBytes.Length];

        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(
            key,
            TagSizeBytes);

        aes.Encrypt(
            nonce,
            plainBytes,
            cipherBytes,
            tag);

        /*
         * Format:
         *
         * version.nonce.tag.ciphertext
         *
         * Everything except the version is Base64 encoded.
         */

        return string.Join(
            ".",
            keyVersion,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(cipherBytes));
    }

    public string Decrypt(
        string encryptedText,
        string keyVersion)
    {
        if (string.IsNullOrWhiteSpace(encryptedText))
        {
            throw new ArgumentException(
                "The encrypted value cannot be empty.",
                nameof(encryptedText));
        }

        if (string.IsNullOrWhiteSpace(keyVersion))
        {
            throw new ArgumentException(
                "The encryption key version cannot be empty.",
                nameof(keyVersion));
        }

        var parts = encryptedText.Split('.');

        if (parts.Length != 4)
        {
            throw new CryptographicException(
                "The encrypted value has an invalid format.");
        }

        var storedKeyVersion = parts[0];

        if (!string.Equals(
                storedKeyVersion,
                keyVersion,
                StringComparison.Ordinal))
        {
            throw new CryptographicException(
                "The encrypted value was created using a different encryption key version.");
        }

        var key = GetKey(keyVersion);

        byte[] nonce;
        byte[] tag;
        byte[] cipherBytes;

        try
        {
            nonce = Convert.FromBase64String(parts[1]);
            tag = Convert.FromBase64String(parts[2]);
            cipherBytes = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException(
                "The encrypted value contains invalid Base64 data.",
                ex);
        }

        if (nonce.Length != NonceSizeBytes)
        {
            throw new CryptographicException(
                "The encrypted value contains an invalid nonce.");
        }

        if (tag.Length != TagSizeBytes)
        {
            throw new CryptographicException(
                "The encrypted value contains an invalid authentication tag.");
        }

        var plainBytes = new byte[cipherBytes.Length];

        try
        {
            using var aes = new AesGcm(
                key,
                TagSizeBytes);

            aes.Decrypt(
                nonce,
                cipherBytes,
                tag,
                plainBytes);
        }
        catch (CryptographicException)
        {
            throw new CryptographicException(
                "The encrypted value could not be authenticated or decrypted.");
        }

        return Encoding.UTF8.GetString(plainBytes);
    }

    private byte[] GetKey(string keyVersion)
    {
        if (!_configuration.Keys.TryGetValue(
                keyVersion,
                out var base64Key))
        {
            throw new InvalidOperationException(
                $"Encryption key version '{keyVersion}' is not configured.");
        }

        byte[] key;

        try
        {
            key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"Encryption key '{keyVersion}' is not valid Base64.",
                ex);
        }

        if (key.Length != KeySizeBytes)
        {
            throw new InvalidOperationException(
                $"Encryption key '{keyVersion}' must be exactly {KeySizeBytes} bytes.");
        }

        return key;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(
                _configuration.CurrentKeyVersion))
        {
            throw new InvalidOperationException(
                "Encryption:CurrentKeyVersion is not configured.");
        }

        if (_configuration.Keys.Count == 0)
        {
            throw new InvalidOperationException(
                "No encryption keys have been configured.");
        }

        _ = GetKey(
            _configuration.CurrentKeyVersion);
    }
}