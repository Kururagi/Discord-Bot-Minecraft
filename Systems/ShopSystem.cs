using System.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;
using Rcon;
using static Program;

public static class ShopSystem
{
    static RconClient rcon;

    public static async Task InitializeRcon()
    {
        try
        {
            rcon = new RconClient();
            await rcon.ConnectAsync(Config.MinecraftServerIP, Config.MinecraftServerPort);
            await rcon.AuthenticateAsync(Config.RconPassword);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing RCON: {ex.Message}");
            rcon = null;
        }
    }

    private static Dictionary<ulong, Dictionary<string, string>> _shopItemCache = new();

    public static async Task ShowShopCategories(DiscordInteraction interaction)
    {
        try
        {
            // ตอบสนองทันทีเพื่อป้องกัน timeout
            await interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral(true));

            using var conn = new MySqlConnection(Config.MySqlConnectionString);
            await conn.OpenAsync();

            // ดึงข้อมูลหมวดหมู่ (จำกัดเพียง 25 รายการแรก)
            var cmd = new MySqlCommand(
                "SELECT id, name FROM shop_categories ORDER BY id LIMIT 25",
                conn);

            using var reader = await cmd.ExecuteReaderAsync();

            var options = new List<DiscordSelectComponentOption>();
            while (await reader.ReadAsync())
            {
                var categoryName = reader.GetString("name");
                var categoryId = reader.GetInt32("id").ToString();

                // ตรวจสอบความยาวชื่อหมวดหมู่
                if (categoryName.Length > 100)
                {
                    categoryName = categoryName.Substring(0, 97) + "...";
                }

                options.Add(new DiscordSelectComponentOption(
                    categoryName,
                    categoryId
                ));
            }

            // ถ้าไม่มีหมวดหมู่
            if (options.Count == 0)
            {
                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder()
                        .WithContent("⚠️ ยังไม่มีหมวดหมู่สินค้าในระบบ"));
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle("🛒 ร้านค้า")
                .WithDescription(options.Count < 25 ?
                    "เลือกหมวดหมู่จากเมนูด้านล่าง" :
                    "⚠️ แสดงเพียง 25 หมวดหมู่แรก")
                .WithColor(DiscordColor.Gold);

            var dropdown = new DiscordSelectComponent(
                "shop_category_select",
                "เลือกหมวดหมู่...",
                options,
                disabled: options.Count == 0);

            await interaction.EditOriginalResponseAsync(
                new DiscordWebhookBuilder()
                    .AddEmbed(embed)
                    .AddComponents(dropdown));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ShowShopCategories: {ex}");

            try
            {
                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder()
                        .WithContent("❌ เกิดข้อผิดพลาดในการแสดงหมวดหมู่"));
            }
            catch
            {
                // ถ้าไม่สามารถแก้ไข response ได้ ส่งข้อความใหม่
                await interaction.Channel.SendMessageAsync(
                    "❌ เกิดข้อผิดพลาดในการแสดงหมวดหมู่");
            }
        }
    }

    public static string TrimString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input.Length <= maxLength ?
            input :
            input.Substring(0, Math.Min(input.Length, maxLength - 3)) + "...";
    }

    public static async Task ShowShopItems(DiscordInteraction interaction, string categoryId)
    {
        try
        {
            await interaction.DeferAsync(true);

            using var conn = new MySqlConnection(Config.MySqlConnectionString);
            await conn.OpenAsync();

            // ดึงข้อมูลหมวดหมู่
            var categoryCmd = new MySqlCommand(
                "SELECT name FROM shop_categories WHERE id = @categoryId",
                conn);
            categoryCmd.Parameters.AddWithValue("@categoryId", categoryId);
            var categoryName = (await categoryCmd.ExecuteScalarAsync())?.ToString() ?? "Unknown Category";

            // ดึงข้อมูลสินค้า
            var items = new List<ShopItem>();
            var itemsCmd = new MySqlCommand(
                "SELECT id, name, image_url, price, currency_type FROM shop_items " +
                "WHERE category_id = @categoryId ORDER BY id",
                conn);
            itemsCmd.Parameters.AddWithValue("@categoryId", categoryId);

            using (var reader = await itemsCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    items.Add(new ShopItem
                    {
                        Id = reader.GetInt32("id"),
                        Name = reader.GetString("name"),
                        ImageUrl = reader.IsDBNull("image_url") ?
                            "https://i.imgur.com/default.png" :
                            reader.GetString("image_url"),
                        Price = reader.GetInt32("price"),
                        CurrencyType = reader.GetString("currency_type")
                    });
                }
            }

            if (items.Count == 0)
            {
                await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"⚠️ ไม่พบสินค้าในหมวดหมู่ '{categoryName}'"));
                return;
            }

            // สร้าง WebhookBuilder สำหรับข้อความแรก
            var mainBuilder = new DiscordWebhookBuilder();

            // สร้าง Embed หัวข้อ
            var headerEmbed = new DiscordEmbedBuilder()
                .WithTitle($"🛒 ร้านค้า - {categoryName}")
                .WithColor(DiscordColor.Gold);

            mainBuilder.AddEmbed(headerEmbed);

            // ส่งข้อความหลัก (ว่างเปล่าเพื่อแสดงหัวข้อ)
            await interaction.EditOriginalResponseAsync(mainBuilder);

            // ส่งสินค้าแต่ละรายการแยกกัน
            foreach (var item in items)
            {
                var currencyIcon = GetCurrencyIcon(item.CurrencyType);

                // สร้าง Embed สำหรับสินค้า - ใช้ Thumbnail แทน Image
                var itemEmbed = new DiscordEmbedBuilder()
                    .WithTitle(item.Name)
                    .WithThumbnail(item.ImageUrl) // ใช้ Thumbnail แทน Image
                    .AddField("ราคา", $"{currencyIcon} {item.Price}", true)
                    .WithColor(DiscordColor.Red);

                // สร้างปุ่มซื้อ
                var buyButton = new DiscordButtonComponent(
                    ButtonStyle.Success,
                    $"buy_item_{item.Id}",
                    $"ซื้อ {item.Price}",
                    emoji: new DiscordComponentEmoji(currencyIcon == "💵" ? "💵" : "🪙"));

                // ส่งข้อความแยกสำหรับแต่ละสินค้า
                await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                .AddEmbed(itemEmbed)
                .AddComponents(buyButton)
                .AsEphemeral(true));

                await Task.Delay(500);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ShowShopItems: {ex}");
            await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent("❌ เกิดข้อผิดพลาดในการแสดงรายการสินค้า"));
        }
    }

    public static async Task HandleBuyItem(DiscordInteraction interaction, string itemId)
    {
        try
        {
            var userId = interaction.User.Id;
            await interaction.DeferAsync(true);

            // Initialize RCON if not already done
            if (rcon == null)
            {
                await InitializeRcon();
                if (rcon == null)
                {
                    await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("❌ ไม่สามารถเชื่อมต่อกับเซิร์ฟเวอร์ได้"));
                    return;
                }
            }

            using var conn = new MySqlConnection(Config.MySqlConnectionString);
            await conn.OpenAsync();

            // ดึงข้อมูลผู้ใช้
            var username = await DatabaseHelper.GetMinecraftUsername(interaction.User.Id);
            if (string.IsNullOrEmpty(username))
            {
                await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("❌ คุณยังไม่ได้ Verify บัญชี Minecraft"));
                return;
            }
            bool isOnline = await MinecraftCommands.IsPlayerOnline(username);

            // ตรวจสอบสถานะออนไลน์
            if (!isOnline)
            {
                var retryButton = new DiscordButtonComponent(
                    ButtonStyle.Primary,
                    $"retry_online_check_{DateTime.Now.Ticks}",
                    "ตรวจสอบอีกครั้ง");

                await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"⚠️ **คุณต้องออนไลน์ในเกมเพื่อซื้อสินค้า**\n" +
                                $"ชื่อในเกม: {username}\n" +
                                $"สถานะออนไลน์: {(isOnline ? "ออนไลน์" : "ออฟไลน์")}\n" +
                                "โปรดตรวจสอบว่า:\n" +
                                "1. คุณล็อกอินเกมแล้ว\n" +
                                "2. คุณอยู่บนเซิร์ฟเวอร์ที่ถูกต้อง")
                    .AddComponents(retryButton));
                return;
            }

            // ดึงข้อมูลสินค้า
            var itemCmd = new MySqlCommand(
            "SELECT name, price, currency_type, command FROM shop_items WHERE id = @itemId",
            conn);
            itemCmd.Parameters.AddWithValue("@itemId", itemId);

            using var reader = await itemCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("❌ ไม่พบสินค้านี้"));
                return;
            }

            var itemName = reader.GetString("name");
            var price = reader.GetInt32("price");
            var currencyType = reader.GetString("currency_type");
            var commands = reader.GetString("command");

            // ตรวจสอบเงิน
            bool canAfford = currencyType == "cash"
                ? await DatabaseHelper.GetUserCash(interaction.User.Id) >= price
                : await DatabaseHelper.GetUserPoints(interaction.User.Id) >= price;

            if (!canAfford)
            {
                string currencyName = currencyType == "cash" ? "Cash" : "Point";
                await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"❌ คุณมี {currencyName} ไม่เพียงพอ (ต้องการ {price})"));
                return;
            }

            // หักเงิน
            if (currencyType == "cash")
            {
                await DatabaseHelper.DeductUserCash(interaction.User.Id, price);
            }
            else
            {
                await DatabaseHelper.DeductUserPoints(interaction.User.Id, price);
            }

            // แยกและส่งคำสั่ง Minecraft
            var commandList = commands.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(cmd => cmd.Trim())
                                     .Where(cmd => !string.IsNullOrWhiteSpace(cmd));

            var results = new List<string>();
            foreach (var cmd in commandList)
            {
                try
                {
                    var formattedCommand = cmd.Replace("{username}", username);
                    await rcon.SendCommandAsync(formattedCommand);
                    results.Add($"✅ {formattedCommand}");

                    // รอสักครู่ระหว่างคำสั่งเพื่อป้องกันการ Flood
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    results.Add($"❌ {cmd} (Error: {ex.Message})");
                }
            }

            // สร้าง Embed ยืนยัน
            var embed = new DiscordEmbedBuilder()
                .WithTitle("✅ ซื้อสินค้าสำเร็จ")
                .WithDescription($"คุณได้รับ **{itemName}** แล้ว!")
                .WithColor(DiscordColor.Green)
                .AddField("ใช้ไป", $"{GetCurrencyIcon(currencyType)} {price}", true)
                .AddField("คงเหลือ", $"{GetCurrencyIcon(currencyType)} {await DatabaseHelper.GetUserBalance(interaction.User.Id, currencyType)}", true)
                .AddField("คำสั่งที่ดำเนินการ", string.Join("\n", results.Take(5))) // แสดงแค่ 5 คำสั่งแรก
                .WithThumbnail("https://cdn3.emoji.gg/emojis/6785-checkmark-green.png");

            if (results.Count > 5)
            {
                embed.AddField("หมายเหตุ", $"และอีก {results.Count - 5} คำสั่ง...");
            }

            await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(embed));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
            await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                .WithContent("❌ เกิดข้อผิดพลาดในการซื้อสินค้า"));
        }
    }

    public static string GetCurrencyIcon(string currencyType)
    {
        return currencyType == "cash" ? "💵" : "🪙";
    }

    public static async Task ShowAddShopCategoryModal(DiscordInteraction interaction)
    {
        var modal = new DiscordInteractionResponseBuilder()
            .WithTitle("เพิ่มหมวดหมู่ร้านค้า")
            .WithCustomId("add_shop_category_modal")
            .AddComponents(new TextInputComponent(
                label: "ชื่อหมวดหมู่",
                customId: "shop_category_name",
                placeholder: "กรอกชื่อหมวดหมู่",
                required: true,
                style: TextInputStyle.Short));

        await interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
    }

    public static void CleanupRcon()
    {
        try
        {
            rcon?.Dispose();
            rcon = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up RCON: {ex.Message}");
        }
    }
}