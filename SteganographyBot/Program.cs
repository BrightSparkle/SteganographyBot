using Microsoft.AspNetCore.Authentication;
using SteganographyBot.Bot;

namespace SteganographyBot;

 class Program
{
    private static void Main(string[] args)
    {
        BotClient client = new BotClient(Resources.token);
        client.Start();
        
        
    }
}