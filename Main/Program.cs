namespace Main;

public class Program
{
    public static async Task Main(string[] args)
    {
        var bot = new Bot();
        await bot.RunAsync();
    }
}