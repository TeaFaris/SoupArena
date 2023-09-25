using C3.SmartEnums;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SoupArena.DataBase;
using SoupArena.ImageGenerators;
using SoupArena.Models.Player.DB;
using SoupArena.Models.SmartEnums;
using SoupArena.Models.SmartEnums.Classes;
using System.Drawing;
using System.Runtime.Versioning;
using Color = Discord.Color;

namespace SoupArena.Discord.Modules.Interactions
{
    public class InventoryInteractions : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly static MessageComponent InventoryButtons = new ComponentBuilder()
                                            .WithButton("Назад", nameof(MainInteractions.MainMenu), ButtonStyle.Secondary)
                                            .WithButton("Экипировка", nameof(InventoryChooseEquipment), ButtonStyle.Success)
                                            .Build();

        [SupportedOSPlatform("windows")]
        [ComponentInteraction(nameof(InventoryEnter))]
        public async Task InventoryEnter()
        {
			await DeferAsync(true);

			using var DB = new DBContext();

            var SessionPlayer = DiscordBot.Instance.GetSessionPlayer(Context.User.Id)!;
            var Player = (await DB
                .Players
                .Include(x => x.Consumables)
                .Include(x => x.Consumables.Items)
                .FirstOrDefaultAsync(x => x.DiscordID == Context.User.Id))!;

            var Items = Player
                .Consumables
                .Items
                .OrderBy(x => x.ConsumableID);

            var ImageTask = InventoryGridImageGenerator.FillGridAndGetImageStreamAsync(Paths.InventoryPath, Items.Select(x => x.Amount.ToString()).ToArray(),
                                                                    new Point(90, 130), new Point(140, 137), 5);

            string EmbedImage = $"{Guid.NewGuid()}.png";
            EmbedBuilder EmbedBuilder = new EmbedBuilder()
                                .WithTitle("Soup Arena | Инвентарь")
                                .WithDescription($"""
                                                  Викинг: {Context.User.Mention}

                                                  Побед: {Player.Wins}
                                                  Поражений: {Player.Loses}

                                                  Винрейт: {((Player.Wins + Player.Loses) == 0 ? 0 : string.Format("{0:0.00}", (double)Player.Wins / (Player.Wins + Player.Loses) * 100))}%

                                                  {Paths.SilverEmoji} Монеты: {Player.Silver}
                                                  """)
                                .WithImageUrl($"attachment://{EmbedImage}")
                                .WithThumbnailUrl(Paths.ChestIcon)
                                .WithColor(Color.DarkGreen);

            await Context.Interaction.FollowupWithFileAsync(await ImageTask, EmbedImage, ephemeral: true, components: InventoryButtons, embed: EmbedBuilder.Build());
        }

