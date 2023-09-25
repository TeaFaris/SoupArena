using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using SoupArena.DataBase;
using SoupArena.Discord.Handlers;
using SoupArena.Discord.Params;
using SoupArena.Discord.Services;
using SoupArena.Models.Player.DB;
using SoupArena.Models.Player;
using RunMode = Discord.Interactions.RunMode;
using SoupArena.Models.SmartEnums;
using C3.SmartEnums;

namespace SoupArena.Discord
{
    public class DiscordBot
    {
        public static DiscordBot Instance { get; private set; } = null!;
        public required DiscordBotParams Params { get; init; }
        public DiscordSocketClient? Client { get; private set; }
        public List<SessionPlayer> SessionPlayers { get; init; } = new List<SessionPlayer>();

        public DiscordBot() => Instance = this;

        public async Task Run()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,
                HandlerTimeout = Timeout.Infinite,
                AlwaysDownloadUsers = true
            });

            var InteractionService = new InteractionService(Client, new InteractionServiceConfig
            {
                DefaultRunMode = RunMode.Async
            });

            var CommandService = new CommandService();
            var LoggingService = new LoggingService(Client, CommandService);
            var SlashCommandsService = new SlashCommandsService(Client, InteractionService, Params.TestGuildID);

            var SlashCommandsHandler = new SlashCommandsHandler(Client, InteractionService);
            var CommandHandler = new CommandHandler(Client, CommandService);
            var InteractionHandler = new InteractionsHandler(Client, InteractionService);

            await SlashCommandsHandler.InitializeAsync();
            await CommandHandler.InitializeAsync();
            await InteractionHandler.InitializeAsync();

            await Client.LoginAsync(TokenType.Bot, Params.Token);
            await Client.StartAsync();

            await Client.SetGameAsync("Soup Arena", type: ActivityType.Playing);
        }
        public async Task AddNewPlayer(ulong DiscordID)
        {
            using var DB = new DBContext();

            if (!DB.Players.ToList().Any(x => x.DiscordID == DiscordID))
            {
                var Consumables = new DBInventory();
                var ConsumableEquiped = new DBInventory();
                await DB.Inventories.AddRangeAsync(Consumables, ConsumableEquiped);

                DBPlayer NewPlayer = new()
                {
                    DiscordID = DiscordID,
                    Consumables = Consumables,
                    ConsumablesEquiped = ConsumableEquiped,
                    Silver = 500
                };

                Consumable[] AllConsumables = SmartEnumExtentions
                                    .GetValues<Consumable, short>();
                NewPlayer.Consumables.Items = AllConsumables
                    .Select(x => new DBInventoryConsumable() { ConsumableID = x.ID, Inventory = NewPlayer.Consumables })
                    .ToList();
                NewPlayer.ConsumablesEquiped.Items = AllConsumables
                    .Select(x => new DBInventoryConsumable() { ConsumableID = x.ID, Inventory = NewPlayer.ConsumablesEquiped })
                    .ToList();

                DB.Players.Add(NewPlayer);
                await DB.SaveChangesAsync();
            }

            if (!SessionPlayers.Any(x => x.DiscordID == DiscordID))
            {
                SessionPlayers.Add(new SessionPlayer(DiscordID));
            }
        }
        public SessionPlayer? GetSessionPlayer(ulong DiscordID) => SessionPlayers.Find(x => x.DiscordID == DiscordID);
    }
}
