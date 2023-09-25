using Discord.Interactions;
using Discord.WebSocket;

namespace SoupArena.Discord.Handlers
{
    public class InteractionsHandler
    {
        private readonly DiscordSocketClient Client;
        private readonly InteractionService Interactions;
        public InteractionsHandler(DiscordSocketClient Client, InteractionService Commands)
        {
            this.Client = Client;
            this.Interactions = Commands;
        }

        public async Task InitializeAsync()
        {
            Client.InteractionCreated += HandleInteraction;

            await Task.CompletedTask;
        }
        private async Task HandleInteraction(SocketInteraction Arg)
        {
            try
            {
                await DiscordBot.Instance.AddNewPlayer(Arg.User.Id);
                var CTX = new SocketInteractionContext(Client, Arg);
                await Interactions.ExecuteCommandAsync(CTX, null);
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex);
            }
        }
    }
}