        private readonly static SelectMenuBuilder EquipmentSelectMenu = new SelectMenuBuilder()
                            .WithCustomId(nameof(InventoryChooseEquipment))
                            .WithPlaceholder("Выберите экиперовку:")
                            .WithMinValues(1)
                            .WithMaxValues(1);
        [ComponentInteraction(nameof(InventoryChooseEquipment))]
        public async Task InventoryChooseEquipment()
        {
            await DeferAsync(true);

            using var DB = new DBContext();

            var Player = (await DB
                .Players
                .Include(x => x.ConsumablesEquiped)
                .Include(x => x.ConsumablesEquiped.Items)
                .FirstOrDefaultAsync(x => x.DiscordID == Context.User.Id))!;

            var RawData = Context.Interaction.Data.GetType().GetProperty("Values")!.GetValue(Context.Interaction.Data);

            Optional<string[]> Data = ((Optional<string[]>)RawData!);
            if (!Data.IsSpecified || !byte.TryParse(Data.Value[0], out byte SelectedEquipmentID))
                SelectedEquipmentID = Player.Equipment?.ID ?? byte.MaxValue;

            if (SelectedEquipmentID != byte.MaxValue)
                Player.EquipmentID = SelectedEquipmentID;

            Equipment[] AllEquipment = SmartEnumExtentions.GetValues<Equipment, byte>();
            EquipmentSelectMenu.Options.Clear();

            for (int i = 0; i < AllEquipment.Length; i++)
            {
                var CurrentEquip = AllEquipment[i];
                EquipmentSelectMenu.AddOption(new SelectMenuOptionBuilder()
                                    .WithValue(i.ToString())
                                    .WithLabel($"{i + 1}. {CurrentEquip.Name}")
                                    .WithDefault(CurrentEquip.ID == Player.EquipmentID));
            }

            EmbedBuilder EmbedBuilder = new EmbedBuilder()
                               .WithTitle("Soup Arena | Экипировка")
                               .WithDescription($"""
                                                  Викинг: {Context.User.Mention}

                                                  Класс: {Player.Class?.Name ?? "Выберите класс."}
                                                  Надето: {Player.Equipment?.Name ?? "Ничего."}

                                                  Расходники:
                                                  {Player.ConsumablesEquiped}
                                                  """)
                               .WithThumbnailUrl(Player.Equipment?.ImageURL ?? Paths.EquipmentDefault)
                               .WithImageUrl(Paths.EquipmentInventory)
                               .WithColor(Color.DarkGreen);

            ComponentBuilder ComponentBuilder = new ComponentBuilder()
                                            .WithSelectMenu(EquipmentSelectMenu)
                                            .WithButton("Назад", nameof(InventoryEnter), ButtonStyle.Secondary)
                                            .WithButton("Выбрать класс", nameof(InventoryChooseClass), ButtonStyle.Primary)
                                            .WithButton("Расходники", nameof(InventoryChooseConsumables), ButtonStyle.Success);

            await Context.Interaction.FollowupAsync(ephemeral: true, components: ComponentBuilder.Build(), embed: EmbedBuilder.Build());

            DB.Update(Player);
            await DB.SaveChangesAsync();
        }

        private readonly static SelectMenuBuilder ClassSelectMenu = new SelectMenuBuilder()
                            .WithCustomId(nameof(InventoryChooseClass))
                            .WithPlaceholder("Выберите класс:")
                            .WithMinValues(1)
                            .WithMaxValues(1);

        [ComponentInteraction(nameof(InventoryChooseClass))]
        public async Task InventoryChooseClass()
        {
            await DeferAsync(true);

            using var DB = new DBContext();

            DBPlayer Player = (await DB
                .Players
                .FindAsync(Context.User.Id))!;

            var RawData = Context
                .Interaction
                .Data
                .GetType()
                .GetProperty("Values")!.GetValue(Context.Interaction.Data);

            Optional<string[]> Data = ((Optional<string[]>)RawData!);
            if (!Data.IsSpecified || !byte.TryParse(Data.Value[0], out byte SelectedClass))
                SelectedClass = Player.Class?.ID ?? byte.MinValue;

            Player.ClassID = SelectedClass;
            Class Class = Player.Class!.Value;

            ClassSelectMenu.Options.Clear();
            var AllClasses = SmartEnumExtentions.GetValues<Class, byte>();
            for (int i = 0; i < AllClasses.Length; i++)
            {
                var CurrentClass = AllClasses[i];
                ClassSelectMenu.AddOption(new SelectMenuOptionBuilder()
                                    .WithValue(i.ToString())
                                    .WithLabel($"{i + 1}. {CurrentClass.Name}")
                                    .WithDefault(CurrentClass.ID == Player.ClassID));
            }

            EmbedBuilder EmbedBuilder = new EmbedBuilder()
                                .WithTitle("Soup Arena | Класс")
                                .WithDescription($"""
                                                  Викинг: {Context.User.Mention}
                                                  У каждого класса разные способности.
                                                  Выбери свой.

                                                  {Class.Name}
                                                  1. Способность: {Class.FirstAbility.Name} - {Class.FirstAbility.Description}
                                                  Кулдаун: {Class.FirstAbility.Cooldown} ход.

                                                  Шанс попадания:
                                                  {Class.FirstAbility.GetChancesString()}

                                                  2. Способность: {Class.SecondAbility.Name} - {Class.SecondAbility.Description}
                                                  Кулдаун: {Class.SecondAbility.Cooldown} ход.

                                                  Шанс попадания:
                                                  {Class.SecondAbility.GetChancesString()}
                                                  """)
                                .WithThumbnailUrl(Paths.ClassIcon)
                                .WithColor(Color.DarkGreen);

            var ComponentBuilder = new ComponentBuilder()
                                            .WithSelectMenu(ClassSelectMenu)
                                            .WithButton("Назад", nameof(InventoryEnter), ButtonStyle.Secondary)
                                            .WithButton("Хейм", nameof(MainInteractions.MainMenu), ButtonStyle.Secondary)
                                            .WithButton("Экипировка", nameof(InventoryChooseEquipment), ButtonStyle.Success);

            await Context.Interaction.FollowupAsync(ephemeral: true, components: ComponentBuilder.Build(), embed: EmbedBuilder.Build());

            DB.Update(Player);
            await DB.SaveChangesAsync();
        }

