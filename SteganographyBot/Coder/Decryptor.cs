using System.Collections;
using SteganographyBot.Bot;

namespace SteganographyBot;

public class Decryptor
{
    private DectyptorHelper _dectyptorHelper;

    public Decryptor()
    {
        _dectyptorHelper = new DectyptorHelper();
    }
    
    public byte[] DecryptStringMessage(byte[] image, bool skipAlpha)
    {
        //Transfome bytes to bits.
        BitArray imageBits = new BitArray(image);

        //Get the size of the message we'll have to decrypt, at the beginning of the image.
        int messageSize = _dectyptorHelper.RetrieveMessageSize(imageBits, 0, skipAlpha);

        //Empty array of bits the size of the message size we just got.
        BitArray messageBits = new BitArray(messageSize);

        //Decrypts the message.

        byte[] messageDecrypted = _dectyptorHelper.ExtractHiddenMessage(imageBits, messageBits, skipAlpha);

        return messageDecrypted;
    }
}