using System.Collections;
using SteganographyBot.Bot;

namespace SteganographyBot;

public class EncryptionHelper
{
     public void ValidateImageCapacity(int imageBitSize, int messageBitSize, bool ignoreAlpha)
    {
        int availableImageSpace;
        int metadataSize = int.Parse(Resources.metadataSize);
        if (ignoreAlpha)
        {
            availableImageSpace = ((imageBitSize / 4) * 3) - metadataSize;
        }
        else
        {
            availableImageSpace = imageBitSize - metadataSize;
        }

        if (availableImageSpace <= 0)
        {
            throw new Exception("The image size is insufficient to store the message size.");
        }

        if (messageBitSize / 2 > availableImageSpace / 8)
        {
            throw new Exception("The message is too large to fit in the image.");
        }
    }

    public static BitArray IntToBitArray(int number)
    {
        byte[] numberInBytes = BitConverter.GetBytes(number);
        BitArray numberInBits = new BitArray(numberInBytes);
        return numberInBits;
    }

    public static void EmbedSizeBits(BitArray imageBits, BitArray sizeBits, int offsetInBits, bool ignoreAlpha)
    {
        int offset = offsetInBits;

        for (int i = 0; i < 16; i++)
        {
            int indexImage = i * 8 + offset;
            int indexSize = i * 2;

            if (ignoreAlpha)
            {
                if ((indexImage + 8) % 32 == 0)
                {
                    offset += 8;
                    i--;
                    continue;
                }
            }

            imageBits[indexImage] = sizeBits[indexSize];
            imageBits[indexImage + 1] = sizeBits[indexSize + 1];
        }
    }

    public static byte[] EmbedMessage(BitArray imageBits, BitArray messageBits,bool ignoreAlpha)
    {
        int offset = 0;
        int size = int.Parse(Resources.metadataSize);

        for (int i = 0; i < imageBits.Count / 8; i++)
        {
            int indexImage = i * 8 + size;
            int indexMessage = i * 2 - offset;

            if (indexMessage >= messageBits.Count)
            {
                break;
            }

            if (ignoreAlpha)
            {
                if ((indexImage + 8) % 32 == 0)
                {
                    offset += 2;
                    continue;
                }
            }

            if (imageBits[indexImage] != messageBits[indexMessage])
            {
                imageBits[indexImage] = messageBits[indexMessage];
            }

            if (imageBits[indexImage + 1] != messageBits[indexMessage + 1])
            {
                imageBits[indexImage + 1] = messageBits[indexMessage + 1];
            }
        }

        byte[] imageEncrypted = new byte[imageBits.Count / 8];
        imageBits.CopyTo(imageEncrypted, 0);

        return imageEncrypted;
    }
}