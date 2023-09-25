using Discord;
using Discord.Interactions;
using SoupArena.Discord.Modules.Interactions;
using SoupArena.Discord.Modules.Interactions.Arena;

namespace SoupArena.Discord.Modules.Commands
{
    public class MainCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public readonly static Embed Embed = new EmbedBuilder()
                                        .WithTitle("Soup Arena | Хейм")
                                        .WithImageUrl(Paths.MainMenu)
                                        .WithColor(Color.DarkGreen)
                                        .Build();

        public readonly static MessageComponent Buttons = new ComponentBuilder()
                                        .WithButton(customId: nameof(ArenaEntarenceInteractions.ArenaEnter), emote: Emote.Parse("<:1070417125967155321:1123210922727575563>"))
                                        .WithButton(customId: nameof(InventoryInteractions.InventoryEnter), emote: Emote.Parse("<:1070417194720165968:1123210924405305344>"))
                                        .WithButton(customId: nameof(MerchantInteractions.MerchantEnter), emote: Emote.Parse("<:1070417227267981353:1123210927270002758>"))
                                        .WithButton(customId: nameof(RatingInteraction.RatingEnter), emote: Emote.Parse("<:1070417472173379704:1123210928863842345>"))
                                        .WithButton(customId: nameof(BonusInteractions.TakeBonus), emote: Emote.Parse("<:1091715563702722590:1123211167213572157>"))
                                        .Build();

        [SlashCommand("хейм", "Главное меню.")]
        public async Task MainMenu()
        {
            await DeferAsync(true);

            await Context.Interaction.FollowupAsync(ephemeral: true, components: Buttons, embed: Embed);
        }
    }
}
