using System.Security.Cryptography;
using System.Text;

namespace Bookstore3.WPF.Utils;

/// <summary>
/// AES-256-CBC string encryption with a random IV prepended to the ciphertext (Base64).
/// </summary>
internal sealed class AesStringCipher : IStringCipher
{
    private static readonly byte[] Key = SHA256.HashData(Encoding.UTF8.GetBytes("Bookstore3.StringCipher.v1"));

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        using var aes = Aes.Create();
        aes.Key = Key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var payload = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, payload, aes.IV.Length, cipherBytes.Length);
        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        var payload = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var ivLength = aes.BlockSize / 8;
        if (payload.Length <= ivLength)
            throw new CryptographicException("Invalid ciphertext.");

        var iv = new byte[ivLength];
        Buffer.BlockCopy(payload, 0, iv, 0, ivLength);
        aes.IV = iv;

        var cipherBytes = new byte[payload.Length - ivLength];
        Buffer.BlockCopy(payload, ivLength, cipherBytes, 0, cipherBytes.Length);

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
