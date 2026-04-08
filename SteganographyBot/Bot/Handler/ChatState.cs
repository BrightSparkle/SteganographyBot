namespace SteganographyBot;

public enum ChatState
{
    Base,                       
    AwaitingPublicKeyForEncode, 
    AwaitingImageAndText,       
    AwaitingPrivateKeyForDecode,
    AwaitingDecodeImage,        
}