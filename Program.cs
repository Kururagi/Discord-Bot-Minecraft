using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using MySql.Data.MySqlClient;
using System.Data;
using Rcon;
using Newtonsoft.Json;
using System.Text;
using SixLabors.ImageSharp;
using static Org.BouncyCastle.Math.EC.ECCurve;

class Program
{
    static DiscordClient discord;
    static InteractivityExtension interactivity;
    static RconClient rcon;

    static async Task Main(string[] args)
    {
        // Configure Discord Bot
        discord = new DiscordClient(new DiscordConfiguration
        {
            Token = Config.DiscordToken,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
            AutoReconnect = true
        });

        interactivity = discord.UseInteractivity(new InteractivityConfiguration
        {
            Timeout = TimeSpan.FromMinutes(5)
        });

        // Setup RCON Client
        rcon = new RconClient(); // กำหนดค่า rcon
        try
        {
            await rcon.ConnectAsync(Config.MinecraftServerIP, Config.MinecraftServerPort);
            await rcon.AuthenticateAsync(Config.RconPassword);
            Console.WriteLine("RCON connected successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RCON connection failed: {ex.Message}");
            return;
        }

        // Initialize AdminCommands ก่อนเชื่อมต่อ
        AdminCommands.Initialize(discord);
        UserCommands.Initialize(discord);

        // Setup Event Handlers
        discord.MessageCreated += MessageCreated.OnMessageCreated;
        discord.ComponentInteractionCreated += VerifySystem.OnVerifyButtonClicked;
        discord.ModalSubmitted += ModalSubmitted.OnModalSubmitted;
        discord.ComponentInteractionCreated += InteractionCreated.OnComponentInteractionCreated;

        // Connect Discord
        await discord.ConnectAsync();
        Console.WriteLine("Discord connected.");

        // Graceful shutdown handling
        var shutdown = new ManualResetEventSlim();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            shutdown.Set();
        };
        shutdown.Wait();

        // Cleanup
        await discord.DisconnectAsync();
    }
}