using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace StegBot.Bot;

public static class SteganographyHelper
{
    
    private static string ToBinary( byte b)
    {
        return Convert.ToString(b, 2).PadLeft(8, '0');
    }
    
    public static (string red, string green, string blue) ToBinary( Color color)
    {
        string red = ToBinary(color.R);
        string green = ToBinary(color.G);
        string blue = ToBinary(color.B);
        return (red, green, blue);
    }
    
    public static string UnicodeStringToBinary( string text)
    {
        //convert text to byte
        string binarytext =
            string.Join(
                string.Empty,
                Encoding.UTF32
                    .GetBytes(text)
                    .Select(byt => Convert.ToString(byt, 2).PadLeft(8, '0')));
        //now we have binary text
        return binarytext;
    }
    
    public static byte[] ConvertBinaryStringToByteArray(this string binary)
    {
        var list = new List<byte>();

        for (int i = 0; i < binary.Length; i += 8)
        {
            string t = binary.Substring(i, 8);

            list.Add(Convert.ToByte(t, 2));
        }

        return list.ToArray();
    }
    
    
    public static string ReplaceFirstBits(string bits, string bitforchange)
    {
        if (bitforchange.Length > 8)
        {
            throw new ArgumentOutOfRangeException("Maximum number of bits is 8");
        }

        int numberOfBits = bitforchange.Length;
        bits = bits.Substring(0, bits.Length - numberOfBits);
        bits += bitforchange;
        return bits;
    }
    
    public static string ReadFirstBits( string bit, byte numberOfBits)
    {
        if (numberOfBits > 8)
        {
            throw new ArgumentOutOfRangeException("Maximum number of bits is 8");
        }

        return bit.Substring(8 - numberOfBits, numberOfBits);
    }
    
    public static string GetUnicodeTextFromBinary( string binarytext)
    {
        //convert binary to asci bytes
        byte[] asciitxt = ConvertBinaryStringToByteArray(binarytext);
        return Encoding.UTF32.GetString(asciitxt);
    }
    
}