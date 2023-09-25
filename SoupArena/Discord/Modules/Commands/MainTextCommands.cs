using Discord.Commands;

namespace SoupArena.Discord.Modules.Commands
{
    public class MainTextCommands : ModuleBase<SocketCommandContext>
    {
        [Command("хейм")]
        public async Task MainMenu()
        {
            if (Context.Channel.Id != DiscordBot.Instance.Params.BotChannelID)
                return;

            await ReplyAsync(embed: MainCommands.Embed, components: MainCommands.Buttons);
        }
    }
}
