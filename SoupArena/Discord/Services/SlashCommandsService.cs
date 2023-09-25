using Discord.Interactions;
using Discord.WebSocket;

namespace SoupArena.Discord.Services
{
    public class SlashCommandsService
    {
        private DiscordSocketClient Client { get; init; }
        public InteractionService InteractionService { get; init; }
        private ulong TestGuildID { get; init; }
        public SlashCommandsService(DiscordSocketClient Client, InteractionService InteractionService, ulong TestGuildID)
        {
            this.Client = Client;
            this.InteractionService = InteractionService;
            this.TestGuildID = TestGuildID;
            Client.Ready += ClientReadyAsync;
        }

        private async Task ClientReadyAsync()
        {
			await InteractionService.RegisterCommandsGloballyAsync(true);

			Console.WriteLine($"Connected as -> [{Client.CurrentUser}].");
        }
    }
}
