using System.Security.Cryptography;
using System.Text;

namespace STS.WALAA.Proxy.Security;

public class AES256(string key, string iv)
{
    private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();

    public string Encrypt(string plainText) => PerformEncryption(plainText, key, iv);

    public string Decrypt(string cipherText) => PerformDecryption(cipherText, key, iv);

    public static string Encrypt(string plainText, string key, string iv) => PerformEncryption(plainText, key, iv);

    public static string Decrypt(string cipherText, string key, string iv) => PerformDecryption(cipherText, key, iv);

    public static string GenerateKey() => GenerateRandomKey(256);

    public static string GenerateIV() => GenerateRandomKey(128);

    private static Aes CreateAes(string key, string iv)
    {
        var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);
        return aes;
    }

    private static string PerformEncryption(string plainText, string key, string iv)
    {
        using var aes = CreateAes(key, iv);
        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }
        var encryptedBytes = msEncrypt.ToArray();
        return Convert.ToBase64String(encryptedBytes);
    }

    private static string PerformDecryption(string cipherText, string key, string iv)
    {
        using var aes = CreateAes(key, iv);
        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        return srDecrypt.ReadToEnd();
    }

    private static string GenerateRandomKey(int length)
    {
        var keyLength = length / 8;
        var bytes = new byte[keyLength];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)[..keyLength];
    }
}