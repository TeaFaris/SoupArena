using C3.SmartEnums;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SoupArena.DataBase;
using SoupArena.Models.Arena;
using SoupArena.Models.Player;
using SoupArena.Models.Player.DB;
using SoupArena.Models.SmartEnums;

namespace SoupArena.Discord.Modules.Interactions
{
    public class MerchantInteractions : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction(nameof(MerchantEnter))]
        public async Task MerchantEnter()
        {
            await DeferAsync(true);

            DBPlayer Player;
            using (var DB = new DBContext())
            {
                Player = (await DB
                    .Players
                    .FindAsync(Context.User.Id))!;
            }

            Merchant Merchant = Merchant.Basic; // TODO: Merchant based on player's arena.

            EmbedBuilder EmbedBuilder = new EmbedBuilder()
                                .WithThumbnailUrl(Merchant.ImageURL)
                                .WithColor(Color.DarkGreen)
                                .WithTitle($"Soup Arena | {Merchant.Name}")
                                .WithImageUrl(Merchant.InventoryImageURL)
                                .WithDescription($"""
                                                  Добро пожаловать {Context.User.Mention}.
                                                  {Merchant.Dialogue}
                                                  

                                                  {Paths.SilverEmoji} Монеты: {Player.Silver}
                                                  """);

            SelectMenuBuilder Menu = new SelectMenuBuilder()
                            .WithCustomId(nameof(MerchantShowProduct))
                            .WithPlaceholder("Выберите товар к покупке!")
                            .WithMinValues(1)
                            .WithMaxValues(1);

            for (int i = 0; i < Merchant.Products.Count; i++)
            {
                var Product = Merchant.Products.ElementAt(i);
                Menu.AddOption(new SelectMenuOptionBuilder()
                                    .WithValue(Product.Key.ID.ToString())
                                    .WithLabel($"{i + 1}. {Product.Key.Name}")
                                    .WithDescription($"{Product.Value} сер."));
            }

            var ComponentBuilder = new ComponentBuilder()
                                            .WithSelectMenu(Menu)
                                            .WithButton("Назад", nameof(MainInteractions.MainMenu), ButtonStyle.Secondary);

            await Context.Interaction.FollowupAsync(ephemeral: true, components: ComponentBuilder.Build(), embed: EmbedBuilder.Build());
        }

        [ComponentInteraction(nameof(MerchantShowProduct))]
        public async Task MerchantShowProduct()
        {
            await DeferAsync(true);

            Merchant Merchant = Merchant.Basic; // TODO: Merchant based on player's arena.
            SessionPlayer SessionPlayer = DiscordBot.Instance.GetSessionPlayer(Context.User.Id)!;

            Optional<string[]> Data = (Context
                .Interaction
                .Data
                .GetType()
                .GetProperty("Values")!
                .GetValue(Context.Interaction.Data)
                as Optional<string[]>?)!
                .Value;

            Consumable Consumable = SmartEnumExtentions.GetByID<Consumable, short>(short.Parse(Data.Value[0]))!.Value;
            SessionPlayer.ObservableConsumable = Consumable;

            var EmbedBuilder = new EmbedBuilder()
                                .WithTitle($"Soup Arena | {Consumable.Name}")
                                .WithDescription($"""
                                                  Описание: {Consumable.Description}
                                                  Действует: {(Consumable.UseDuration == 0 ? "Моментально" : $"{Consumable.UseDuration} ход.")}
                                                  Кулдаун: {(Consumable.Cooldown == 0 ? "Моментально" : $"{Consumable.Cooldown} ход.")}
                                                  {Paths.SilverEmoji} {Merchant.Products[Consumable]} за штуку
                                                  """)
                                .WithThumbnailUrl(Consumable.ImageURL)
                                .WithColor(Color.DarkGreen);
            var ComponentBuilder = new ComponentBuilder()
                                            .WithButton("Назад", nameof(MerchantEnter), ButtonStyle.Secondary)
                                            .WithButton("Хейм", nameof(MainInteractions.MainMenu))
                                            .WithButton("Купить", nameof(MerchantAskAmount), ButtonStyle.Success);
            await Context.Interaction.FollowupAsync(ephemeral: true, components: ComponentBuilder.Build(), embed: EmbedBuilder.Build());
        }

        public class AskAmountModal : IModal
        {
            public string Title { get; } = "Количество для покупки";
            [InputLabel("Укажите количество от 1 до 99")]
            [ModalTextInput(nameof(Amount), TextInputStyle.Short, "1-99", 1, 2)]
            [RequiredInput]
            public byte Amount { get; set; }
        }
        [ComponentInteraction(nameof(MerchantAskAmount))]
        public async Task MerchantAskAmount()
        {
            await RespondWithModalAsync<AskAmountModal>(nameof(BuyProductModal));
        }

        private readonly static MessageComponent BuyProductButtons = new ComponentBuilder()
                                        .WithButton("Назад", nameof(MerchantEnter), ButtonStyle.Secondary)
                                        .WithButton("Хейм", nameof(MainInteractions.MainMenu))
                                        .Build();
        [ModalInteraction(nameof(BuyProductModal))]
        public async Task BuyProductModal(AskAmountModal Modal)
        {
            SessionPlayer SessionPlayer = DiscordBot.Instance.GetSessionPlayer(Context.User.Id)!;

            if (SessionPlayer.ObservableConsumable is null)
            {
                await RespondAsync("Произошла непредвиденная ошибка, попробуйте ещё раз.", ephemeral: true, components: BuyProductButtons);
                return;
            }

            using var DB = new DBContext();

            DBPlayer Player = (await DB
                        .Players
                        .Include(x => x.Consumables)
                        .Include(x => x.Consumables.Items)
                        .FirstOrDefaultAsync(x => x.DiscordID == Context.User.Id))!;

            Merchant Merchant = Merchant.Basic; // TODO: Merchant based on player's arena.

            Consumable Product = SessionPlayer.ObservableConsumable!.Value;

            ulong Price = Modal.Amount * Merchant.Products[Product];

            if (Player.Silver < Price)
            {
                await RespondAsync("У Вас недостаточно средств!", ephemeral: true, components: BuyProductButtons);
                return;
            }

            await DeferAsync(true);

            EmbedBuilder EmbedBuilder = new EmbedBuilder()
                            .WithTitle($"Soup Arena | {Product.Name}")
                            .WithDescription("Ваш товар доставлен в инвентарь.")
                            .WithThumbnailUrl(Product.ImageURL)
                            .WithFooter(new EmbedFooterBuilder()
                                                .WithIconUrl(Paths.SilverIcon)
                                                .WithText($"Вы купили {Modal.Amount} штук за {Price}"))
                            .WithColor(Color.DarkGreen);
            Player.Silver -= Price;

            Player
                .Consumables
                .Items
                .Find(x => x.Consumable == Product)!.Amount += Modal.Amount;

            await Context.Interaction.FollowupAsync(ephemeral: true, components: BuyProductButtons, embed: EmbedBuilder.Build());

            DB.Players.Update(Player);
            await DB.SaveChangesAsync();
        }
    }
}
