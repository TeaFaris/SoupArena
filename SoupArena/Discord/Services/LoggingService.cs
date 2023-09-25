using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace SoupArena.Discord.Services
{
    public class LoggingService
    {
        public LoggingService(DiscordSocketClient Client, CommandService Command)
        {
            Client.Log += LogAsync;
            Command.Log += LogAsync;
        }
        private async Task LogAsync(LogMessage Message)
        {
            if (Message.Exception is CommandException CmdException)
            {
                Console.WriteLine($"[Command/{Message.Severity}] {CmdException.Command.Aliases.First()}"
                    + $" failed to execute in {CmdException.Context.Channel}.");
                Console.WriteLine(CmdException);
            }
            else if(Message.Exception is Exception Ex)
            {
                Console.WriteLine($"[General/{Message.Severity}] {Ex}");
            }

            await Task.CompletedTask;
        }
    }
}
