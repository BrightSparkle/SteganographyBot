using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Mime;
using SteganographyBot.Bot;
using StegBot.Bot;

namespace SteganographyBot;

public class Encryptor
{
    private readonly string _startPoint = "@startpoint";
    private readonly string _endPoint = "@endpoint";

    public Encryptor()
    {
       
    }
    
    public async Task<MemoryStream> EncryptStringMessageIntoImage(Stream photo, string message)
    { 
        string content = _startPoint + message + _endPoint;
       var image = new Bitmap(photo);
       var bmp = (Bitmap)image.Clone();

       string binaryContent = SteganographyHelper.UnicodeStringToBinary(content);
       
       Console.WriteLine(content);
       Console.WriteLine(binaryContent);
       
       byte[] newnumber = new byte[3];

       //proccess bits in image
       for (int x = 0; x < image.Width; x++)
       {
           for (int y = 0; y < image.Height; y++)
           {
               if (binaryContent.Length.Equals(0))
               {
                   break;
               }

               Color pixel = image.GetPixel(x, y);
               var pixelColors = SteganographyHelper.ToBinary(pixel);

               //note:some case 2 or 4 bits left we need to check if its lastone or not
               pixelColors.red = SteganographyHelper.ReplaceFirstBits(pixelColors.red,!binaryContent.Length.Equals(0) ?
                   binaryContent.Substring(0, 2) : pixelColors.red.Substring(6, 2));
               binaryContent = binaryContent.Length.Equals(0) ? string.Empty : binaryContent.Remove(0, 2);

               pixelColors.green = SteganographyHelper.ReplaceFirstBits(pixelColors.green,!binaryContent.Length.Equals(0) ?
                   binaryContent.Substring(0, 2) : pixelColors.green.Substring(6, 2));
               binaryContent = binaryContent.Length.Equals(0) ? string.Empty : binaryContent.Remove(0, 2);

               pixelColors.blue = SteganographyHelper.ReplaceFirstBits(pixelColors.blue,!binaryContent.Length.Equals(0) ?
                   binaryContent.Substring(0, 2) : pixelColors.blue.Substring(6, 2));
               binaryContent = binaryContent.Length.Equals(0) ? string.Empty : binaryContent.Remove(0, 2);

               //Convert binary to number
               newnumber = SteganographyHelper.ConvertBinaryStringToByteArray(pixelColors.red + pixelColors.green + pixelColors.blue);
               var newColor = Color.FromArgb(int.Parse(newnumber[0].ToString()), int.Parse(newnumber[1].ToString()), int.Parse(newnumber[2].ToString()));
               bmp.SetPixel(x, y, newColor);
           }
       }

       var ms = new MemoryStream();
       bmp.Save(ms, ImageFormat.Png);
       
       ms.Seek(0, SeekOrigin.Begin);
       Console.WriteLine(ms);
       return ms;
       
    }
    
   
}