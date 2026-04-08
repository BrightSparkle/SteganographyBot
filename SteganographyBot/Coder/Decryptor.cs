using System.Drawing;
using System.Text;
using StegBot.Bot;

namespace SteganographyBot;

public class Decryptor
{
    public async Task<string> DecodeImage(Stream image, string privateKeyXml)
    {
        using var img = new Bitmap(image);

        // 1. Извлекаем байты из LSB
        byte[] extracted = SteganographyHelper.ExtractBytes(img);
        if (extracted.Length == 0)
            return "No hidden data found or the image is corrupted.";

        // 2. Разбираем пакет
        int offset = 0;
        if (extracted.Length < 4) return "Invalid package: missing length.";
        // длина зашифрованного ключа (4 байта, big-endian)
        byte[] lenBytes = extracted.Take(4).ToArray();
        if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
        int encryptedKeyLen = BitConverter.ToInt32(lenBytes, 0);
        offset += 4;

        if (extracted.Length < offset + encryptedKeyLen)
            return "Invalid package: encrypted key truncated.";
        byte[] encryptedAesKey = extracted.Skip(offset).Take(encryptedKeyLen).ToArray();
        offset += encryptedKeyLen;

        if (extracted.Length < offset + 16)
            return "Invalid package: IV missing.";
        byte[] aesIv = extracted.Skip(offset).Take(16).ToArray();
        offset += 16;

        byte[] encryptedMessage = extracted.Skip(offset).ToArray();

        // 3. Расшифровываем AES-ключ через RSA
        byte[] aesKey;
        try
        {
            aesKey = CryptoHelper.RsaDecrypt(encryptedAesKey, privateKeyXml);
        }
        catch (Exception ex)
        {
            return $"RSA decryption failed: {ex.Message}";
        }

        // 4. Расшифровываем сообщение
        byte[] plainMessage;
        try
        {
            plainMessage = CryptoHelper.AesDecrypt(encryptedMessage, aesKey, aesIv);
        }
        catch (Exception ex)
        {
            return $"AES decryption failed: {ex.Message}";
        }

        return Encoding.UTF8.GetString(plainMessage);
    }
}