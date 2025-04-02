using DSharpPlus.Entities;
using DSharpPlus;
using Newtonsoft.Json;
using static Program;

public static class TopupSystem
{
    public static readonly Dictionary<ulong, string> _paymentSessions = new();
    public static async Task ShowTopupModal(DiscordInteraction interaction)
    {
        try
        {
            var modal = new DiscordInteractionResponseBuilder()
                .WithTitle("เติมเงิน")
                .WithCustomId("topup_modal")
                .AddComponents(new TextInputComponent(
                    label: "จำนวนเงินที่ต้องการเติม (บาท)",
                    customId: "topup_amount",
                    placeholder: "กรอกจำนวนเงินเป็นตัวเลขเท่านั้น",
                    required: true,
                    style: TextInputStyle.Short,
                    min_length: 1,
                    max_length: 6));

            await interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating topup modal: {ex}");
            await interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("❌ เกิดข้อผิดพลาดในการเปิดฟอร์มเติมเงิน")
                    .AsEphemeral(true));
        }
    }

    public static async Task HandleTopupModal(DiscordInteraction interaction, string amount)
    {
        try
        {
            await interaction.DeferAsync(true);

            // Validate amount
            if (!int.TryParse(amount, out var topupAmount) || topupAmount <= 0)
            {
                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder()
                        .WithContent("❌ จำนวนเงินต้องเป็นตัวเลขบวก"));
                return;
            }

            // Generate payment reference ID
            var paymentId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            var userId = interaction.User.Id;

            // Store payment session
            _paymentSessions[userId] = JsonConvert.SerializeObject(new PaymentSession
            {
                Amount = topupAmount,
                PaymentId = paymentId,
                Timestamp = DateTime.UtcNow
            });

            // สร้างคำแนะนำการเติมเงิน
            var embed = new DiscordEmbedBuilder()
                .WithTitle("💰 คำสั่งการเติมเงิน")
                .WithDescription($"รหัสอ้างอิง: `{paymentId}`")
                .AddField("จำนวนเงิน", $"{topupAmount} บาท", true)
                .AddField("โอนเงินผ่านธนาคาร", "418-1-54792-8 กรุงศรี\n" + "วิชญ์ภาส สิทธิโรจน์", true) // เปลี่ยนเป็นข้อมูลจริง
                .AddField("โอนเงินผ่าน TrueWallet", "063-769-3895\n" + "เอกพล จิรสถิตพาณิชย์", true) // เปลี่ยนเป็นข้อมูลจริง
                .AddField("ขั้นตอน", "1. ชำระเงินตามจำนวนที่กำหนด\n" +
                                  "2. เปิด Ticket และ ส่งสลิปในTicket")
                .WithColor(DiscordColor.Gold)
                .WithFooter("ระบบเติมเงิน | By KururagiSeimei");

            await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"✅ กรุณาโอนเงิน {topupAmount} บาท")
                .AddEmbed(embed));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Topup error: {ex}");
            await interaction.EditOriginalResponseAsync(
                new DiscordWebhookBuilder()
                    .WithContent("❌ เกิดข้อผิดพลาดในการดำเนินการ"));
        }
    }

    public static async Task ShowTopupLink(DiscordInteraction interaction)
    {
        try
        {
            // Create a button that links to the external website
            var linkButton = new DiscordLinkButtonComponent(
                "http://45.154.24.114/vslip/index.php",
                "ไปยังหน้าเติมเงิน",
                false,
                new DiscordComponentEmoji("💰"));

            var embed = new DiscordEmbedBuilder()
                .WithTitle("💰 การเติมเงิน")
                .WithDescription("กรุณาคลิกปุ่มด้านล่างเพื่อไปยังหน้าเติมเงิน")
                .WithColor(DiscordColor.Gold)
                .WithFooter("ระบบเติมเงิน | By KururagiSeimei");

            await interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed)
                    .AddComponents(linkButton)
                    .AsEphemeral(true));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating topup link: {ex}");
            await interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("❌ เกิดข้อผิดพลาดในการเปิดลิงก์เติมเงิน")
                    .AsEphemeral(true));
        }
    }
}