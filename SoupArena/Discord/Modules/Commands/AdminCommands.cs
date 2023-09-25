using C3.SmartEnums;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SoupArena.DataBase;
using SoupArena.Discord;
using SoupArena.Models.Player.DB;
using SoupArena.Models.SmartEnums;
using System.Diagnostics;

namespace SoupArena.Modules.Commands
{
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public class AdminCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private async Task<bool> IsAdmin()
        {
            if (!DiscordBot.Instance.Params.Admins.Contains(Context.User.Id))
            {
                await RespondAsync("У Вас нет прав на выполнение этой команды.", ephemeral: true);
                return false;
            }
            return true;
        }

		[SlashCommand("ping", "Посмотреть задержку бота.")]
		public async Task Ping()
        {
            Stopwatch SW = Stopwatch.StartNew();
            await ReplyAsync("Pong!");
            SW.Stop();
            await ReplyAsync($"Задержка: {SW.ElapsedMilliseconds} мс.");
        }

        [SlashCommand("вайп-рейтинга", "Сбрасывает весь рейтинг всех игроков")]
        public async Task RatingWipe()
        {
            if (!await IsAdmin())
                return;

            using var DB = new DBContext();
            var Action = DB
                .Players
                .ForEachAsync(Player => Player.Wins = 0);

            await RespondAsync("Вайпнули весь рейтинг.", ephemeral: true);

            await Action;
            await DB.SaveChangesAsync();
        }
        [SlashCommand("сброс-рейтинга", "Сбрасывает рейтинг игроку")]
        public async Task ResetRating([Summary("Игрок")] IUser User)
        {
            if (!await IsAdmin())
                return;

            using var DB = new DBContext();
            var Player = await DB.Players.FindAsync(User.Id);
            
            if (Player is null)
            {
                await RespondAsync($"Игрок {User.Mention} не найден в базе данных.", ephemeral: true);
                return;
            }

            Player.Wins = 0;
            await RespondAsync($"Вайпнули весь рейтинг игроку {User.Mention}.", ephemeral: true);

            DB.Update(Player);
            await DB.SaveChangesAsync();
        }

        [SlashCommand("начислить-серебро", "Начислять игроку")]
        public async Task AddSilver([Summary("Игрок")] IUser User, [Summary("Количество")] uint Amount)
        {
            if (!await IsAdmin())
                return;

            using var DB = new DBContext();
            var Player = await DB.Players.FindAsync(User.Id);
            if(Player is null)
            {
                await RespondAsync($"Игрок {User.Mention} не найден в базе данных.", ephemeral: true);
                return;
            }

            Player.Silver += Amount;
            await RespondAsync($"Начислили игроку {User.Mention} {Amount} серебра. Теперь у него {Player.Silver} серебра.", ephemeral: true);

            DB.Update(Player);
            await DB.SaveChangesAsync();
        }
        [SlashCommand("начислить-серебро-всем", "Начислить всем")]
        public async Task AddSilver([Summary("Количество")] uint Amount)
        {
            if (!await IsAdmin())
                return;

            using var DB = new DBContext();
            DB
                .Players
                .AsParallel()
                .ForAll(Player => Player.Silver += Amount);

            await RespondAsync($"Начислили всем игрокам {Amount} серебра.", ephemeral: true);
            await DB.SaveChangesAsync();
        }

        [SlashCommand("снять-серебро", "Снять игроку")]
        public async Task RemoveSilver([Summary("Игрок")] IUser User, [Summary("Количество")] uint Amount)
        {
            if (!await IsAdmin())
                return;

            using var DB = new DBContext();
            var Player = await DB.Players.FindAsync(User.Id);

            if (Player is null)
            {
                await RespondAsync($"Игрок {User.Mention} не найден в базе данных.", ephemeral: true);
                return;
            }
            if(Player.Silver < Amount)
            {
                await RespondAsync($"У игрока {User.Mention} не достаточно серебра.", ephemeral: true);
                return;
            }
            Player.Silver -= Amount;
            await RespondAsync($"Сняли игроку {User.Mention} {Amount} серебра. Теперь у него {Player.Silver} серебра.", ephemeral: true);

            DB.Update(Player);
            await DB.SaveChangesAsync();
        }
        [SlashCommand("снять-серебро-всем", "Снять всем")]
        public async Task RemoveSilver([Summary("Количество")] uint Amount)
        {
            if (!await IsAdmin())
                return;

            using var DB = new DBContext();
            DB
                .Players
                .AsParallel()
                .ForAll(Player =>
                {
                    if (Player.Silver < Amount)
                    {
                        Player.Silver = 0;
                        return;
                    }
                    Player.Silver -= Amount;
                });

            await RespondAsync($"Сняли всем игрокам {Amount} серебра.", ephemeral: true);

            await DB.SaveChangesAsync();
        }