        private readonly static SelectMenuBuilder ChooseConsumablesMenu = new SelectMenuBuilder()
                            .WithCustomId(nameof(InventoryChooseConsumablesAmount))
                            .WithPlaceholder("Выберите расходник:")
                            .WithMinValues(1)
                            .WithMaxValues(1);
        [SupportedOSPlatform("windows")]
        [ComponentInteraction(nameof(InventoryChooseConsumables))]
        public async Task InventoryChooseConsumables()
        {
            using var DB = new DBContext();

            var SessionPlayer = DiscordBot.Instance.GetSessionPlayer(Context.User.Id)!;

            var Player = (await DB
                .Players
                .Include(x => x.Consumables)
                .Include(x => x.Consumables.Items)
                .Include(x => x.ConsumablesEquiped)
                .Include(x => x.ConsumablesEquiped.Items)
                .FirstOrDefaultAsync(x => x.DiscordID == Context.User.Id))!;

            var UnfilteredItems = Player
                .Consumables
                .Items
                .OrderBy(x => x.ConsumableID)
                .ToArray();
            var Items = UnfilteredItems
                .Where(x => x.Amount > 0)
                .ToArray();

            ComponentBuilder ChooseConsumablesButtons = new ComponentBuilder()
                                .WithButton("Назад", nameof(InventoryEnter), ButtonStyle.Secondary)
                                .WithButton("Экипировка", nameof(InventoryChooseEquipment), ButtonStyle.Success);

            if (Items.Length == 0)
            {
                await RespondAsync("У Вас нет расходников!", ephemeral: true, components: ChooseConsumablesButtons.Build());
                return;
            }

            var ImageTask = InventoryGridImageGenerator.FillGridAndGetImageStreamAsync(Paths.InventoryPath, UnfilteredItems.Select(x => x.Amount.ToString()).ToArray(),
                                                                    new Point(90, 130), new Point(140, 137), 5);

            ChooseConsumablesMenu.Options.Clear();
            for (int i = 0; i < Items.Length; i++)
            {
                var CurrentItem = Items[i];
                ChooseConsumablesMenu.AddOption(new SelectMenuOptionBuilder()
                                    .WithValue(CurrentItem.ConsumableID.ToString())
                                    .WithDescription($"{CurrentItem.Amount} шт.")
                                    .WithLabel($"{i + 1}. {CurrentItem.Consumable.Name}"));
            }
            ChooseConsumablesButtons.WithSelectMenu(ChooseConsumablesMenu);

            var EmbedImage = $"{Guid.NewGuid()}.png";
            var EmbedBuilder = new EmbedBuilder()
                               .WithTitle("Soup Arena | Расходники")
                               .WithDescription($"""
                                                 {Context.User.Mention} На арену можно взять 5 любых расходников, не более 10 штук каждого вида.

                                                 Расходники:
                                                 {Player.ConsumablesEquiped}
                                                 """)
                               .WithThumbnailUrl(Paths.ConsumablesIcon)
                               .WithImageUrl($"attachment://{EmbedImage}")
                               .WithColor(Color.DarkGreen);

            await DeferAsync(true);

            await Context.Interaction.FollowupWithFileAsync(await ImageTask, EmbedImage, ephemeral: true, components: ChooseConsumablesButtons.Build(), embed: EmbedBuilder.Build());
        }

