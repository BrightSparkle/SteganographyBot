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
    
    public static byte[] ConvertBinaryStringToByteArray( string binary)
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
    
    
    public static string BinaryToReadableText( string binaryText)
    {
        //the final text that decode
        string result = "";
        //the number can divide 8 bits in long string
        int index = 0;
        //the array that holds the characters bits
        string[] bits = new string[binaryText.Length / 8];

        //lets first divide the characters 
        for (int i = 0; i < bits.Length; i++)
        {
            bits[i] = binaryText.Substring(index, 8);
            index += 8;
        }

        foreach (string item in bits)
        {
            //numbers must convert differently
            //00001010 its value of Next line
            if (item.Substring(0, 4) == "0000" && !item.Equals("00001010"))
            {
                result += Convert.ToInt32(item, 2).ToString();
            }
            else
            {
                var data = ConvertBinaryStringToByteArray(item);
                result += Encoding.ASCII.GetString(data);
            }
        }
        return result;
    }
    
    
    public static string ConvertStringToBinary(this string text)
    {
        //store results of binary 
        string binaryresult = "";
        foreach (char item in text)
        {
            //Checkif the input is number
            if (item.ToString().Any(char.IsDigit))
            {
                //convert int to binary(we need to know if the input is number ot not cause the first 4 bits of first digits are 0000)
                binaryresult += string.Format("{0:00000000}", int.Parse(Convert.ToString(int.Parse(item.ToString()), 2)));
            }
            else
            {
                //if input is text
                binaryresult += ToBinary(  (byte)item  );
            }
        }
        //fully converted
        return binaryresult;
    }
    
}