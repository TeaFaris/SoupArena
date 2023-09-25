using Discord;
using Discord.Interactions;
using SoupArena.Discord.Modules.Interactions.Arena;
using SoupArena.Models.Battle.Offer;

namespace SoupArena.Discord.Modules.Commands
{
    public class ArenaOfferCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("предложить-бой", "Предложить кому-либо бой.")]
        public async Task OfferBattle(IUser MentionedUser)
        {
            var SessionPlayer = DiscordBot.Instance.GetSessionPlayer(Context.User.Id)!;
            var MentionedSessionPlayer = DiscordBot.Instance.GetSessionPlayer(MentionedUser.Id)!;

            var EmbedBuilder = new EmbedBuilder()
                                .WithTitle("Soup Arena | Предложить бой")
                                .WithThumbnailUrl(Paths.ArenaSearchMatchIcon)
                                .WithColor(Color.DarkGreen);

            if (SessionPlayer.CurrentBattle is not null)
            {
                await RespondAsync(text: "Вы находитесь в бою!", ephemeral: true);
                return;
            }

            var ComponentBuilder = new ComponentBuilder()
                                            .WithButton("Назад", nameof(ArenaEntarenceInteractions.ArenaEnter), ButtonStyle.Secondary);

            if (MentionedUser.Id == Context.User.Id)
            {
                EmbedBuilder
                        .WithDescription("""
                                         Вы не можете предложить бой самому себе!
                                         """);
                await RespondAsync(ephemeral: true, components: ComponentBuilder.Build(), embed: EmbedBuilder.Build());
                return;
            }

            if (MentionedUser is null || MentionedSessionPlayer is null)
            {
                EmbedBuilder
                        .WithDescription("""
                                         Такого викинга ещё нет на просторах Мидгарда!
                                         """);

                await RespondAsync(ephemeral: true, components: ComponentBuilder.Build(), embed: EmbedBuilder.Build());
                return;
            }

            if (MentionedSessionPlayer.BattleOffers.Any(x => x.Key.SecondPlayer.SessionPlayer == SessionPlayer || x.Key.FirstPlayer.SessionPlayer == SessionPlayer))
            {
                await RespondAsync("Вы уже отправили предложение сразиться этому игроку!", ephemeral: true);
                return;
            }

            EmbedBuilder
                    .WithDescription($"""
                                      Ваше предложение сразиться на арене успешно отправлено {MentionedUser.Mention}

                                      Ожидайте подтверждения, Вам придёт уведомление.
                                      """);

            OfferPlayer FirstPlayer = new OfferPlayer() { SessionPlayer = SessionPlayer };
            OfferPlayer SecondPlayer = new OfferPlayer() { SessionPlayer = MentionedSessionPlayer };

            FirstPlayer.Ready += ArenaSearchBattle.OfferPlayer_Ready;
            SecondPlayer.Ready += Player_Ready;

            BattleOffer BattleOffer = new BattleOffer(FirstPlayer, SecondPlayer);

            MentionedSessionPlayer
                .BattleOffers
                .Add(BattleOffer, DateTime.UtcNow + TimeSpan.FromMinutes(5));

            await RespondAsync(ephemeral: true, components: ComponentBuilder.Build(), embed: EmbedBuilder.Build());

            EmbedBuilder
                    .WithDescription("Вам пришло предложение сразиться на арене!");
            ComponentBuilder
                .WithButton("Посмотреть", nameof(ArenaOfferInteractions.ArenaSeeOffer), ButtonStyle.Primary);

            await MentionedUser.SendMessageAsync(embed: EmbedBuilder.Build(), components: ComponentBuilder.Build());
        }

        private readonly static MessageComponent Buttons = new ComponentBuilder()
                                            .WithButton("В бой!", nameof(ArenaBattleInteractions.BattleBegin), ButtonStyle.Primary)
                                            .WithButton("Отклонить", nameof(ArenaSearchBattle.ArenaCancelBattle), ButtonStyle.Primary)
                                            .Build();
        private void Player_Ready(object? sender, BattleOffer BattleOffer)
        {
            ArenaSearchBattle.OfferPlayer_Ready(sender, BattleOffer);

            var Sender = (OfferPlayer)sender!;
            OfferPlayer SecondPlayer;

            if (BattleOffer.FirstPlayer == Sender) SecondPlayer = BattleOffer.SecondPlayer;
            else SecondPlayer = BattleOffer.FirstPlayer;

            SecondPlayer.SessionPlayer.SelectedBattleOffer = BattleOffer;

            SecondPlayer.SessionPlayer.User.SendMessageAsync("Выберите действие:", components: Buttons);
        }
    }
}
