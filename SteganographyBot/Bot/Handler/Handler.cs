using Telegram.Bot;
using Telegram.Bot.Types;

namespace SteganographyBot;

    public class Handler
    {
        
        private CommandHandler _commandHandler;

        public Handler()
        {
            _commandHandler = new CommandHandler();
        }
        public async Task HandleUpdateAsync(ITelegramBotClient telegramClient, Update update,
            CancellationToken cancellationToken)
        {
            // Обработка обновления
            Console.WriteLine($"Получено обновление: {update}");
           _commandHandler.HandleCommand(telegramClient, update);
        }

        public async Task HandleErrorAsync(ITelegramBotClient telegramClient, Exception exception,
            CancellationToken cancellationToken)
        {
            // Обработка ошибки
            Console.WriteLine($"Ошибка: {exception.Message}");
        }
    }
