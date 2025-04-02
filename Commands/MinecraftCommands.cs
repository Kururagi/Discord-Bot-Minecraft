using Rcon;

public static class MinecraftCommands
{
    private static RconClient rcon;
    private static bool isInitialized = false;

    public static async Task InitializeRcon()
    {
        try
        {
            rcon = new RconClient();
            await rcon.ConnectAsync(Config.MinecraftServerIP, Config.MinecraftServerPort);
            await rcon.AuthenticateAsync(Config.RconPassword);
            isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RCON Initialization Error: {ex.Message}");
            isInitialized = false;
        }
    }

    public static async Task SendMinecraftCommand(string username, GachaItem item)
    {
        try
        {
            // Make sure rcon is initialized
            if (!isInitialized || rcon == null)
            {
                await InitializeRcon();
                if (!isInitialized) return; // Failed to initialize
            }

            // Announce reward
            await rcon.SendCommandAsync($"say {username} received {item.Name}!");

            // Send command from database
            var formattedCommand = item.Command.Replace("{username}", username);
            await rcon.SendCommandAsync(formattedCommand);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending Minecraft command: {ex.Message}");
            // Mark as not initialized so we'll try to reconnect next time
            isInitialized = false;
        }
    }

    public static async Task<bool> IsPlayerOnline(string username)
    {
        try
        {
            if (!isInitialized || rcon == null)
            {
                await InitializeRcon();
                if (!isInitialized) return false;
            }

            // ใช้คำสั่งที่แม่นยำกว่าในการตรวจสอบผู้เล่นเฉพาะคน
            var response = await rcon.SendCommandAsync($"data get entity {username} Pos");

            // ถ้าคำสั่งสำเร็จจะได้ค่าตำแหน่งกลับมา
            return !response.Contains("No entity was found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RCON Error: {ex.Message}");
            isInitialized = false;
            return false;
        }
    }
}