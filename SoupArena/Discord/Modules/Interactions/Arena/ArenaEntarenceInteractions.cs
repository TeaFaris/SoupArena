using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SoupArena.DataBase;
using SoupArena.ImageGenerators;
using SoupArena.Models.SmartEnums;
using System.Drawing;
using Color = Discord.Color;

namespace SoupArena.Discord.Modules.Interactions.Arena
{
    public class ArenaEntarenceInteractions : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction(nameof(ArenaEnter))]
        public async Task ArenaEnter()
        {
            using var DB = new DBContext();

            var Player = (await DB
                .Players
                .Include(x => x.ConsumablesEquiped)
                .Include(x => x.ConsumablesEquiped.Items)
                .FirstOrDefaultAsync(x => x.DiscordID == Context.User.Id))!;

            var Items = Player
                .ConsumablesEquiped
                .Items
                .OrderBy(x => x.ConsumableID);

            string EmbedImage = $"{Guid.NewGuid()}.png";
            string Description;
            string ImagePath;
            ComponentBuilder ComponentBuilder;

            if (Player.Equipment is Equipment Equipment)
            {
                Description = $"""
                               {Context.User.Mention} Помни! Если ты проиграешь потеряешь все расходники, которые взял с собой.

                               Победа на арене: 200 {Paths.SilverEmoji}
                               Выбранный класс: {Player.Class?.Name ?? "Не выбран"}

                               Вы экипированы:
                               """;
                ImagePath = Equipment.ArenaEnteranceImagePath;
                ComponentBuilder = new ComponentBuilder()
                                            .WithButton("Назад", nameof(MainInteractions.MainMenu), ButtonStyle.Secondary)
                                            .WithButton("Поиск боя", nameof(ArenaSearchBattle.ArenaSearchStart), ButtonStyle.Primary, disabled: Player.Class is null || Player.Equipment is null)
                                            .WithButton("Предложить бой", nameof(ArenaSendOfferInfo), ButtonStyle.Primary, disabled: Player.Class is null || Player.Equipment is null);
            }
            else
            {
                Description = $"""
                               {Context.User.Mention} Эй, ты куда без брони?!
                               Отправляйся в инвентарь и экипируйся.

                               Победа на арене: 200 {Paths.SilverEmoji}
                               Поражение: Потеря всех расходников, которые взял с собой.

                               Вы не экипированы:
                               """;
                ImagePath = Paths.ArenaEnteranceWithoutEquipmentPath;
                ComponentBuilder = new ComponentBuilder()
                                            .WithButton("Назад", nameof(MainInteractions.MainMenu), ButtonStyle.Secondary)
                                            .WithButton("Инвентарь", nameof(InventoryInteractions.InventoryEnter), ButtonStyle.Primary);
            }

            await DeferAsync(true);

            var Image = await InventoryGridImageGenerator.FillGridAndGetImageStreamAsync(ImagePath, Items.Select(x => x.Amount.ToString()).ToArray(), new Point(95, 670), new Point(137, 135), 5);
            var EmbedBuilder = new EmbedBuilder()
                                .WithTitle("Soup Arena | Вход на арену")
                                .WithDescription(Description)
                                .WithImageUrl($"attachment://{EmbedImage}")
                                .WithThumbnailUrl(Paths.ArenaEnteranceIcon)
                                .WithColor(Color.DarkGreen);

            await Context.Interaction.FollowupWithFileAsync(Image, EmbedImage, ephemeral: true, components: ComponentBuilder.Build(), embed: EmbedBuilder.Build());
        }

        private readonly static MessageComponent Buttons = new ComponentBuilder()
                                            .WithButton("Назад", nameof(ArenaEnter), ButtonStyle.Secondary)
                                            .Build();
        private readonly static Embed Embed = new EmbedBuilder()
                                .WithTitle("Soup Arena | Вход на арену")
                                .WithDescription("""
                                                 Введите команду с тегом участника, которому хотите предложить бой:
                                                 /предложить-бой @ник#000
                                                 """)
                                .WithThumbnailUrl(Paths.ArenaEnteranceIcon)
                                .WithColor(Color.DarkGreen)
                                .Build();
        [ComponentInteraction(nameof(ArenaSendOfferInfo))]
        public async Task ArenaSendOfferInfo()
        {
            await RespondAsync(ephemeral: true, components: Buttons, embed: Embed);
        }
    }
}
