using System.Collections;
using System.Drawing;
using SteganographyBot.Bot;
using StegBot.Bot;

namespace SteganographyBot;

public class Decryptor
{
    private readonly string _startPoint = "@startpoint";
    private readonly string _endPoint = "@endpoint";
    public Decryptor()
    {
        
    }
    
    
    public async Task<string> DecodeImage(Stream image)
    {
      await image.CopyToAsync(image);
        Bitmap img = new Bitmap(image);

        //holds the new bits extract from image
        string bits = "";
        string extractedtext = "";
        bool shouldBreak = false;
        for (int x = 0; x < img.Width; x++)
        {
            if (shouldBreak)
            {
                break;
            }

            for (int y = 0; y < img.Height; y++)
            {

                if (extractedtext.Length >= _startPoint.Length && !extractedtext.Contains(_startPoint))
                {
                    //nothin in the picture
                    shouldBreak = true;
                    extractedtext = "No content found, Please send image with original format as file.";
                    break;
                }

                if (extractedtext.Contains(_endPoint))
                {
                    extractedtext = extractedtext
                        .Replace(_startPoint, string.Empty)
                        .Replace(_endPoint, string.Empty);

                    shouldBreak = true;
                    break;
                }

                Color pixel = img.GetPixel(x, y);
                var colors = SteganographyHelper.ToBinary(pixel);
                //read each pixel rgb first bits
                bits += SteganographyHelper.ReadFirstBits(colors.red,2) + SteganographyHelper.ReadFirstBits(colors.green,2) + SteganographyHelper.ReadFirstBits(colors.blue,2);
                //if it isnt default

                    if (bits.Length >= 32)
                    {
                        extractedtext += SteganographyHelper.GetUnicodeTextFromBinary(bits.Substring(0, 32));
                        bits = bits.Remove(0, 32);
                    }
                    Console.WriteLine(extractedtext);
                }
               
            
        }

        return extractedtext;
    }
    
}