public static class Config
{
    // Discord Bot Token
    //FOR TEST
    //public static string DiscordToken => "";
    //FOR REAL
    public static string DiscordToken => "";

    // Minecraft RCON Settings
    public static string MinecraftServerIP => "45.154.24.114";
    public static int MinecraftServerPort => 25575; // Default RCON port
    public static string RconPassword => "1956312207";

    // MySQL Database
    public static string MySqlHost = "45.154.24.114";
    public static string MySqlDatabase = "minecraft";
    public static string MySqlUsername = "admin";
    public static string MySqlPassword = "1956312207";
    public static int MySqlPort = 3306; // Default MySQL port

    public static string MySqlConnectionString =>
        $"Server={MySqlHost};Database={MySqlDatabase};User ID={MySqlUsername};Password={MySqlPassword};";
}
