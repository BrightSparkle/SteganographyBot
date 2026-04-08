using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using StegBot.Bot;

namespace SteganographyBot;

public class Encryptor
{
    public async Task<MemoryStream> EncryptStringMessageIntoImage(Stream photo, string message, string publicKeyXml)
    {
        using var image = new Bitmap(photo);
        var bmp = new Bitmap(image); // рабочая копия

        // 1. Сеансовый ключ AES и IV
        byte[] aesKey = CryptoHelper.GenerateAesKey();
        byte[] aesIv = CryptoHelper.GenerateAesIv();

        // 2. Шифруем сообщение AES
        byte[] plainMessage = Encoding.UTF8.GetBytes(message);
        byte[] encryptedMessage = CryptoHelper.AesEncrypt(plainMessage, aesKey, aesIv);

        // 3. Шифруем AES-ключ RSA
        byte[] encryptedAesKey = CryptoHelper.RsaEncrypt(aesKey, publicKeyXml);

        // 4. Формируем пакет: [длина зашифрованного ключа (4 байта big-endian)] + [зашифрованный ключ] + [IV] + [зашифрованное сообщение]
        byte[] keyLen = BitConverter.GetBytes(encryptedAesKey.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(keyLen);
        byte[] fullPackage = keyLen.Concat(encryptedAesKey).Concat(aesIv).Concat(encryptedMessage).ToArray();

        // 5. Встраиваем в изображение
        SteganographyHelper.EmbedBytes(bmp, fullPackage);

        var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}