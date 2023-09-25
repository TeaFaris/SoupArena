using Discord.Interactions;
using SoupArena.Discord.Modules.Commands;

namespace SoupArena.Discord.Modules.Interactions
{
    public class MainInteractions : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction(nameof(MainMenu))]
        public async Task MainMenu()
        {
            await RespondAsync(ephemeral: true, components: MainCommands.Buttons, embed: MainCommands.Embed);
        }
    }
}
