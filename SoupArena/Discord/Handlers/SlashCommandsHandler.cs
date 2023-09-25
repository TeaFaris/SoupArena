using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;

namespace SoupArena.Discord.Handlers
{
    public class SlashCommandsHandler
    {
        private readonly DiscordSocketClient Client;
        private readonly InteractionService Commands;
        public SlashCommandsHandler(DiscordSocketClient Client, InteractionService Commands)
        {
            this.Client = Client;
            this.Commands = Commands;
        }

        public async Task InitializeAsync()
        {
            await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            Commands.SlashCommandExecuted += SlashCommandExecuted;
            Commands.ContextCommandExecuted += ContextCommandExecuted;
            Commands.ComponentCommandExecuted += ComponentCommandExecuted;
        }

        private async Task ComponentCommandExecuted(ComponentCommandInfo Arg1, IInteractionContext Arg2, IResult Arg3)
        {
            if (!Arg3.IsSuccess)
            {
                Console.WriteLine(Arg3.ErrorReason);
            }

            await DiscordBot.Instance.AddNewPlayer(Arg2.User.Id);
        }

        private async Task ContextCommandExecuted(ContextCommandInfo Arg1, IInteractionContext Arg2, IResult Arg3)
        {
            if (!Arg3.IsSuccess)
            {
                Console.WriteLine(Arg3.ErrorReason);
            }

            await DiscordBot.Instance.AddNewPlayer(Arg2.User.Id);
        }

        private async Task SlashCommandExecuted(SlashCommandInfo Arg1, IInteractionContext Arg2, IResult Arg3)
        {
            if (!Arg3.IsSuccess)
            {
                Console.WriteLine(Arg3.ErrorReason);
            }

            await DiscordBot.Instance.AddNewPlayer(Arg2.User.Id);
        }
    }
}