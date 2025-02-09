using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace SteganographyBot;

public class CommandHandler
{
    private readonly Dictionary<long, ChatState> _chatStates;
    private readonly Encryptor _encryptor;
    private readonly Decryptor _decryptor;

    public CommandHandler()
    {
        _chatStates = new Dictionary<long, ChatState>();
        _encryptor = new Encryptor();
        _decryptor = new Decryptor();
    }

    // Entry point to handle incoming messages
    public async void HandleCommand(ITelegramBotClient telegramClient, Update update)
    {
        var chatId = update.Message.Chat.Id;

        // Initialize chat state if it doesn't exist
        InitializeChatState(chatId);

        switch (update.Message.Type)
        {
            case MessageType.Text:
                await HandleTextMessage(telegramClient, update, chatId);
                break;

            case MessageType.Photo:
                await HandlePhotoAsync(telegramClient, update, chatId);
                break;

            default:
                await telegramClient.SendMessage(update.Message.Chat.Id, "Wrong message type. Please use /help.");
                _chatStates[chatId] = ChatState.Base;
                break;
        }
    }

    // Initialize chat state
    private void InitializeChatState(long chatId)
    {
        if (!_chatStates.ContainsKey(chatId)) _chatStates[chatId] = ChatState.Base;
    }


    // Handle text messages (commands)
    private async Task HandleTextMessage(ITelegramBotClient telegramClient, Update update, long chatId)
    {
        if (_chatStates[chatId] != ChatState.Base)
        {
            await HandleInvalidStateMessage(telegramClient, update, chatId);
            return;
        }

        switch (update.Message.Text)
        {
            case "/start":
                break;

            case "/help":
                await SendHelpMessage(telegramClient, update);
                break;

            case "/encode":
                await HandleEncodeCommand(telegramClient, update, chatId);
                break;

            case "/decode":
                await HandleDecodeCommand(telegramClient, update, chatId);
                break;

            default:
                await telegramClient.SendMessage(update.Message.Chat.Id, "Unknown command. Please use /help.");
                break;
        }
    }


    // Send help message
    private async Task SendHelpMessage(ITelegramBotClient telegramClient, Update update)
    {
        await telegramClient.SendMessage(update.Message.Chat.Id, "This bot can encode and decode messages in images.");
    }


    // Handle /encode command
    private async Task HandleEncodeCommand(ITelegramBotClient telegramClient, Update update, long chatId)
    {
        await telegramClient.SendMessage(update.Message.Chat.Id, "Please send an image with a caption for encoding.");
        _chatStates[chatId] = ChatState.AwaitingImageAndText;
    }


    // Handle /decode command
    private async Task HandleDecodeCommand(ITelegramBotClient telegramClient, Update update, long chatId)
    {
        await telegramClient.SendMessage(update.Message.Chat.Id, "Please send an image for decoding.");
        _chatStates[chatId] = ChatState.AwaitingDecodeImage;
    }


    // Handle invalid state (if a command is issued in an invalid state)
    private async Task HandleInvalidStateMessage(ITelegramBotClient telegramClient, Update update, long chatId)
    {
        if (_chatStates[chatId] == ChatState.AwaitingImageAndText)
            await telegramClient.SendMessage(update.Message.Chat.Id, "Needed image and text for encoding. Going back.");
        else if (_chatStates[chatId] == ChatState.AwaitingDecodeImage)
            await telegramClient.SendMessage(update.Message.Chat.Id, "Needed image for decoding. Going back.");
        _chatStates[chatId] = ChatState.Base;
    }

    // Handle photo messages for encoding and decoding
    private async Task HandlePhotoAsync(ITelegramBotClient botClient, Update update, long chatId)
    {
        if (!_chatStates.ContainsKey(chatId) || _chatStates[chatId] == ChatState.Base)
        {
            await botClient.SendMessage(chatId, "Please use /encode or /decode or /help to start the process.");
            return;
        }


        if (_chatStates[chatId] == ChatState.AwaitingImageAndText)
            await ProcessImageForEncoding(botClient, update, chatId);
        else if (_chatStates[chatId] == ChatState.AwaitingDecodeImage)
            await ProcessImageForDecoding(botClient, update, chatId);
    }


    // Process image for encoding
    private async Task ProcessImageForEncoding(ITelegramBotClient botClient, Update update, long chatId)
    {
        var photo = update.Message.Photo[0];
        var file = await botClient.GetFile(photo.FileId);

        using (var fileStream = new MemoryStream())
        {
            await botClient.DownloadFile(file.FilePath, fileStream);
            var imageBytes = fileStream.ToArray();
            var caption = update.Message.Caption;

            if (!string.IsNullOrEmpty(caption))
            {
                var encodedImage = _encryptor.EncryptStringMessage(imageBytes, Encoding.UTF8.GetBytes(caption), false);
                await SendEncodedImage(botClient, chatId, encodedImage, caption);
            }
            else
            {
                await botClient.SendMessage(chatId, "Please send an image with a non-empty caption for encoding.");
            }

            _chatStates[chatId] = ChatState.Base;
        }
    }


    // Send encoded image
    public async Task SendEncodedImage(ITelegramBotClient botClient, long chatId, byte[] encodedImage, string caption)
    {
        try
        {
            // Попытка отправить изображение
            using (var stream = new MemoryStream(encodedImage))
            {
                var photo = new InputFileStream(stream, "encoded_image.jpg");
                await botClient.SendDocument(chatId, photo, caption);
            }
        }
        catch (Exception ex)
        {
            // Логирование ошибки Telegram Bot API
            Console.WriteLine($"Ошибка Telegram API: {ex.Message}");
            // Дополнительно можно отправить сообщение об ошибке администратору
            await botClient.SendMessage(chatId, "Произошла ошибка при обработке изображения.");
        }
    }


    // Process image for decoding
    private async Task ProcessImageForDecoding(ITelegramBotClient botClient, Update update, long chatId)
    {
        var photo = update.Message.Photo?[0];
        var file = await botClient.GetFile(photo?.FileId);

        using (var fileStream = new MemoryStream())
        {
            await botClient.DownloadFile(file.FilePath, fileStream);
            var imageBytes = fileStream.ToArray();

            if (string.IsNullOrEmpty(update.Message.Caption))
            {
                var decodedText = Encoding.UTF8.GetString(_decryptor.DecryptStringMessage(imageBytes, false));
                await botClient.SendMessage(chatId, decodedText);
            }
            else
            {
                await botClient.SendMessage(chatId, "Please send an image without caption for decoding.");
            }

            _chatStates[chatId] = ChatState.Base;
        }
    }
}

