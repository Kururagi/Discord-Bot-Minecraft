public static class Config
{
    // Discord Bot Token
    //FOR TEST
    //public static string DiscordToken => "";
    //FOR REAL
    public static string DiscordToken => "";

    // Minecraft RCON Settings
    public static string MinecraftServerIP => "";
    public static int MinecraftServerPort => ; // Default RCON port
    public static string RconPassword => "";

    // MySQL Database
    public static string MySqlHost = "";
    public static string MySqlDatabase = "";
    public static string MySqlUsername = "";
    public static string MySqlPassword = "";
    public static int MySqlPort = ; // Default MySQL port

    public static string MySqlConnectionString =>
        $"Server={MySqlHost};Database={MySqlDatabase};User ID={MySqlUsername};Password={MySqlPassword};";
}
