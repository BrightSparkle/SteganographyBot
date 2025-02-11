using System.Drawing;
using System.Text;
using StegBot.Bot;
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
            
            case MessageType.Document:
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
        {
            await ProcessImageForEncoding(botClient, update, chatId);
            _chatStates[chatId] = ChatState.Base;
        }
        else if (_chatStates[chatId] == ChatState.AwaitingDecodeImage)
        {
            await ProcessImageForDecoding(botClient, update, chatId);
            _chatStates[chatId] = ChatState.Base;
        }
    }


    // Process image for encoding
    private async Task ProcessImageForEncoding(ITelegramBotClient botClient, Update update, long chatId)
    {
        var image = new MemoryStream();
        var photo = update.Message.Photo.Last();
        var file = await botClient.GetFileAsync(photo.FileId);
        await botClient.DownloadFile(file.FilePath!, image);
        string caption = update.Message.Caption;

        botClient.SendPhoto(update.Message.Chat.Id, image);
        botClient.SendMessage(update.Message!.Chat.Id,"Start encoding ...");
        
        var result = await _encryptor.EncryptStringMessageIntoImage(image, caption);
        InputFileStream encodedPicutre = new(result, DateTime.Now + ".png");
        
        Message sentMessage = await botClient.SendDocumentAsync(update.Message!.Chat.Id,encodedPicutre);

    }


 

    // Process image for decoding
    private async Task ProcessImageForDecoding(ITelegramBotClient botClient, Update update, long chatId)
    {
        var image = new MemoryStream();
        var photo = update!.Message!.Document!;
        var file = await botClient.GetFileAsync(photo.FileId);
        await botClient.DownloadFile(file.FilePath!, image);
        String caption = update.Message.Caption;

        if (String.IsNullOrEmpty(caption))
        {
            botClient.SendTextMessageAsync(update.Message!.Chat.Id,"Start decoding ...");
            var result = await _decryptor.DecodeImage(image);
            
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: update.Message!.Chat.Id,
                text: result);
        }
    }
}

