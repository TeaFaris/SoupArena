using Discord;
using Discord.Interactions;
using SoupArena.Models.Battle;
using SoupArena.Models.Battle.Offer;
using SoupArena.Models.Player;
using System.Diagnostics;
using Timer = System.Timers.Timer;

namespace SoupArena.Discord.Modules.Interactions.Arena
{
	public class ArenaSearchBattle : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly TimeSpan BattleSearchTime = new TimeSpan(0, 5, 0);

		private readonly static MessageComponent ArenaSearchButtons = new ComponentBuilder()
				.WithButton("Прервать поиск", nameof(ArenaStopSearch), ButtonStyle.Primary)
				.Build();

		[ComponentInteraction(nameof(ArenaSearchStart))]
		public async Task ArenaSearchStart()
		{
			var CurrentArena = Models.Arena.Arena.Basic;

			var SessionPlayer = DiscordBot.Instance.GetSessionPlayer(Context.User.Id)!;

			if (SessionPlayer.SelectedBattleOffer is not null)
			{
				await RespondAsync("Примите или отклоните текущий бой!", ephemeral: true);
				return;
			}

			var OfferPlayer = new OfferPlayer() { SessionPlayer = SessionPlayer };

			var SW = new Stopwatch();
			var Timer = new Timer
			{
				Interval = 3000
			};
			Timer.Elapsed += Timer_Elapsed;
			OfferPlayer.BattleFound += (_, _) =>
			{
				Timer?.Stop();
				Timer?.Close();
				Timer?.Dispose();
				SW?.Stop();

				Timer = null;
				SW = null;
			};

			OfferPlayer.BattleFound += OfferPlayer_BattleFound;
			OfferPlayer.Ready += OfferPlayer_Ready;
			CurrentArena.AddPlayerToQueue(OfferPlayer);

			EmbedBuilder EmbedBuilder = new EmbedBuilder()
								.WithTitle("Soup Arena | Поиск боя")
								.WithDescription($"""
                                                  Поиск боя длится {BattleSearchTime.Minutes} минут.
                                                  
                                                  **Идёт поиск: 00:00**
                                                  """)
								.WithThumbnailUrl(Paths.ArenaSearchMatchIcon)
								.WithColor(Color.DarkGreen);

			await RespondAsync(ephemeral: true, components: ArenaSearchButtons, embed: EmbedBuilder.Build());
			Timer?.Start();
			SW?.Start();

			async void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs Args)
			{
				EmbedBuilder EmbedBuilder = new EmbedBuilder()
									.WithTitle("Soup Arena | Поиск боя")
									.WithDescription($"""
                                                      Поиск боя длится {BattleSearchTime.Minutes} минут.
                                                      
                                                      **Идёт поиск: {SW.Elapsed.Minutes:D2}:{SW.Elapsed.Seconds:D2}**
                                                      """)
									.WithThumbnailUrl(Paths.ArenaSearchMatchIcon)
									.WithColor(Color.DarkGreen);

				if (SW.Elapsed >= BattleSearchTime)
				{
					Timer?.Stop();
					Timer?.Close();
					Timer?.Dispose();
					SW?.Stop();

					Timer = null;
					SW = null;

					var OfferPlayer = CurrentArena.GetPlayerFromQueue(SessionPlayer);
					OfferPlayer.Dispose();

					CurrentArena.RemovePlayerFromQueue(OfferPlayer);

					await ModifyOriginalResponseAsync(MessageProp =>
					{
						MessageProp.Embed = EmbedBuilder
												.WithDescription($"""
                                                                  Поиск боя длится {BattleSearchTime.Minutes} минут.
                                                                   
                                                                  **Время вышло, соперник не найден.**
                                                                  """)
												.Build();
						MessageProp.Components = new ComponentBuilder()
													.WithButton("Назад", nameof(ArenaEntarenceInteractions.ArenaEnter), ButtonStyle.Secondary)
													.WithButton("Новый поиск", nameof(ArenaSearchStart), ButtonStyle.Primary)
													.Build();
					});
				}

				await ModifyOriginalResponseAsync(MessageProp => MessageProp.Embed = EmbedBuilder.Build());
			}
		}

		private readonly static MessageComponent Buttons = new ComponentBuilder()
				.WithButton("Назад", nameof(ArenaEntarenceInteractions.ArenaEnter), ButtonStyle.Primary)
				.Build();
		[ComponentInteraction(nameof(ArenaStopSearch))]
		public async Task ArenaStopSearch()
		{
			var SessionPlayer = DiscordBot.Instance.GetSessionPlayer(Context.User.Id)!;
			var CurrentArena = Models.Arena.Arena.Basic;

			var OfferPlayer = CurrentArena.GetPlayerFromQueue(SessionPlayer);
			if (OfferPlayer is null)
			{
				await RespondAsync("У Вас нет активного поиска.", ephemeral: true, components: Buttons);
				return;
			}

			OfferPlayer.InvokeBattleFounded(null, null!);
			OfferPlayer.Dispose();

			CurrentArena.RemovePlayerFromQueue(OfferPlayer);

			await RespondAsync("Вы отменили поиск.", ephemeral: true, components: Buttons);
		}

		[ComponentInteraction(nameof(ArenaCancelBattle))]
		public async Task ArenaCancelBattle()
		{
			var SessionPlayer = DiscordBot.Instance.GetSessionPlayer(Context.User.Id)!;

			if (SessionPlayer.SelectedBattleOffer is null)
			{
				await RespondAsync("Нет активных предложений боя.", ephemeral: true, components: Buttons);
				return;
			}

			OfferPlayer FirstPlayer, SecondPlayer;
			if (SessionPlayer.SelectedBattleOffer.FirstPlayer.SessionPlayer == SessionPlayer)
			{
				FirstPlayer = SessionPlayer.SelectedBattleOffer.FirstPlayer;
				SecondPlayer = SessionPlayer.SelectedBattleOffer.SecondPlayer;
			}
			else
			{
				FirstPlayer = SessionPlayer.SelectedBattleOffer.SecondPlayer;
				SecondPlayer = SessionPlayer.SelectedBattleOffer.FirstPlayer;
			}

			FirstPlayer.SessionPlayer.BattleOffers.Remove(FirstPlayer.SessionPlayer.SelectedBattleOffer!);

			FirstPlayer.SessionPlayer.SelectedBattleOffer = null;
			SecondPlayer.SessionPlayer.SelectedBattleOffer = null;

			var SecondPlayerUser = SecondPlayer.SessionPlayer.User;

			var EmbedBuilder = new EmbedBuilder()
				.WithColor(Color.DarkGreen)
				.WithTitle("Soup Arena | Поиск боя")
				.WithDescription($"{Context.User.Mention} отклонил предложение битвы.")
				.WithThumbnailUrl(Paths.ArenaSearchMatchIcon);

			await SecondPlayerUser.SendMessageAsync(embed: EmbedBuilder.Build());

			await RespondAsync("Вы отклонили битву!", ephemeral: true, components: Buttons);
		}

		private readonly static MessageComponent BattleFoundButtons = new ComponentBuilder()
												.WithButton("В бой!", nameof(ArenaBattleInteractions.BattleBegin), ButtonStyle.Primary)
												.WithButton("Отклонить", nameof(ArenaCancelBattle), ButtonStyle.Primary)
												.Build();
		private async void OfferPlayer_BattleFound(object? sender, BattleOffer BattleOffer)
		{
			try
			{
				if (sender is null || BattleOffer is null)
					return;

				var Sender = (OfferPlayer)sender!;
				OfferPlayer SecondPlayer;

				if (Sender == BattleOffer.FirstPlayer) SecondPlayer = BattleOffer.SecondPlayer;
				else SecondPlayer = BattleOffer.FirstPlayer;

				var SecondPlayerUser = SecondPlayer.SessionPlayer.User;
				var SenderUser = Sender.SessionPlayer.User;

				Console.WriteLine($"Battle found: {SecondPlayerUser.Username} with {SenderUser.Username}");

				EmbedBuilder EmbedBuilder = new EmbedBuilder()
								.WithTitle("Soup Arena | Поиск боя")
								.WithThumbnailUrl(Paths.ArenaSearchMatchIcon)
								.WithColor(Color.DarkGreen);

				
				while (true)
				{
					try
					{
						await ModifyOriginalResponseAsync(Message =>
						{
							Message.Embed = EmbedBuilder
												.WithDescription($"""
																  {SenderUser.Mention} Соперник найден! Вы готовы к сражению?!
																  
																  Примите бой в личных сообщениях!
																  """)
												.Build();
						});
						await Task.Delay(3000);
						await Sender.SessionPlayer.User.SendMessageAsync(
							embed: EmbedBuilder
										.WithDescription($"""
														 {SenderUser.Mention} Соперник найден! Вы готовы к сражению?!
														 
														 Ваш соперник:
														 {SecondPlayerUser.Mention}
														 """)
										.Build(),
							components: BattleFoundButtons);
						break;
					}
					catch
					{
						await Task.Delay(3000);
					}
				}

				Sender.BattleFound -= OfferPlayer_BattleFound;
			}
			catch(Exception Ex)
			{
				Console.WriteLine(Ex);
			}
		}
		public static async void OfferPlayer_Ready(object? sender, BattleOffer BattleOffer)
		{
			try
			{
				var Sender = (OfferPlayer)sender!;
				OfferPlayer SecondPlayer;

				if (BattleOffer.FirstPlayer == Sender) SecondPlayer = BattleOffer.SecondPlayer;
				else SecondPlayer = BattleOffer.FirstPlayer;

				var SecondPlayerUser = SecondPlayer.SessionPlayer.User;
				var SenderUser = Sender.SessionPlayer.User;

				Console.WriteLine($"{SenderUser.Username} is ready.");

				var EmbedBuilder = new EmbedBuilder()
					.WithColor(Color.DarkGreen)
					.WithTitle("Soup Arena | Поиск боя")
					.WithDescription($"{SenderUser.Mention} принял предложение битвы.")
					.WithThumbnailUrl(Paths.ArenaSearchMatchIcon);

				if (Sender.IsReady ^ SecondPlayer.IsReady)
					BattleOffer.BattleStarted += BattleOffer_BattleStarted;

				Sender.Ready -= OfferPlayer_Ready;

				await SecondPlayerUser.SendMessageAsync(embed: EmbedBuilder.Build());
			}
			catch(Exception Ex)
			{
				Console.WriteLine(Ex);
			}
		}
		public static async void BattleOffer_BattleStarted(object? sender, BattleManager BattleManager)
		{
			try
			{
				Console.WriteLine($"Battle started: {BattleManager.FirstPlayer.SessionPlayer.User.Username} vs {BattleManager.SecondPlayer.SessionPlayer.User.Username}");
				BattlePlayer WhoseMove = BattleManager.FirstPlayer.Cell != BattleManager.WhoseMove ? BattleManager.SecondPlayer : BattleManager.FirstPlayer;
				await BattleManager.Field.Move(WhoseMove, 0);
				await BattleManager.Move(WhoseMove, MoveType.Start);
			}
			catch (Exception Ex)
			{
				Console.WriteLine(Ex);
			}
		}
	}
}
