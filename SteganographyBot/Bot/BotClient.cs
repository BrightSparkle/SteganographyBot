using Telegram.Bot;


namespace SteganographyBot.Bot;

public class BotClient
{
    private readonly TelegramBotClient _client;

    private Handler _handler;

    public BotClient(String token)
    {
        _client = new TelegramBotClient(token);
        _handler = new Handler();
    }

    public void Start()
    {
        _client.StartReceiving(_handler.HandleUpdateAsync, _handler.HandleErrorAsync);
        
        while (true)
        {
            Task.Delay(1000);
        }
    }
    
    
}