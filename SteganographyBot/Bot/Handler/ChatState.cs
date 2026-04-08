namespace SteganographyBot;

public enum ChatState
{
    Base,                         // начальное состояние
    AwaitingPublicKeyForEncode,   // ожидание публичного RSA-ключа для шифрования
    AwaitingImageAndText,         // ожидание изображения и текста для встраивания
    AwaitingPrivateKeyForDecode,  // ожидание приватного RSA-ключа для расшифровки
    AwaitingDecodeImage,          // ожидание изображения для извлечения
}