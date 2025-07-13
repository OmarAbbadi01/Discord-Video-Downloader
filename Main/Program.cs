namespace Main;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var cancellationTokenSource = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            Console.WriteLine("Shutdown requested...");
            cancellationTokenSource.Cancel();
            e.Cancel = true;
        };
        
        var bot = new Bot();
        await bot.RunAsync(cancellationTokenSource.Token);
    }
}