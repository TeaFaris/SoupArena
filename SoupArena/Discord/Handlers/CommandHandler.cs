using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;

namespace SoupArena.Discord.Handlers
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient Client;
        private readonly CommandService Commands;

        public CommandHandler(DiscordSocketClient Client, CommandService Commands)
        {
            this.Commands = Commands;
            this.Client = Client;
        }

        public async Task InitializeAsync()
        {
            Client.MessageReceived += HandleCommandAsync;
            await Commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: null);
        }

        private async Task HandleCommandAsync(SocketMessage MessageParam)
        {
            try
            {
               if (MessageParam is not SocketUserMessage Message)
                    return;

                int ArgPos = 0;

                bool HasExclCharPrefix = Message.HasCharPrefix('!', ref ArgPos);
                bool HasSlashCharPrefix = Message.HasCharPrefix('/', ref ArgPos);
                bool HasMentionPrefix = Message.HasMentionPrefix(Client.CurrentUser, ref ArgPos);
                if (!(HasExclCharPrefix || HasMentionPrefix || HasSlashCharPrefix) || Message.Author.IsBot)
                    return;

                var Context = new SocketCommandContext(Client, Message);

                await DiscordBot.Instance.AddNewPlayer(Context.User.Id);

                await Commands.ExecuteAsync(
                    context: Context,
                    argPos: ArgPos,
                    services: null);
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex);
            }
        }
    }
}
