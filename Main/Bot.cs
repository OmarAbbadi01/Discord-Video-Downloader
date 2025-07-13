using System.Diagnostics;
using Discord;
using Discord.WebSocket;

namespace Main;

public class Bot : IAsyncDisposable
{
    private readonly DiscordSocketClient _client;

    public Bot()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        });

        _client.Log += LogAsync;
        _client.Ready += OnReadyAsync;
        _client.SlashCommandExecuted += OnSlashCommandAsync;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var discordToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        if (discordToken is null)
        {
            Console.WriteLine("Discord token is missing from env!");
            return;
        }
        
        await _client.LoginAsync(TokenType.Bot, discordToken);
        await _client.StartAsync();
        
        try
        {
            // Wait until shutdown is requested
            await Task.Delay(-1, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Graceful shutdown triggered.");
        }
        finally
        {
            await _client.StopAsync();
            await DisposeAsync();
        }
    }

    private static Task LogAsync(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private async Task OnReadyAsync()
    {
        var command = new SlashCommandBuilder()
            .WithName("download")
            .WithDescription("Download a video")
            .AddOption("url", ApplicationCommandOptionType.String, "The video URL to download", isRequired: true);

        try
        {
            await _client.CreateGlobalApplicationCommandAsync(command.Build());
            Console.WriteLine("✅ Slash command synced.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Command sync error: {ex.Message}");
        }
    }

    private static async Task OnSlashCommandAsync(SocketSlashCommand command)
    {
        if (command.Data.Name != "download")
            return;

        var url = command.Data.Options.First().Value.ToString();
        var tempFilename = $"video_{Guid.NewGuid()}.mp4";
        
        try
        {
            await command.DeferAsync(); // "is thinking..."

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"-f mp4 -o \"{tempFilename}\" \"{url}\" --no-playlist --max-filesize 25M",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            var process = Process.Start(processStartInfo);
            await process!.WaitForExitAsync();
            
            await command.FollowupWithFileAsync(
                filePath: tempFilename,
                text: $"Sent by: {command.User.Mention}",
                isTTS: false
            );
        }
        catch (Exception)
        {
            await command.FollowupAsync($"❌ Unable to download video. File could be too large. \n" +
                                        $" URL: {url}. \n" +
                                        $" Sent by: {command.User.Mention}");
        }
        finally
        {
            if (File.Exists(tempFilename))
                File.Delete(tempFilename);

            if (File.Exists(tempFilename + ".part"))
                File.Delete(tempFilename + ".part");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}