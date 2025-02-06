namespace SteganographyBot;

public enum ChatState
{
    Base,              // Состояние без активности
    AwaitingImageAndText,     // Ожидаем изображение для кодирования
    AwaitingDecodeImage, // Ожидаем изображение для декодирования
}