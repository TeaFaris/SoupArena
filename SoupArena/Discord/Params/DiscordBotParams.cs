namespace SoupArena.Discord.Params
{
    public class DiscordBotParams
    {
        public string Token { get; init; } = "";
        public ulong[] Admins { get; set; } = new ulong[1];
        public ulong TestGuildID { get; init; }
        public ulong BotChannelID { get; init; }
        public ulong GuildID { get; init; }
        public ulong LogChannelId { get; init; }
    }
}
