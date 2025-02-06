using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace SteganographyBot;

public class CommandHandler
{
    private readonly Dictionary<long, ChatState> _chatStates ;
    private readonly Encryptor _encryptor;
    private readonly Decryptor _decryptor;

    public CommandHandler()
    {
        _chatStates = new Dictionary<long, ChatState>();
        _encryptor = new Encryptor();
        _decryptor = new Decryptor();
    }

    public async void HandleCommand(ITelegramBotClient telegramClient, Update update)
    {
        long chatId = update.Message.Chat.Id;

        // Initialize chat state if it doesn't exist
        if (!_chatStates.ContainsKey(chatId))
        {
            _chatStates[chatId] = ChatState.Base;
        }

        
        
        switch (update.Message.Type)
        {
            
            
            
            case MessageType.Text:
            {
                
                
                
                
                if (_chatStates[chatId] != ChatState.Base)
                {
                    if (_chatStates[chatId] == ChatState.AwaitingImageAndText)
                    {
                        await telegramClient.SendMessage(update.Message.Chat.Id,
                            "Needed image and text for encoding. Going back.");
                        _chatStates[chatId] = ChatState.Base;
                        break;
                    }

                    if (_chatStates[chatId] == ChatState.AwaitingDecodeImage)
                    {
                        await telegramClient.SendMessage(update.Message.Chat.Id,
                            "Needed image for decoding. Going back.");
                        _chatStates[chatId] = ChatState.Base;
                        break;
                    }
                }
                
                
                
                
                
                
                switch (update.Message.Text)
                {
                    case "/help":
                    {   //TODO нафигачь описания
                        await telegramClient.SendMessage(update.Message.Chat.Id,
                            "This bot can encode and decode messages in images.");
                        break;
                    }

                    case "/encode":
                    {
                        // Change state to expect an image with a caption for encoding
                        await telegramClient.SendMessage(update.Message.Chat.Id,
                            "Please send an image with a caption for encoding.");
                        _chatStates[chatId] = ChatState.AwaitingImageAndText;
                        break;
                    }

                    case "/decode":
                    {
                        // Change state to expect only an image for decoding
                        await telegramClient.SendMessage(update.Message.Chat.Id,
                            "Please send an image for decoding.");
                        _chatStates[chatId] = ChatState.AwaitingDecodeImage;
                        break;
                    }

                    default:
                    {
                        await telegramClient.SendMessage(update.Message.Chat.Id, "Unknown command. Please use /help.");
                        break;
                    }
                        
                }

                break;
            }
            
            
            
            
            

            case MessageType.Photo:
            {
                    
                await HandlePhotoAsync(telegramClient, update);

                
                break;
            }
            default:
            {
                await telegramClient.SendMessage(update.Message.Chat.Id, "Wrong message type. Please use /help.");
                break;
            }
            
            //рофло-комментарий
            
        }
        
    }

    
    
    
    
    
    public async Task HandlePhotoAsync(ITelegramBotClient botClient, Update update)
    {
        
        long chatId = update.Message.Chat.Id;

        
        // Check if the user is in the correct state to receive an image
        if (!_chatStates.ContainsKey(chatId) || _chatStates[chatId] == ChatState.Base)
        {
            await botClient.SendMessage(chatId, "Please use /encode or /decode or /help to start the process.");
            return;
        }
        

        if (_chatStates[chatId]== ChatState.AwaitingImageAndText)
        {
            // Handling the image for encoding (same as before)
            if (update.Message.Type == MessageType.Photo)
            {
                var photo = update.Message.Photo[0]; // Get the largest photo

                // Download the file
                var file = await botClient.GetFileAsync(photo.FileId);
                using (var fileStream = new MemoryStream())
                {
                    await botClient.DownloadFile(file.FilePath, fileStream);
                    byte[] imageBytes = fileStream.ToArray();

                    // Now you can process the image and caption
                    string text = update.Message.Caption; // The caption (encoded message or data)

                    if (text != null && text.Length > 0)
                    {
                        //ТУТ ДЕЛАЕМ ЭНКОД
                        byte[] encoded = _encryptor.EncryptStringMessage(imageBytes, Encoding.UTF8.GetBytes(text),false);
                        // Send back the image and caption for now (encoding response)
                        await botClient.SendPhotoAsync(
                            chatId: chatId,
                            photo: new InputFileStream(new MemoryStream(encoded), "encoded_image.png"),
                            caption: $"Received image for encoding: {text}"
                        );
                    }
                    else
                    {
                        botClient.SendMessage(chatId, "Please send an image with not empty caption for encoding.");
                    }

                    // Reset state to Idle after processing
                    _chatStates[chatId] = ChatState.Base;
                }
            }
            else
            {
                await botClient.SendMessage(chatId, "Please send an image with a caption for encoding.");
            }
        }
        else if (_chatStates[chatId] == ChatState.AwaitingDecodeImage)
        {
            // Handling the image for decoding
            if (update.Message.Type == MessageType.Photo)
            {
                var photo = update.Message.Photo[0]; // Get the largest photo

                // Download the file
                var file = await botClient.GetFileAsync(photo.FileId);
                using (var fileStream = new MemoryStream())
                {
                    await botClient.DownloadFile(file.FilePath, fileStream);
                    byte[] imageBytes = fileStream.ToArray();
                    string text = update.Message.Caption;
                    // Now you can process the image (decoding logic here)
                    // For demonstration, we'll just send the image back
                    if (String.IsNullOrEmpty(text))
                    {
                        byte[] decoded = _decryptor.DecryptStringMessage(imageBytes, false);
                        await botClient.SendPhotoAsync(
                            chatId: chatId,
                            photo: new InputFileStream(new MemoryStream(decoded), "decoded_image.png"),
                            caption: "Image received for decoding."
                        );
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "Please send an image without caption for decoding.");
                    }

                    // Reset state to Idle after processing
                    _chatStates[chatId] = ChatState.Base;
                }
            }
        }
    }
}
