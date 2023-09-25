using Discord;
using Discord.Interactions;
using SoupArena.Models.Battle;
using SoupArena.Models.Battle.Offer;

namespace SoupArena.Discord.Modules.Interactions.Arena
{
    public class ArenaBattleInteractions : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly static MessageComponent Buttons = new ComponentBuilder()
                .WithButton("Назад", nameof(ArenaEntarenceInteractions.ArenaEnter), ButtonStyle.Primary)
                .Build();
        private readonly static Embed Embed = new EmbedBuilder()
                        .WithTitle("Soup Arena | Арена")
                        .WithThumbnailUrl(Paths.ArenaBattleIcon)
                        .WithColor(Color.DarkGreen)
                        .WithDescription("""
                                     Ожидаем согласия противника!
                                     Как только противник примет бой, битва продолжится в личных сообщениях с ботом.
                                     """)
                        .Build();

        [ComponentInteraction(nameof(BattleBegin))]
        public async Task BattleBegin()
        {
            var SessionPlayer = DiscordBot.Instance.GetSessionPlayer(Context.User.Id)!;

            if (SessionPlayer.SelectedBattleOffer is null)
            {
                await RespondAsync("Нет активных предложений боя.", ephemeral: true, components: Buttons);
                return;
            }

            OfferPlayer OfferPlayer;
            if (SessionPlayer.SelectedBattleOffer.FirstPlayer.SessionPlayer == SessionPlayer)
            {
                OfferPlayer = SessionPlayer.SelectedBattleOffer.FirstPlayer;
            }
            else
            {
                OfferPlayer = SessionPlayer.SelectedBattleOffer.SecondPlayer;
            }
            SessionPlayer.SelectedBattleOffer.SecondPlayer.SessionPlayer.SelectedBattleOffer = SessionPlayer.SelectedBattleOffer;

			OfferPlayer.SetReady();

            await RespondAsync(ephemeral: true, components: Buttons, embed: Embed);
        }

        public async Task BattleButton(int MoveType)
        {
            var SessionPlayer = DiscordBot.Instance.GetSessionPlayer(Context.User.Id)!;

            if (SessionPlayer.CurrentBattle is null)
            {
                await RespondAsync("У Вас нет активного боя.", ephemeral: true, components: Buttons);
                return;
            }

            BattlePlayer BattlePlayer = SessionPlayer.CurrentBattle.FirstPlayer.SessionPlayer == SessionPlayer ? SessionPlayer.CurrentBattle.FirstPlayer : SessionPlayer.CurrentBattle.SecondPlayer;

            await SessionPlayer.CurrentBattle.Move(BattlePlayer, (MoveType)MoveType);
        }

        [ComponentInteraction("0")]
        public async Task BattleButton0() => await BattleButton(0);
        [ComponentInteraction("1")]
        public async Task BattleButton1() => await BattleButton(1);
        [ComponentInteraction("2")]
        public async Task BattleButton2() => await BattleButton(2);
        [ComponentInteraction("3")]
        public async Task BattleButton3() => await BattleButton(3);
        [ComponentInteraction("4")]
        public async Task BattleButton4() => await BattleButton(4);
        [ComponentInteraction("5")]
        public async Task BattleButton5() => await BattleButton(5);
        [ComponentInteraction("6")]
        public async Task BattleButton6() => await BattleButton(6);
        [ComponentInteraction("7")]
        public async Task BattleButton7() => await BattleButton(7);
        [ComponentInteraction("8")]
        public async Task BattleButton8() => await BattleButton(8);
        [ComponentInteraction("9")]
        public async Task BattleButton9() => await BattleButton(9);
        [ComponentInteraction("10")]
        public async Task BattleButton10() => await BattleButton(10);
        [ComponentInteraction("11")]
        public async Task BattleButton11() => await BattleButton(11);
    }
}
