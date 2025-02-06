using System.Collections;
using SteganographyBot.Bot;

namespace SteganographyBot;

public class DectyptorHelper
{
    public byte[] ExtractHiddenMessage(BitArray imageBits, BitArray messageBits , bool excludeAlpha)
{
    // Начинаем извлечение данных после первых 24 байтов, которые содержат размер сообщения.
    int startOffset = int.Parse(Resources.metadataSize);

    // Для каждого бита, который содержит сообщение:
    for (int i = 0; i < messageBits.Count / 2; i++)
    {
        int imageIndex = i * 8 + startOffset;
        int messageIndex = i * 2;

        if(imageIndex >= imageBits.Count || messageIndex >= messageBits.Count)
        {
            throw new Exception("Ошибка при извлечении сообщения.\n" +
                "Пожалуйста, убедитесь, что выбранное изображение действительно содержит сообщение.");
        }

        if (excludeAlpha)
        {
            // Пропускаем альфа-канал
            if ((imageIndex + 8) % 32 == 0)
            {
                startOffset += 8;
                i--;
                continue;
            }
        }

        // Копируем биты изображения в соответствующие биты сообщения
        messageBits[messageIndex] = imageBits[imageIndex + 0];
        messageBits[messageIndex + 1] = imageBits[imageIndex + 1];
    }

    byte[] decryptedMessage = new byte[messageBits.Count / 8];

    // Преобразуем биты в байты
    messageBits.CopyTo(decryptedMessage, 0);

    return decryptedMessage;
}

public int RetrieveMessageSize(BitArray image, int offset, bool excludeAlpha)
{
    // Размер сообщения не должен превышать 4 байта (32 бита)
    BitArray sizeBits = new BitArray(32);

    int currentOffset = offset;

    // Для каждого бита в массиве размера, берем по два бита за раз:
    for (int i = 0; i < sizeBits.Count / 2; i++)
    {
        int currentByte = i * 8 + currentOffset;
        int sizeBitIndex = i * 2;

        if (excludeAlpha)
        {
            // Пропускаем альфа-канал
            if ((currentByte + 8) % 32 == 0)
            {
                currentOffset += 8;
                i--;
                continue;
            }
        }

        // Извлекаем биты, которые содержат информацию о размере
        sizeBits[sizeBitIndex] = image[currentByte];
        sizeBits[sizeBitIndex + 1] = image[currentByte + 1];
    }

    byte[] sizeByteArray = new byte[sizeBits.Count / 8];

    sizeBits.CopyTo(sizeByteArray, 0);

    // Преобразуем байты в целое число (размер сообщения)
    int messageSize = BitConverter.ToInt32(sizeByteArray, 0);

    return messageSize;
}
}