        [SlashCommand("начислить-расходник", "Начислить игроку")]
        public async Task AddItem([Summary("Игрок")] IUser User, [Summary("Расходник")] string ItemName, [Summary("Количество")] uint Amount)
        {
            if (!await IsAdmin())
                return;

            using var DB = new DBContext();
            var Player = (await DB
                .Players
                .Include(x => x.Consumables)
                .Include(x => x.Consumables.Items)
                .FirstOrDefaultAsync(x => x.DiscordID == User.Id));

            if (Player is null)
            {
                await RespondAsync($"Игрок {User.Mention} не найден в базе данных.", ephemeral: true);
                return;
            }

            var Item = Player
                .Consumables
                .Items
                .Find(x => x.Consumable.Name == ItemName);

            if(Item is null)
            {
                await RespondAsync($"Расходник \"{ItemName}\" не найден в базе данных.", ephemeral: true);
                return;
            }

            Item.Amount += Amount;
            await RespondAsync($"Начислили игроку {User.Mention} {Amount} расходников типа \"{ItemName}\". Теперь у него {Item.Amount} расходников типа \"{ItemName}\".", ephemeral: true);

            DB.Update(Player);
            await DB.SaveChangesAsync();
        }
        [SlashCommand("начислить-расходник-всем", "Начислить всем")]
        public async Task AddItem([Summary("Расходник")] string ItemName, [Summary("Количество")] uint Amount)
        {
            if (!await IsAdmin())
                return;

            if(!SmartEnumExtentions.GetValues<Consumable, short>().Any(x => x.Name == ItemName))
            {
                await RespondAsync($"Расходник \"{ItemName}\" не найден в базе данных.", ephemeral: true);
                return;
            }

            using var DB = new DBContext();
            DB
                .Players
                .Include(x => x.Consumables)
                .Include(x => x.Consumables.Items)
                .AsParallel()
                .ForAll(Player =>
                {
                    var Item = Player.Consumables.Items.Find(x => x.Consumable.Name == ItemName)!;
                    Item.Amount += Amount;
                });

            await RespondAsync($"Выдали всем игрокам {Amount} расходников типа \"{ItemName}\".", ephemeral: true);

            await DB.SaveChangesAsync();
        }

        [SlashCommand("снять-расходник", "Снять игроку")]
        public async Task RemoveItem([Summary("Игрок")] IUser User, [Summary("Расходник")] string ItemName, [Summary("Количество")] uint Amount)
        {
            if (!await IsAdmin())
                return;

            using var DB = new DBContext();
            var Player = (await DB
                .Players
                .Include(x => x.Consumables)
                .Include(x => x.Consumables.Items)
                .FirstOrDefaultAsync(x => x.DiscordID == User.Id));

            if (Player is null)
            {
                await RespondAsync($"Игрок {User.Mention} не найден в базе данных.", ephemeral: true);
                return;
            }

            DBInventoryConsumable? Item = Player
                .Consumables
                .Items
                .Find(x => x.Consumable.Name == ItemName);

            if (Item is null)
            {
                await RespondAsync($"Расходник \"{ItemName}\" не найден в базе данных.", ephemeral: true);
                return;
            }

            if(Item.Amount < Amount)
            {
                await RespondAsync($"У игрока {User.Mention} не достаточно расходников.", ephemeral: true);
                return;
            }

            Item.Amount -= Amount;
            await RespondAsync($"Сняли игроку {User.Mention} {Amount} расходников типа \"{ItemName}\". Теперь у него {Item.Amount} расходников типа \"{ItemName}\".", ephemeral: true);

            DB.Update(Player);
            await DB.SaveChangesAsync();
        }
        [SlashCommand("снять-расходник-всем", "Снять всем")]
        public async Task RemoveItem([Summary("Расходник")] string ItemName, [Summary("Количество")] uint Amount)
        {
            if (!await IsAdmin())
                return;

            if (!SmartEnumExtentions.GetValues<Consumable, short>().Any(x => x.Name == ItemName))
            {
                await RespondAsync($"Расходник \"{ItemName}\" не найден в базе данных.", ephemeral: true);
                return;
            }

            using var DB = new DBContext();
            DB
                .Players
                .Include(x => x.Consumables)
                .Include(x => x.Consumables.Items)
                .AsParallel()
                .ForAll(Player =>
                {
                    var Item = Player.Consumables.Items.Find(x => x.Consumable.Name == ItemName)!;
                    if (Item.Amount < Amount)
                    {
                        Item.Amount = 0;
                        return;
                    }

                    Item.Amount -= Amount;
                });

            await RespondAsync($"Сняли всем игрокам {Amount} расходников типа \"{ItemName}\".", ephemeral: true);

            await DB.SaveChangesAsync();
        }
    }
}
