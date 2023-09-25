using Discord;
using Discord.Interactions;
using SoupArena.DataBase;
using SoupArena.Models.Player.DB;

namespace SoupArena.Discord.Modules.Interactions
{
    public class RatingInteraction : InteractionModuleBase<SocketInteractionContext>
    {
        private const string NoOnePlaceholder = "---- ----";

        private readonly static MessageComponent RatingButtons = new ComponentBuilder()
                                            .WithButton("Назад", nameof(MainInteractions.MainMenu), ButtonStyle.Secondary)
                                            .WithButton("Выбери ТОП:", "lol", ButtonStyle.Secondary, disabled: true)
                                            .WithButton("Серебро", nameof(RatingSilver), ButtonStyle.Success, row: 1)
                                            .WithButton("Арена", nameof(RatingArena), ButtonStyle.Success, row: 1)
                                            .Build();

        [ComponentInteraction(nameof(RatingEnter))]
        public async Task RatingEnter()
        {
            await DeferAsync(true);

            var EmbedBuilder = new EmbedBuilder()
                                .WithTitle("Soup Arena | Рейтинг")
                                .WithDescription($"""
                                                  {Context.User.Mention}, вот список доступных ТОПов

                                                  Выбирай, какой ТОП желаешь посмотреть.
                                                  """)
                                .WithThumbnailUrl(Paths.CupIcon)
                                .WithColor(Color.DarkGreen);

            await Context.Interaction.FollowupAsync(ephemeral: true, components: RatingButtons, embed: EmbedBuilder.Build());
        }

        [ComponentInteraction(nameof(RatingSilver))]
        public async Task RatingSilver() => await ShowRating("Всего монет", Paths.SilverEmoji, Player => Player.Silver);
        [ComponentInteraction(nameof(RatingArena))]
        public async Task RatingArena() => await ShowRating("Всего побед на арене", ":crossed_swords:", Player => Player.Wins);

        private async Task ShowRating<T>(string Title, string Emoji, Func<DBPlayer, T> KeySelector)
        {
            using var DB = new DBContext();

            List<DBPlayer?> Players = DB.Players.OrderByDescending(KeySelector).ToList()!;

            var PlayerIndex = Players.FindIndex(x => x!.DiscordID == Context.User.Id);
            var Player = Players[PlayerIndex]!;

            if (Players.Count < 10)
                Players.AddRange(Enumerable.Repeat<DBPlayer?>(null, 10 - Players.Count));

            var Users = Players
                .Take(10)
                .Select(x => x is null ? ValueTask.FromResult<IUser?>(null) : Context.Client.GetUserAsync(x.DiscordID))
                .ToList();

            var EmbedBuilder = new EmbedBuilder()
                                .WithTitle("Soup Arena | Рейтинг")
                                .WithDescription($"""
                                                  **
                                                  {Title}
                                                  ТОП-10 игроков

                                                  <:1070345224716562523:1123210914640973935> #1 {(await Users[0])?.Username ?? NoOnePlaceholder}
                                                  {Emoji} {(Players[0] is null ? 0 : KeySelector(Players[0]!))}

                                                  <:1070345228571131974:1123210916431933472> #2 {(await Users[1])?.Username ?? NoOnePlaceholder}
                                                  {Emoji} {(Players[1] is null ? 0 : KeySelector(Players[1]!))}

                                                  <:1070345230605357119:1123210919858675723> #3 {(await Users[2])?.Username ?? NoOnePlaceholder}
                                                  {Emoji} {(Players[2] is null ? 0 : KeySelector(Players[2]!))}

                                                  #4 {(await Users[3])?.Username ?? NoOnePlaceholder}
                                                  {Emoji} {(Players[3] is null ? 0 : KeySelector(Players[3]!))}

                                                  #5 {(await Users[4])?.Username ?? NoOnePlaceholder}
                                                  {Emoji} {(Players[4] is null ? 0 : KeySelector(Players[4]!))}

                                                  #6 {(await Users[5])?.Username ?? NoOnePlaceholder}
                                                  {Emoji} {(Players[5] is null ? 0 : KeySelector(Players[5]!))}

                                                  #7 {(await Users[6])?.Username ?? NoOnePlaceholder}
                                                  {Emoji} {(Players[6] is null ? 0 : KeySelector(Players[6]!))}

                                                  #8 {(await Users[7])?.Username ?? NoOnePlaceholder}
                                                  {Emoji} {(Players[7] is null ? 0 : KeySelector(Players[7]!))}

                                                  #9 {(await Users[8])?.Username ?? NoOnePlaceholder}
                                                  {Emoji} {(Players[8] is null ? 0 : KeySelector(Players[8]!))}

                                                  #10 {(await Users[9])?.Username ?? NoOnePlaceholder}
                                                  {Emoji} {(Players[9] is null ? 0 : KeySelector(Players[9]!))}

                                                  ---- ---- ---- ----

                                                  #{PlayerIndex + 1} {(await Context.Client.GetUserAsync(Player.DiscordID)).Username}
                                                  {Emoji} {KeySelector(Player)}
                                                  **
                                                  """)
                                .WithThumbnailUrl(Paths.CupIcon)
                                .WithColor(Color.DarkGreen);

            await RespondAsync(ephemeral: true, components: RatingButtons, embed: EmbedBuilder.Build());
        }
    }
}