        public class AskAmountModal : IModal
        {
            public string Title { get; } = "Количество для взятия на арену";
            [InputLabel("Укажите количество от 1 до 10")]
            [ModalTextInput(nameof(Amount), TextInputStyle.Short, "1-10", 1, 2)]
            [RequiredInput]
            public byte Amount { get; set; }
        }
        [ComponentInteraction(nameof(InventoryChooseConsumablesAmount))]
        public async Task InventoryChooseConsumablesAmount()
        {
            using var DB = new DBContext();

            var SessionPlayer = DiscordBot.Instance.GetSessionPlayer(Context.User.Id)!;

            var Player = (await DB
                .Players
                .Include(x => x.Consumables)
                .Include(x => x.Consumables.Items)
                .FirstOrDefaultAsync(x => x.DiscordID == Context.User.Id))!;

            var RawData = Context.Interaction.Data.GetType().GetProperty("Values")!.GetValue(Context.Interaction.Data);
            Optional<string[]> Data = ((Optional<string[]>)RawData!);
            if (!Data.IsSpecified || !short.TryParse(Data.Value[0], out short SelectedConsumableID))
                SelectedConsumableID = 0;

            DBInventoryConsumable Item = Player
                .Consumables
                .Items
                .Find(x => x.ConsumableID == SelectedConsumableID)!;
            SessionPlayer.ChoosedConsumable = Item.Consumable;

            uint MaxAmount = Item.Amount > 10 ? 10 : Item.Amount;

            await RespondWithModalAsync<AskAmountModal>(nameof(ChooseConsumableModal));
        }

        private readonly static MessageComponent ChooseConsumableButtons = new ComponentBuilder()
                        .WithButton("Назад", nameof(InventoryChooseConsumables), ButtonStyle.Secondary)
                        .WithButton("Хейм", nameof(MainInteractions.MainMenu))
                        .Build();
        [ModalInteraction(nameof(ChooseConsumableModal))]
        public async Task ChooseConsumableModal(AskAmountModal Modal)
        {
            var SessionPlayer = DiscordBot.Instance.GetSessionPlayer(Context.User.Id)!;

            if (SessionPlayer.ChoosedConsumable is null)
            {
                await RespondAsync("Произошла непредвиденная ошибка, попробуйте ещё раз.", ephemeral: true, components: ChooseConsumableButtons);
                return;
            }

            using var DB = new DBContext();

            var Player = (await DB
                .Players
                .Include(x => x.Consumables)
                .Include(x => x.Consumables.Items)
                .Include(x => x.ConsumablesEquiped)
                .Include(x => x.ConsumablesEquiped.Items)
                .FirstOrDefaultAsync(x => x.DiscordID == Context.User.Id))!;

            DBInventoryConsumable Item = Player
                .Consumables
                .Items
                .Find(x => x.ConsumableID == SessionPlayer.ChoosedConsumable!.Value.ID)!;

            if (Item!.Amount < Modal.Amount)
            {
                await RespondAsync("У Вас недостаточно расходников!", ephemeral: true, components: ChooseConsumableButtons);
                return;
            }
            if (Modal.Amount > 10)
            {
                await RespondAsync("Вы не можете взять больше 10 расходников одного типа на арену!", ephemeral: true, components: ChooseConsumableButtons);
                return;
            }

            Player
                .ConsumablesEquiped
                .Items
                .Find(x => x.ConsumableID == SessionPlayer.ChoosedConsumable!.Value.ID)!
                .Amount = Modal.Amount;

            var EmbedBuilder = new EmbedBuilder()
                            .WithTitle("Soup Arena | Расходники")
                            .WithDescription($"Вы взяли на арену \"{Item.Consumable.Name}\" в количестве {Modal.Amount}.")
                            .WithColor(Color.DarkGreen);

            await RespondAsync(ephemeral: true, components: ChooseConsumableButtons, embed: EmbedBuilder.Build());

            DB.Update(Player);
            await DB.SaveChangesAsync();
        }
    }
}
