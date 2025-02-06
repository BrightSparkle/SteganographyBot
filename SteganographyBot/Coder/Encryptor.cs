using System.Collections;
using System.Drawing;
using System.Net.Mime;
using SteganographyBot.Bot;

namespace SteganographyBot;

public class Encryptor
{
    private EncryptionHelper _encryptionHelper;

    public Encryptor()
    {
        _encryptionHelper = new EncryptionHelper();
    }
    
    public byte[] EncryptStringMessage(byte[] image, byte[] message, bool skipAlpha)
    {
        //Convert the byte arrays to bit arrays.
        BitArray imageBits = new BitArray(image);
        BitArray messageBits = new BitArray(message);

        //The image needs at least 24 bytes to hold the message size information.
        //The message will begin at the 24th byte of the image (192 bits).

        _encryptionHelper.ValidateImageCapacity(imageBits.Count, messageBits.Count, skipAlpha);

        //In the first 24 bytes set the size of the message we are going to hide, 
        //this will be used later when decrypting:

        //Transform the message size into a bit array
        BitArray sizeInBits = EncryptionHelper.IntToBitArray(messageBits.Count);

        //DEFINE SIZE

        EncryptionHelper.EmbedSizeBits(imageBits, sizeInBits, 0, skipAlpha);

        //DEFINE MESSAGE

        byte[] imageEncrypted = EncryptionHelper.EmbedMessage(imageBits, messageBits, skipAlpha);

        return imageEncrypted;
    }
    
   
}