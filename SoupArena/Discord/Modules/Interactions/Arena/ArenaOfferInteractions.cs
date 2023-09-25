using C3.Linq;
using Discord;
using Discord.Interactions;
using System.Text;

namespace SoupArena.Discord.Modules.Interactions.Arena
{
    public class ArenaOfferInteractions : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction(nameof(ArenaSeeOffer))]
        public async Task ArenaSeeOffer()
        {
            var SessionPlayer = DiscordBot.Instance.GetSessionPlayer(Context.User.Id)!;

            if(SessionPlayer.CurrentBattle is not null)
            {
                await RespondAsync(text: "Вы находитесь в бою!", ephemeral: true);
                return;
            }

            EmbedBuilder EmbedBuilder = new EmbedBuilder()
                                .WithTitle("Soup Arena | Принять бой")
                                .WithThumbnailUrl(Paths.ArenaSearchMatchIcon)
                                .WithColor(Color.DarkGreen);
            ComponentBuilder ComponentBuilder = new ComponentBuilder()
                                            .WithButton("Назад", nameof(ArenaEntarenceInteractions.ArenaEnter), ButtonStyle.Secondary);

            SessionPlayer
                .BattleOffers
                .Where(x => x.Value <= DateTime.UtcNow)
                .ForEach(x =>
                {
                    x.Key.FirstPlayer.Dispose();
                    x.Key.SecondPlayer.Dispose();
                    x.Key.BattleStarted -= ArenaSearchBattle.BattleOffer_BattleStarted;
                });

            SessionPlayer.BattleOffers = SessionPlayer
                                            .BattleOffers
                                            .Where(x => x.Value > DateTime.UtcNow)
                                            .ToDictionary(x => x.Key, x => x.Value);

            if (SessionPlayer.BattleOffers.Count == 0)
            {
                EmbedBuilder
                    .WithDescription("""
                                     Предложений сразиться на арене не поступало.
                                     """);

                await RespondAsync(ephemeral: true, components: ComponentBuilder.Build(), embed: EmbedBuilder.Build());
                return;
            }

            var SB = new StringBuilder();

            SB.AppendLine("Предложения боя:");

            var Menu = new SelectMenuBuilder()
                            .WithCustomId(nameof(ChooseAcceptOfferBattle))
                            .WithPlaceholder("Выберите битву:")
                            .WithMinValues(1)
                            .WithMaxValues(1);

            SessionPlayer.BattleOffers.For((x, i) =>
            {
                SB.Append(i + 1).Append(". ").AppendLine(x.Key.FirstPlayer.SessionPlayer.User.Mention);
                Menu.AddOption(x.Key.FirstPlayer.SessionPlayer.User.Username, x.Key.FirstPlayer.SessionPlayer.User.Id.ToString());
            });

            ComponentBuilder.WithSelectMenu(Menu);

            EmbedBuilder
                .WithDescription(SB.ToString());

            await RespondAsync(ephemeral: true, components: ComponentBuilder.Build(), embed: EmbedBuilder.Build());
        }

        private readonly static MessageComponent Buttons = new ComponentBuilder()
                                            .WithButton("Принять", nameof(ArenaBattleInteractions.BattleBegin), ButtonStyle.Primary)
                                            .WithButton("Отклонить", nameof(ArenaSearchBattle.ArenaCancelBattle), ButtonStyle.Primary)
                                            .Build();
        [ComponentInteraction(nameof(ChooseAcceptOfferBattle))]
        private async Task ChooseAcceptOfferBattle()
        {
            var Data = Context.Interaction.Data.GetType().GetProperty("Values")!.GetValue(Context.Interaction.Data);
            var SessionPlayer = DiscordBot.Instance.GetSessionPlayer(Context.User.Id)!;

            if (!((Optional<string[]>)Data!).IsSpecified || !ulong.TryParse(((Optional<string[]>)Data!).Value[0], out ulong SelectedOffer) || SessionPlayer.BattleOffers.FirstOrDefault(x => x.Key.FirstPlayer.SessionPlayer.DiscordID == SelectedOffer).Value <= DateTime.UtcNow)
            {
                await RespondAsync(text: "Предложение уже неактуально!", ephemeral: true);
                return;
            }

            var Offer = SessionPlayer
                .BattleOffers
                .FirstOrDefault(x => x.Key.FirstPlayer.SessionPlayer.DiscordID == SelectedOffer)
                .Key;

            SessionPlayer.SelectedBattleOffer = Offer;

            EmbedBuilder EmbedBuilder = new EmbedBuilder()
                                .WithTitle("Soup Arena | Принять бой")
                                .WithDescription($"{Context.User.Mention} викинг {Offer.FirstPlayer.SessionPlayer.User.Mention} предлагает Вам бой!")
                                .WithThumbnailUrl(Paths.ArenaSearchMatchIcon)
                                .WithColor(Color.DarkGreen);

            await RespondAsync(ephemeral: true, components: Buttons, embed: EmbedBuilder.Build());
        }
    }
}
