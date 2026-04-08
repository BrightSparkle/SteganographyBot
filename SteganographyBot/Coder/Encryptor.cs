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
        var bmp = new Bitmap(image);

        byte[] aesKey = CryptoHelper.GenerateAesKey();
        byte[] aesIv = CryptoHelper.GenerateAesIv();

        byte[] plainMessage = Encoding.UTF8.GetBytes(message);
        byte[] encryptedMessage = CryptoHelper.AesEncrypt(plainMessage, aesKey, aesIv);

        byte[] encryptedAesKey = CryptoHelper.RsaEncrypt(aesKey, publicKeyXml);

        byte[] keyLen = BitConverter.GetBytes(encryptedAesKey.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(keyLen);
        byte[] fullPackage = keyLen.Concat(encryptedAesKey).Concat(aesIv).Concat(encryptedMessage).ToArray();

        SteganographyHelper.EmbedBytes(bmp, fullPackage);

        var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}