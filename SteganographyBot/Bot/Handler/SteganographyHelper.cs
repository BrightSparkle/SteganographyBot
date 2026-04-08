using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace StegBot.Bot;

public static class SteganographyHelper
{
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

    public static void EmbedBytes(Bitmap image, byte[] data)
    {
        byte[] lengthBytes = BitConverter.GetBytes(data.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBytes);

        byte[] fullData = lengthBytes.Concat(data).ToArray();
        string bits = string.Join("", fullData.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
        EmbedBits(image, bits);
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
                if (bitIndex + 2 <= bits.Length)
                {
                    rBits = ReplaceFirstBits(rBits, bits.Substring(bitIndex, 2));
                    bitIndex += 2;
                }
                if (bitIndex + 2 <= bits.Length)
                {
                    gBits = ReplaceFirstBits(gBits, bits.Substring(bitIndex, 2));
                    bitIndex += 2;
                }
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

    public static byte[] ExtractBytes(Bitmap image)
    {
        string first36Bits = ExtractBitsFromPixels(image, 0, 6);
        if (first36Bits.Length < 32)
            return Array.Empty<byte>();

        byte[] lengthBytes = new byte[4];
        for (int i = 0; i < 4; i++)
            lengthBytes[i] = Convert.ToByte(first36Bits.Substring(i * 8, 8), 2);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBytes);

        int dataLength = BitConverter.ToInt32(lengthBytes, 0);

        if (dataLength <= 0 || dataLength > 100 * 1024 * 1024)
            return Array.Empty<byte>();

        int totalBitsNeeded = 32 + dataLength * 8;
        int pixelsNeeded = (int)Math.Ceiling(totalBitsNeeded / 6.0);

        string allBits = ExtractBitsFromPixels(image, 0, pixelsNeeded);
        if (allBits.Length < totalBitsNeeded)
            return Array.Empty<byte>();

        byte[] result = new byte[dataLength];
        for (int i = 0; i < dataLength; i++)
        {
            result[i] = Convert.ToByte(allBits.Substring(32 + i * 8, 8), 2);
        }

        return result;
    }

    private static string ExtractBitsFromPixels(Bitmap image, int startPixel, int pixelsCount)
    {
        if (pixelsCount <= 0)
            return string.Empty;

        int startX = startPixel / image.Height;
        int startY = startPixel % image.Height;

        var sb = new StringBuilder(pixelsCount * 6);
        int pixelsRead = 0;

        for (int x = startX; x < image.Width && pixelsRead < pixelsCount; x++)
        {
            int yStart = (x == startX) ? startY : 0;
            for (int y = yStart; y < image.Height && pixelsRead < pixelsCount; y++)
            {
                Color pixel = image.GetPixel(x, y);

                int rBits = pixel.R & 0x03;
                int gBits = pixel.G & 0x03;
                int bBits = pixel.B & 0x03;

                sb.Append(Convert.ToString(rBits, 2).PadLeft(2, '0'));
                sb.Append(Convert.ToString(gBits, 2).PadLeft(2, '0'));
                sb.Append(Convert.ToString(bBits, 2).PadLeft(2, '0'));

                pixelsRead++;
            }
        }

        return sb.ToString();
    }
}