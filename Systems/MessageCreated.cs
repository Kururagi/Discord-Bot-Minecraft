using DSharpPlus.EventArgs;
using DSharpPlus;

public static class MessageCreated
{

    public static async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
    {
        if (e.Author.IsBot) return;

        if (e.Message.Content.StartsWith("-verifychannelcreate"))
        {
            await VerifySystem.verifychannelcreate(e);
        }
        else if (e.Message.Content.StartsWith("-userpanelcreate"))
        {
            await UserCommands.userpanelcreate(e);
        }
        else if (e.Message.Content.StartsWith("-adminpanelcreate"))
        {
            await AdminCommands.AdminPanelCreate(e);
        }
    }
}