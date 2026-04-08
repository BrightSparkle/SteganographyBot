using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace StegBot.Bot;

public static class SteganographyHelper
{
    // ========== Вспомогательные методы ==========
    private static string ToBinary(byte b) => Convert.ToString(b, 2).PadLeft(8, '0');

    public static (string red, string green, string blue) ToBinary(Color color)
    {
        return (ToBinary(color.R), ToBinary(color.G), ToBinary(color.B));
    }

    public static string ReplaceFirstBits(string bits, string bitForChange)
    {
        if (bitForChange.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(bitForChange), "Maximum number of bits is 8");
        return bits.Substring(0, bits.Length - bitForChange.Length) + bitForChange;
    }

    public static string ReadFirstBits(string bit, byte numberOfBits)
    {
        if (numberOfBits > 8)
            throw new ArgumentOutOfRangeException(nameof(numberOfBits), "Maximum number of bits is 8");
        return bit.Substring(8 - numberOfBits, numberOfBits);
    }

    public static byte[] ConvertBinaryStringToByteArray(string binary)
    {
        var list = new List<byte>();
        for (int i = 0; i < binary.Length; i += 8)
            list.Add(Convert.ToByte(binary.Substring(i, 8), 2));
        return list.ToArray();
    }

    // ========== Новые методы для работы с байтами ==========
    public static void EmbedBytes(Bitmap image, byte[] data)
    {
        // Формируем полные данные: длина (4 байта, big-endian) + сами данные
        byte[] lengthBytes = BitConverter.GetBytes(data.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBytes); // приводим к big-endian

        byte[] fullData = lengthBytes.Concat(data).ToArray();
        string bits = string.Join("", fullData.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
        EmbedBits(image, bits);
    }

    public static byte[] ExtractBytes(Bitmap image)
    {
        string bits = ExtractAllBits(image);
        if (bits.Length < 32) return Array.Empty<byte>();

        // Первые 32 бита — длина данных (big-endian)
        byte[] lengthBytes = new byte[4];
        for (int i = 0; i < 4; i++)
            lengthBytes[i] = Convert.ToByte(bits.Substring(i * 8, 8), 2);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBytes);
        int dataLength = BitConverter.ToInt32(lengthBytes, 0);

        if (bits.Length < 32 + dataLength * 8)
            return Array.Empty<byte>();

        string dataBits = bits.Substring(32, dataLength * 8);
        byte[] result = new byte[dataLength];
        for (int i = 0; i < dataLength; i++)
            result[i] = Convert.ToByte(dataBits.Substring(i * 8, 8), 2);
        return result;
    }

    private static void EmbedBits(Bitmap image, string bits)
    {
        int bitIndex = 0;
        for (int x = 0; x < image.Width && bitIndex < bits.Length; x++)
        {
            for (int y = 0; y < image.Height && bitIndex < bits.Length; y++)
            {
                Color pixel = image.GetPixel(x, y);
                var (rBits, gBits, bBits) = ToBinary(pixel);

                // 2 бита в красный
                if (bitIndex + 2 <= bits.Length)
                {
                    rBits = ReplaceFirstBits(rBits, bits.Substring(bitIndex, 2));
                    bitIndex += 2;
                }
                // 2 бита в зелёный
                if (bitIndex + 2 <= bits.Length)
                {
                    gBits = ReplaceFirstBits(gBits, bits.Substring(bitIndex, 2));
                    bitIndex += 2;
                }
                // 2 бита в синий
                if (bitIndex + 2 <= bits.Length)
                {
                    bBits = ReplaceFirstBits(bBits, bits.Substring(bitIndex, 2));
                    bitIndex += 2;
                }

                byte[] newRgb = ConvertBinaryStringToByteArray(rBits + gBits + bBits);
                Color newColor = Color.FromArgb(newRgb[0], newRgb[1], newRgb[2]);
                image.SetPixel(x, y, newColor);
            }
        }
        if (bitIndex < bits.Length)
            throw new Exception("Image is too small to embed the data.");
    }

    private static string ExtractAllBits(Bitmap image)
    {
        var sb = new StringBuilder();
        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                Color pixel = image.GetPixel(x, y);
                var (rBits, gBits, bBits) = ToBinary(pixel);
                sb.Append(ReadFirstBits(rBits, 2));
                sb.Append(ReadFirstBits(gBits, 2));
                sb.Append(ReadFirstBits(bBits, 2));
            }
        }
        return sb.ToString();
    }
}