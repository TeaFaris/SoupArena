using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SoupArena.DataBase;

namespace SoupArena.Discord.Modules.Interactions
{
	public class BonusInteractions : InteractionModuleBase<SocketInteractionContext>
	{
		private const ulong BonusInSilver = 250;
		private const uint BonusDelayInHours = 6;

		private readonly static EmbedBuilder BonusEmbed = new EmbedBuilder()
								.WithTitle("Soup Arena | Бонус")
								.WithThumbnailUrl(Paths.BonusIcon)
								.WithColor(Color.DarkGreen);
		private readonly static MessageComponent BonusButtons = new ComponentBuilder()
											.WithButton("Назад", nameof(MainInteractions.MainMenu), ButtonStyle.Secondary)
											.Build();

		[ComponentInteraction(nameof(TakeBonus))]
		public async Task TakeBonus()
		{
			await DeferAsync(true);

			using var DB = new DBContext();

			var Player = (await DB
				.Players
				.FirstOrDefaultAsync(x => x.DiscordID == Context.User.Id))!;

			DateTime? AllowedTimeForBonus = Player.LastTimeClaimedBonus + TimeSpan.FromHours(BonusDelayInHours);

			if (Player.LastTimeClaimedBonus is not null && AllowedTimeForBonus > DateTime.UtcNow)
			{
				BonusEmbed.WithDescription($"Вы уже получали бонус в течении последних {BonusDelayInHours} часов.\nПолучить бонус снова можно через: {AllowedTimeForBonus - DateTime.UtcNow:hh\\:mm\\:ss}.");
			}
			else
			{
				BonusEmbed.WithDescription($"Вы получили бонус в размере {BonusInSilver} серебра.");
				Player.Silver += BonusInSilver;
				Player.LastTimeClaimedBonus = DateTime.UtcNow;

				await DB.SaveChangesAsync();
			}

			await Context.Interaction.FollowupAsync(ephemeral: true, components: BonusButtons, embed: BonusEmbed.Build());
		}
	}
}