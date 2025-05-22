namespace SteganographyBot;

public enum ChatState
{
    Base,              // Inactive state
    AwaitingImageAndText,     // Waiting for image and text to encode
    AwaitingDecodeImage, // Waiting for image to decode
}