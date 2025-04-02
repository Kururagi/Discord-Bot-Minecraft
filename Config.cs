public static class Config
{
    // Discord Bot Token
    //FOR TEST
    //public static string DiscordToken => "ODE2OTgwNjI0NzEwODI4MDYz.G0z0wD.6e1XD4p9ZlvDwuRTwly8qZx5teK1mbLdOw36To";
    //FOR REAL
    public static string DiscordToken => "MTM0NjUxNjIzMDUwODUxNTM4OQ.Gjk43q.Xto0-i5xUVOeDfgAYnB1RVE7E3dUkkT24rTRqk";

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