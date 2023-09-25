using Microsoft.EntityFrameworkCore;
using SoupArena.DataBase;
using SoupArena.Discord;
using SoupArena.Discord.Params;
using System.Text.Json;

const string ParamsPath = "Params.txt";

if (!File.Exists(ParamsPath))
{
    var Json = JsonSerializer.Serialize(new DiscordBotParams());
    await File.WriteAllTextAsync(ParamsPath, Json);
    Console.WriteLine($"{ParamsPath} was created! Fill it with data.");
    return;
}

var JSON = await File.ReadAllTextAsync(ParamsPath);
var Params = JsonSerializer.Deserialize<DiscordBotParams>(JSON)!;

var Bot = new DiscordBot
{
    Params = Params
};

using (var DB = new DBContext())
{
    await DB.Database.MigrateAsync();
    await DB.SaveChangesAsync();
}

try
{
    await Bot.Run();
    await Task.Delay(Timeout.Infinite);
}
catch(Exception Ex)
{
    Console.WriteLine(Ex);
    await Task.Delay(Timeout.Infinite);
}