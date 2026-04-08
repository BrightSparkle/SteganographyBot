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
    private readonly Dictionary<long, string> _tempPublicKeys;   // chatId -> public key XML
    private readonly Dictionary<long, string> _tempPrivateKeys;  // chatId -> private key XML
    private readonly Encryptor _encryptor;
    private readonly Decryptor _decryptor;

    public CommandHandler()
    {
        _chatStates = new Dictionary<long, ChatState>();
        _tempPublicKeys = new Dictionary<long, string>();
        _tempPrivateKeys = new Dictionary<long, string>();
        _encryptor = new Encryptor();
        _decryptor = new Decryptor();
    }

    public async void HandleCommand(ITelegramBotClient telegramClient, Update update)
    {
        var chatId = update.Message.Chat.Id;
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
                await telegramClient.SendMessage(chatId, "Wrong message type. Use /help.");
                _chatStates[chatId] = ChatState.Base;
                break;
        }
    }

    private void InitializeChatState(long chatId)
    {
        if (!_chatStates.ContainsKey(chatId))
            _chatStates[chatId] = ChatState.Base;
    }

    private async Task HandleTextMessage(ITelegramBotClient telegramClient, Update update, long chatId)
    {
        // Обработка ввода ключей
        if (_chatStates[chatId] == ChatState.AwaitingPublicKeyForEncode)
        {
            _tempPublicKeys[chatId] = update.Message.Text;
            await telegramClient.SendMessage(chatId, "Public key saved. Now send an image with a caption to encode.");
            _chatStates[chatId] = ChatState.AwaitingImageAndText;
            return;
        }
        if (_chatStates[chatId] == ChatState.AwaitingPrivateKeyForDecode)
        {
            _tempPrivateKeys[chatId] = update.Message.Text;
            await telegramClient.SendMessage(chatId, "Private key saved. Now send the image to decode.");
            _chatStates[chatId] = ChatState.AwaitingDecodeImage;
            return;
        }

        if (_chatStates[chatId] != ChatState.Base)
        {
            await HandleInvalidStateMessage(telegramClient, update, chatId);
            return;
        }

        switch (update.Message.Text)
        {
            case "/start":
                await telegramClient.SendMessage(chatId, "Welcome! Use /help for commands.");
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
            case "/genkeys":
                await GenerateKeyPair(telegramClient, update, chatId);
                break;
            default:
                await telegramClient.SendMessage(chatId, "Unknown command. Use /help.");
                break;
        }
    }

    private async Task SendHelpMessage(ITelegramBotClient telegramClient, Update update)
    {
        await telegramClient.SendMessage(update.Message.Chat.Id,
            "Commands:\n" +
            "/encode - encode a message into an image (you will be asked for RSA public key and image with caption)\n" +
            "/decode - extract a message from an image (you will be asked for RSA private key and image)\n" +
            "/genkeys - generate a new RSA key pair (public + private)\n" +
            "/help - show this help");
    }

    private async Task HandleEncodeCommand(ITelegramBotClient telegramClient, Update update, long chatId)
    {
        await telegramClient.SendMessage(chatId, "Please send the recipient's RSA public key (in XML format).");
        _chatStates[chatId] = ChatState.AwaitingPublicKeyForEncode;
    }

    private async Task HandleDecodeCommand(ITelegramBotClient telegramClient, Update update, long chatId)
    {
        await telegramClient.SendMessage(chatId, "Please send your RSA private key (in XML format).");
        _chatStates[chatId] = ChatState.AwaitingPrivateKeyForDecode;
    }

    private async Task GenerateKeyPair(ITelegramBotClient telegramClient, Update update, long chatId)
    {
        var (publicKey, privateKey) = CryptoHelper.GenerateRsaKeyPair();
        await telegramClient.SendMessage(chatId, $"Your PUBLIC key:\n```\n{publicKey}\n```", parseMode: ParseMode.Markdown);
        await telegramClient.SendMessage(chatId, $"Your PRIVATE key (keep secret!):\n```\n{privateKey}\n```", parseMode: ParseMode.Markdown);
    }

    private async Task HandleInvalidStateMessage(ITelegramBotClient telegramClient, Update update, long chatId)
    {
        await telegramClient.SendMessage(chatId, "Operation cancelled due to invalid input. Starting over.");
        _chatStates[chatId] = ChatState.Base;
        _tempPublicKeys.Remove(chatId);
        _tempPrivateKeys.Remove(chatId);
    }

    private async Task HandlePhotoAsync(ITelegramBotClient botClient, Update update, long chatId)
    {
        if (!_chatStates.ContainsKey(chatId) || _chatStates[chatId] == ChatState.Base)
        {
            await botClient.SendMessage(chatId, "Please use /encode or /decode first.");
            return;
        }

        if (_chatStates[chatId] == ChatState.AwaitingImageAndText)
        {
            await ProcessImageForEncoding(botClient, update, chatId);
            _chatStates[chatId] = ChatState.Base;
            _tempPublicKeys.Remove(chatId);
        }
        else if (_chatStates[chatId] == ChatState.AwaitingDecodeImage)
        {
            await ProcessImageForDecoding(botClient, update, chatId);
            _chatStates[chatId] = ChatState.Base;
            _tempPrivateKeys.Remove(chatId);
        }
        else
        {
            await botClient.SendMessage(chatId, "Unexpected state. Please start over with /encode or /decode.");
            _chatStates[chatId] = ChatState.Base;
        }
    }

    private async Task ProcessImageForEncoding(ITelegramBotClient botClient, Update update, long chatId)
    {
        if (!_tempPublicKeys.TryGetValue(chatId, out string publicKeyXml))
        {
            await botClient.SendMessage(chatId, "Public key missing. Start again with /encode.");
            return;
        }

        var imageStream = new MemoryStream();
        var photo = update.Message.Photo?.Last();
        if (photo == null)
        {
            await botClient.SendMessage(chatId, "No photo found. Please send an image.");
            return;
        }

        var file = await botClient.GetFileAsync(photo.FileId);
        await botClient.DownloadFile(file.FilePath!, imageStream);
        string caption = update.Message.Caption;
        if (string.IsNullOrEmpty(caption))
        {
            await botClient.SendMessage(chatId, "Please provide a caption (the secret message).");
            return;
        }

        await botClient.SendMessage(chatId, "Encoding with hybrid RSA+AES...");
        var result = await _encryptor.EncryptStringMessageIntoImage(imageStream, caption, publicKeyXml);
        var encodedFile = new InputFileStream(result, $"{DateTime.Now.Ticks}.png");
        await botClient.SendDocumentAsync(chatId, encodedFile);
        await botClient.SendMessage(chatId, "Encoding completed!");
    }

    private async Task ProcessImageForDecoding(ITelegramBotClient botClient, Update update, long chatId)
    {
        if (!_tempPrivateKeys.TryGetValue(chatId, out string privateKeyXml))
        {
            await botClient.SendMessage(chatId, "Private key missing. Start again with /decode.");
            return;
        }

        var imageStream = new MemoryStream();
        // Поддерживаем как Document, так и Photo
        if (update.Message.Document != null)
        {
            var file = await botClient.GetFileAsync(update.Message.Document.FileId);
            await botClient.DownloadFile(file.FilePath!, imageStream);
        }
        else if (update.Message.Photo != null)
        {
            var file = await botClient.GetFileAsync(update.Message.Photo.Last().FileId);
            await botClient.DownloadFile(file.FilePath!, imageStream);
        }
        else
        {
            await botClient.SendMessage(chatId, "Please send an image as a document or photo.");
            return;
        }

        await botClient.SendMessage(chatId, "Decoding...");
        string result = await _decryptor.DecodeImage(imageStream, privateKeyXml);
        await botClient.SendMessage(chatId, $"Decoded message:\n{result}");
    }
}