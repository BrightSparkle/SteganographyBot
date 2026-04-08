using System.Security.Cryptography;
using System.Text;

namespace SteganographyBot;

public static class CryptoHelper
{
    // Генерация случайного AES-ключа (256 бит) и IV (128 бит)
    public static byte[] GenerateAesKey() => RandomNumberGenerator.GetBytes(32);
    public static byte[] GenerateAesIv() => RandomNumberGenerator.GetBytes(16);

    // AES шифрование (CBC, PKCS7)
    public static byte[] AesEncrypt(byte[] plaintext, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
    }

    // AES расшифрование
    public static byte[] AesDecrypt(byte[] ciphertext, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    // RSA шифрование (OAEP-SHA256)
    public static byte[] RsaEncrypt(byte[] data, string publicKeyXml)
    {
        using var rsa = RSA.Create();
        rsa.FromXmlString(publicKeyXml);
        return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
    }

    // RSA расшифрование
    public static byte[] RsaDecrypt(byte[] encryptedData, string privateKeyXml)
    {
        using var rsa = RSA.Create();
        rsa.FromXmlString(privateKeyXml);
        return rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
    }

    // Генерация пары ключей (для удобства пользователя)
    public static (string publicKey, string privateKey) GenerateRsaKeyPair()
    {
        using var rsa = RSA.Create(2048);
        string publicKey = rsa.ToXmlString(false);
        string privateKey = rsa.ToXmlString(true);
        return (publicKey, privateKey);
    }
}