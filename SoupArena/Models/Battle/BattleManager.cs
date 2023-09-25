using C3.Linq;
using C3.SmartEnums;
using Discord;
using Microsoft.EntityFrameworkCore;
using SoupArena.DataBase;
using SoupArena.Discord;
using SoupArena.Discord.Modules.Interactions;
using SoupArena.Models.Battle.Entities;
using SoupArena.Models.Player;
using SoupArena.Models.SmartEnums;
using System.Diagnostics;
using System.Text;
using Timer = System.Timers.Timer;

namespace SoupArena.Models.Battle
{
    public sealed class BattleManager
    {
        private const uint MaxAFKMoves = 3;
        private const uint GoldReward = 200;
        private readonly static TimeSpan MoveTime = new TimeSpan(0, 1, 0);

        private const string YourMoveMessage = "Сделай шаг или заверши ход.";
        private const string EnemysMoveMessage = "Сейчас ход врага!";
        private readonly DateTime BattleStart = DateTime.UtcNow;

        private readonly Stopwatch Stopwatch = new Stopwatch();
        private readonly Timer Timer = new Timer
        {
            Interval = 3000
        };

        public BattleField Field { get; init; }
        public BattlePlayer FirstPlayer { get; init; }
        public BattlePlayer SecondPlayer { get; init; }
        public BattleCell WhoseMove { get; private set; }
        public readonly StringBuilder BattleMessageLog = new();

        public uint ActionPerformsLeft { get; set; } = 2;
        public uint AdditionalMovesLeft { get; set; } = 0;
        public volatile bool ActionPerformed;
        public volatile bool ConsumableUsed;
        public volatile bool Moved;
        public bool BattleEnded { get; private set; }

        public uint AFKMoves { get; private set; }

        private readonly static MessageComponent Components = new ComponentBuilder()
                .WithButton("Хейм", nameof(MainInteractions.MainMenu), ButtonStyle.Secondary)
                .Build();
        private readonly static TimeZoneInfo MoscowZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

        public BattleManager(SessionPlayer FirstPlayer, SessionPlayer SecondPlayer)
        {
            FirstPlayer.CurrentBattle = this;
            SecondPlayer.CurrentBattle = this;

            Field = new BattleField();

            using var DB = new DBContext();
            var Players = DB
                            .Players
                            .Include(x => x.ConsumablesEquiped)
                            .Include(x => x.ConsumablesEquiped.Items);

            var FirstDBPlayer = Players
                                    .FirstOrDefault(x => x.DiscordID == FirstPlayer.DiscordID)!;
            var SecondDBPlayer = Players
                                    .FirstOrDefault(x => x.DiscordID == SecondPlayer.DiscordID)!;

            var FirstBattlePlayer = new BattlePlayer(new BattleClass(FirstDBPlayer.Class!.Value) { MovesLeft = 0 }, FirstDBPlayer.Equipment!.Value)
            {
                SessionPlayer = FirstPlayer,
                Consumables = FirstDBPlayer
                    .ConsumablesEquiped
                    .Items
                    .OrderBy(x => x.ConsumableID)
                    .Select(x => new BattleConsumable() { ConsumableItem = x.CloneToBattleConsumable(), MovesLeft = 0 })
                    .ToList(),
                Cell = BattleCell.Green
            };
            var SecondBattlePlayer = new BattlePlayer(new BattleClass(SecondDBPlayer.Class!.Value) { MovesLeft = 0 }, SecondDBPlayer.Equipment!.Value)
            {
                SessionPlayer = SecondPlayer,
                Consumables = SecondDBPlayer
                    .ConsumablesEquiped
                    .Items
                    .OrderBy(x => x.ConsumableID)
                    .Select(x => new BattleConsumable() { ConsumableItem = x.CloneToBattleConsumable(), MovesLeft = 0 })
                    .ToList(),
                Cell = BattleCell.Red
            };

            FirstBattlePlayer.OnDeath += OnSomeonesDeath;
            SecondBattlePlayer.OnDeath += OnSomeonesDeath;

            WhoseMove = SmartEnumExtentions.GetByID<BattleCell, byte>((byte)Random.Shared.Next(1, 3))!.Value;

            Console.WriteLine($"Whose move: {WhoseMove}");

            this.FirstPlayer = FirstBattlePlayer;
            this.SecondPlayer = SecondBattlePlayer;

            Timer.Elapsed += Timer_Elapsed;
        }

        public async Task Move(BattlePlayer Player, MoveType MoveType)
        {
#if DEBUG
            var SW = Stopwatch.StartNew();
#endif

            try
            {
                if (Player.Cell != WhoseMove)
                    return;

                BattlePlayer Enemy = Player == FirstPlayer ? SecondPlayer : FirstPlayer;

                var DeleteMessagesTask = DeleteMessages();

                BattleMessageLog.Clear();
				BattleMessageLog.Append(Player.SessionPlayer.User.Mention).Append(' ').Append(Player.Cell.Emoji).Append("\n\n");

                if (MoveType != MoveType.TimeoutMove)
                    AFKMoves = 0;

                if(MoveType != MoveType.Surrender)
					Player.Surrendering = false;

				switch (MoveType)
                {
                    case MoveType.FirstConsumable:
                    case MoveType.SecondConsumable:
                    case MoveType.ThirdConsumable:
                    case MoveType.FourthConsumable:
                    case MoveType.FifthConsumable:
                        {
                            ActionPerformsLeft--;
                            ConsumableUsed = true;

                            var Consumable = Player.Consumables[(int)MoveType];

                            await Consumable.Move(Player, Enemy, this, BattleMessageLog);
                        }
                        break;
                    case MoveType.RightMove:
                    case MoveType.LeftMove:
                        {
                            if (!await Field.Move(Player, MoveType == MoveType.LeftMove ? -1 : 1))
                                break;

                            if (AdditionalMovesLeft != 0)
                            {
                                AdditionalMovesLeft--;
                            }
                            else if (ActionPerformsLeft != 0 && !Moved)
                            {
                                ActionPerformsLeft--;
                                Moved = true;
                            }
                            else if (Moved)
                            {
                                break;
                            }

							BattleMessageLog
								.Append("Сделал шаг: ")
								.Append(MoveType == MoveType.LeftMove ? "Влево" : "Вправо");
                        }
                        break;
                    case MoveType.FirstAbility:
                    case MoveType.SecondAbility:
                        {
                            if (ActionPerformed || ActionPerformsLeft == 0)
                                break;

                            ActionPerformed = true;
                            ActionPerformsLeft--;

                            var Ability = MoveType == MoveType.FirstAbility ? Player.Class.FirstAbility : Player.Class.SecondAbility;

                            await Ability.Move(Player, Enemy, this, BattleMessageLog);
                        }
                        break;
                    case MoveType.Attack:
                        {
                            if (ActionPerformed || ActionPerformsLeft == 0)
                                break;

                            ActionPerformed = true;
                            ActionPerformsLeft--;

                            uint Damage = Player.Equipment.Damage;
                            Enemy.GiveDamage(ref Damage, Player);

                            BattleMessageLog
                                .Append("Нанёс удар оружием.\n-")
                                .Append(Damage)
                                .Append(" здоровья у врага.");
                        }
                        break;
                    case MoveType.TimeoutMove:
                    case MoveType.EndMove:
                        {
                            ActionPerformed = true;
                            Moved = true;
                            ConsumableUsed = true;
                            ActionPerformsLeft = 0;
                            AdditionalMovesLeft = 0;

                            AFKMoves++;

                            BattleMessageLog.Append("Завершил ход.");
                        }
                        break;
                    case MoveType.Surrender:
                        {
                            if (Player.Surrendering)
                            {
                                uint MaxDamage = Player.Health * 10;
                                Player.GiveDamage(ref MaxDamage, Enemy);
                                BattleMessageLog.Append("Поднял белый флаг и сдался!");
                                break;
                            }

                            BattleMessageLog.Append("Вы уверены что хотите сдаться?\nНажмите на кнопку сдаться ещё раз чтобы подтвердить.");

                            Player.Surrendering = true;
                        }
                        break;
                    case MoveType.Start:
                        {
                            Timer.Start();
                            Stopwatch.Start();
                        }
                        break;
                }

				if (AFKMoves >= MaxAFKMoves)
				{
					var MinHP = Player.Health > Enemy.Health ? Enemy : Player;
					uint MaxDamage = MinHP.Health * 10;
					MinHP.GiveDamage(ref MaxDamage, Enemy);
				}

				if (ActionPerformsLeft == 0 &&
                    ActionPerformsLeft == 0 &&
                    AdditionalMovesLeft == 0 &&
                    (
                        (Moved && ConsumableUsed) ||
                        (Moved && ActionPerformed) ||
                        (ActionPerformed && ConsumableUsed)
                    ))
                {
                    ActionPerformed = false;
                    Moved = false;
                    ConsumableUsed = false;

                    if (!BattleEnded)
                        WhoseMove = WhoseMove.Invert()!.Value;

                    ActionPerformsLeft = 2;
                    AdditionalMovesLeft = 0;

                    new List<BattleBuff>(Player.Buffs)
                        .ForEach(async x => await x.Move(Player, Enemy, this, BattleMessageLog));

                    Player.Consumables
                        .Where(x => x.MovesLeft != 0)
                        .ForEach(x => x.MovesLeft--);

                    await Player.Class.Move(Player, Enemy, this, BattleMessageLog);

                    Stopwatch.Restart();
                }

                var PlayerMessage = await GetPlayerMessage(Player, MoveType == MoveType.Start);
                var EnemyMessage = await GetPlayerMessage(Enemy, MoveType == MoveType.Start);

                var PlayerMessageTask = Player.SessionPlayer.User.SendMessageAsync(embed: PlayerMessage.Embed, components: PlayerMessage.Component);
                var EnemyMessageTask = Enemy.SessionPlayer.User.SendMessageAsync(embed: EnemyMessage.Embed, components: EnemyMessage.Component);

				if (BattleEnded)
                {
                    var Winner = FirstPlayer.Health > 0 ? FirstPlayer : SecondPlayer;
                    var Loser = FirstPlayer.Health == 0 ? FirstPlayer : SecondPlayer;

                    var EmbedBuilder = new EmbedBuilder()
                            .WithImageUrl("https://i.imgur.com/PfyG5v7.jpg")
                            .WithColor(Color.DarkGreen);

                    while (true)
                    {
                        try
                        {
                            await Loser.SessionPlayer.User.SendMessageAsync(embed: EmbedBuilder.Build(), components: Components);
                            break;
                        }
                        catch
                        {
							await Task.Delay(1000);
						}
					}

                    EmbedBuilder
                        .WithImageUrl("https://i.imgur.com/24NDiNY.jpg");

                    while (true)
                    {
                        try
                        {
                            await Winner.SessionPlayer.User.SendMessageAsync(embed: EmbedBuilder.Build(), components: Components);
                            break;
                        }
                        catch
                        {
                            await Task.Delay(1000);
                        }
                    }
                }

                await DeleteMessagesTask;

                Player.Message = await PlayerMessageTask;
                Enemy.Message = await EnemyMessageTask;
			}
            catch (Exception Ex)
            {
                Console.WriteLine($"MoveHandler threw an exception: {Ex}");
            }

#if DEBUG
            SW.Stop();
            Console.WriteLine($"Move handled in {SW.ElapsedMilliseconds} ms.");
#endif
        }

        private void OnSomeonesDeath(object? Sender, BattlePlayer Winner)
        {
            BattleEnded = true;

            Timer.Stop();
            Timer.Close();
            Timer.Dispose();

            Stopwatch.Stop();

            WhoseMove = BattleCell.None;
            BattlePlayer Loser = (BattlePlayer)Sender!;

            using var DB = new DBContext();

            var Players = DB
                .Players
                .Include(x => x.Consumables)
                .Include(x => x.Consumables.Items)
                .Include(x => x.ConsumablesEquiped)
                .Include(x => x.ConsumablesEquiped.Items);

            var LoserPlayer = Players.FirstOrDefault(x => x.DiscordID == Loser.SessionPlayer.DiscordID)!;
			Player.DB.DBPlayer WinnerPlayer = Players.FirstOrDefault(x => x.DiscordID == Winner.SessionPlayer.DiscordID)!;

            LoserPlayer.Consumables -= LoserPlayer.ConsumablesEquiped;
            LoserPlayer
                .ConsumablesEquiped
                .Items
                .ForEach(x => x.Amount = 0);

            WinnerPlayer
                .ConsumablesEquiped
                .Items
                .ForEach(x => x.Amount -= Winner
                                            .Consumables
                                            .Select(x => x.ConsumableItem)
                                            .FirstOrDefault(y => y.ConsumableID == x.ConsumableID)!
                                            .Amount);

            WinnerPlayer.Consumables -= WinnerPlayer.ConsumablesEquiped;
            WinnerPlayer
                .ConsumablesEquiped
                .Items
                .ForEach(x => x.Amount = 0);

            WinnerPlayer.Wins++;
            WinnerPlayer.Silver += GoldReward;

            LoserPlayer.Loses++;

            DB.Update(LoserPlayer);
            DB.Update(WinnerPlayer);
            DB.SaveChanges();

            TimeSpan BattleSpan = DateTime.UtcNow - BattleStart;
            var LogEmbedBuilder = new EmbedBuilder()
                .WithTitle("Бой окончился:")
                .WithDescription($"""
                                      Участник 1 = {Winner.SessionPlayer.User.Mention}.
                                      Участник 2 = {Loser.SessionPlayer.User.Mention}.

                                      Победитель = {Winner.SessionPlayer.User.Mention}.
                                      
                                      Начало битвы: {TimeZoneInfo.ConvertTime(BattleStart, MoscowZone)}
                                      Битва длилась: {BattleSpan.Hours} ч. {BattleSpan.Minutes} м. {BattleSpan.Seconds} с.
                                      """)
                .WithTimestamp(TimeZoneInfo.ConvertTime(DateTime.UtcNow, MoscowZone))
                .WithColor(Color.DarkGreen);

            DiscordBot.Instance.Client!.GetGuild(DiscordBot.Instance.Params.GuildID)
                .GetTextChannel(DiscordBot.Instance.Params.LogChannelId)
                .SendMessageAsync(embed: LogEmbedBuilder.Build());

            Loser.SessionPlayer.CurrentBattle = null;
            Winner.SessionPlayer.CurrentBattle = null;
            Loser.SessionPlayer.SelectedBattleOffer = null;
            Winner.SessionPlayer.SelectedBattleOffer = null;

            Loser.OnDeath -= OnSomeonesDeath;
            Winner.OnDeath -= OnSomeonesDeath;
        }
        private async void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if(Stopwatch.Elapsed > MoveTime)
            {
                await Move(FirstPlayer.Cell == WhoseMove ? FirstPlayer : SecondPlayer, MoveType.TimeoutMove);
                return;
            }

            void ModifyMessage(BattlePlayer Player, MessageProperties Message)
            {
                EmbedBuilder EmbedBuilder = Player
                                                .Message!
                                                .Embeds
                                                .First()
                                                .ToEmbedBuilder();
                EmbedFieldBuilder Last = EmbedBuilder.Fields.Last();

                var TimeLeft = MoveTime - Stopwatch.Elapsed;

                var Field = EmbedBuilder
                                .Fields
                                .Last()
                                .WithValue($":stopwatch: {TimeLeft.Minutes:D2}:{TimeLeft.Seconds:D2}");

                Message.Embed = EmbedBuilder.Build();
            }
            try
            {
                if (FirstPlayer.Message is not null)
                    await FirstPlayer.Message.ModifyAsync(Message => ModifyMessage(FirstPlayer, Message));
            }
            catch { }
            try
            {
                if (SecondPlayer.Message is not null)
                    await SecondPlayer.Message.ModifyAsync(Message => ModifyMessage(SecondPlayer, Message));
            }
            catch { }
        }

        private async Task<(Embed Embed, MessageComponent Component)> GetPlayerMessage(BattlePlayer Player, bool Start = false)
        {
            BattlePlayer Enemy = Player == FirstPlayer ? SecondPlayer : FirstPlayer;

            if (Start)
            {
                BattleMessageLog.Clear();
                if (Player.Cell != WhoseMove)
                {
                    BattleMessageLog
                        .AppendLine("Ты ходишь вторым!")
                        .AppendLine("Бог Один отдал первый ход твоему врагу.")
                        .AppendLine()
                        .Append("Твой враг: ")
                        .AppendLine(Enemy.Class.Class.Name)
                        .Append("Твой персонаж: ")
                        .AppendLine(Player.Cell.ToString())
                        .AppendLine()
                        .Append("Ожидаем действий от врага.");
                }
                else
                {
                    BattleMessageLog
                        .AppendLine("Бог Один даровал тебе шанс ходить первым!")
                        .AppendLine()
                        .Append("Твой враг: ")
                        .AppendLine(Enemy.Class.Class.Name)
                        .Append("Твой персонаж: ")
                        .AppendLine(Player.Cell.ToString())
                        .AppendLine()
                        .AppendLine("Твои действия?")
                        .Append("У тебя 1 минута для принятия решения.");
                }
            }

            bool YourMove = Player.Cell == WhoseMove;

            if (YourMove)
                BattleMessageLog.Replace("\n\n" + EnemysMoveMessage, string.Empty);
            else
                BattleMessageLog.Replace("\n\n" + YourMoveMessage, string.Empty);

            var EmbedBuilder = new EmbedBuilder()
                .WithTitle("Soup Arena | Арена")
                .WithThumbnailUrl(Paths.ArenaBattleIcon)
                .WithImageUrl(Field.ImageURL)
                .WithDescription('\n' +
                                 BattleMessageLog
                                    .Append("\n\n")
                                    .Append(YourMove ? YourMoveMessage : (WhoseMove == Enemy.Cell ? EnemysMoveMessage : string.Empty))
                                    .ToString());
            var ComponentBuilder = new ComponentBuilder();

            const string Placeholder = "Пусто.";

            var TimeLeft = MoveTime - Stopwatch.Elapsed;

            EmbedBuilder
                .AddField(new EmbedFieldBuilder()
                                    .WithName("Расходники:")
                                    .WithIsInline(true)
                                    .WithValue(Player
                                                    .Consumables
                                                    .Where(x => x.ConsumableItem.Amount > 0)
                                                    .ToString(x => x.ConsumableItem.ToString())
                                                    .IfNullOrEmpty(Placeholder)))
                .AddField(new EmbedFieldBuilder()
                                    .WithName("КД Расходники:")
                                    .WithIsInline(true)
                                    .WithValue(Player
                                                    .Consumables
                                                    .Where(x => x.MovesLeft > 0)
                                                    .ToString(x => x.ToString())
                                                    .IfNullOrEmpty(Placeholder)))
                .AddField(new EmbedFieldBuilder()
                                    .WithName("КД Скиллы:")
                                    .WithIsInline(true)
                                    .WithValue(Player.Class.FirstAbility.ToString() + '\n' +
                                               Player.Class.SecondAbility.ToString()))
                .AddField(new EmbedFieldBuilder()
                                    .WithName("Баффы:")
                                    .WithIsInline(true)
                                    .WithValue(Player
                                                    .Buffs
                                                    .Where(x => !x.IsDebuff)
                                                    .ToString(x => x.ToString())
                                                    .IfNullOrEmpty(Placeholder)))
                .AddField(new EmbedFieldBuilder()
                                    .WithName("Дебаффы:")
                                    .WithIsInline(true)
                                    .WithValue(Player
                                                    .Buffs
                                                    .Where(x => x.IsDebuff)
                                                    .ToString(x => $"{x} {x.MovesLeft} ход.")
                                                    .IfNullOrEmpty(Placeholder)))
                .AddField(new EmbedFieldBuilder()
                                    .WithName("Здоровье:")
                                    .WithIsInline(true)
                                    .WithValue("Моё: " + Player.ToString() + '\n' +
                                               "Врага: " + (Player == FirstPlayer ? SecondPlayer : FirstPlayer).ToString()))
                .AddField(new EmbedFieldBuilder()
                                    .WithName("Времени осталось:")
                                    .WithIsInline(false)
                                    .WithValue($":stopwatch: {TimeLeft.Minutes:D2}:{TimeLeft.Seconds:D2}"));

            for (int i = 0; i < Player.Consumables.Count; i++)
            {
                var Consumable = Player.Consumables[i];
                ComponentBuilder.WithButton(customId: i.ToString(), style: ButtonStyle.Success, emote: Emote.Parse(Consumable.ConsumableItem.Consumable.Emoji), row: 0,
                    disabled: BattleEnded || Consumable.MovesLeft != 0 || Consumable.ConsumableItem.Amount == 0 || !Consumable.ConsumableItem.Consumable.RequirmentsSatisfied(Player, Enemy, this) || !BattleRequirmentSatisfied(Player, (MoveType)i));
            }

            ComponentBuilder
                .WithButton(customId: ((int)MoveType.RightMove).ToString(), style: ButtonStyle.Primary, emote: Emote.Parse("<:1075657459798126612:1123210946001768520>"), row: 1,
                            disabled: !BattleRequirmentSatisfied(Player, MoveType.RightMove))
                .WithButton(customId: ((int)MoveType.FirstAbility).ToString(), style: ButtonStyle.Danger, emote: Emote.Parse(Player.Class.FirstAbility.Ability.Emoji), row: 1,
                            disabled: !BattleRequirmentSatisfied(Player, MoveType.FirstAbility))
                .WithButton(customId: ((int)MoveType.Attack).ToString(), style: ButtonStyle.Danger, emote: Emote.Parse("<:1075657277815660595:1123210932588384356>"), row: 1,
                            disabled: !BattleRequirmentSatisfied(Player, MoveType.Attack))
                .WithButton(customId: ((int)MoveType.SecondAbility).ToString(), style: ButtonStyle.Danger, emote: Emote.Parse(Player.Class.SecondAbility.Ability.Emoji), row: 1,
                            disabled: !BattleRequirmentSatisfied(Player, MoveType.SecondAbility))
                .WithButton(customId: ((int)MoveType.LeftMove).ToString(), style: ButtonStyle.Primary, emote: Emote.Parse("<:1075657490039046264:1123210947633356892>"), row: 1,
                            disabled: !BattleRequirmentSatisfied(Player, MoveType.LeftMove))

                .WithButton(customId: ((int)MoveType.EndMove).ToString(), style: ButtonStyle.Secondary, emote: Emote.Parse("<:1075658282020122695:1123210982399950909>"), row: 2,
                            disabled: !BattleRequirmentSatisfied(Player, MoveType.EndMove))
                .WithButton(customId: ((int)MoveType.Surrender).ToString(), style: ButtonStyle.Secondary, emote: Emote.Parse("<:1075658252798406836:1123211164076228718>"), row: 2,
                            disabled: !BattleRequirmentSatisfied(Player, MoveType.Surrender));

            return await Task.FromResult((EmbedBuilder.Build(), ComponentBuilder.Build()));
        }

        private bool BattleRequirmentSatisfied(BattlePlayer Player, MoveType Type)
        {
            bool IsStunned = Player.Buffs.Any(x => x.Type is BuffType.Stun);
            bool IsPlayersMove = Player.Cell == WhoseMove;

            BattlePlayer Enemy = Player == FirstPlayer ? SecondPlayer : FirstPlayer;

			if (BattleEnded || Player.PositionOnField is null || Enemy.PositionOnField is null)
				return false;

			int Distance = Math.Abs(Enemy.PositionOnField!.Value - Player.PositionOnField!.Value);

            switch (Type)
            {
                case MoveType.FirstConsumable:
                case MoveType.SecondConsumable:
                case MoveType.ThirdConsumable:
                case MoveType.FourthConsumable:
                case MoveType.FifthConsumable:
                    {
                        return !ConsumableUsed &&
                            ActionPerformsLeft != 0 &&
                            IsPlayersMove &&
                            (!IsStunned ||
                            Player.Consumables[(int)Type].ConsumableItem.ConsumableID == Consumable.Razvey.ID);
                    }
                case MoveType.RightMove:
                case MoveType.LeftMove:
                    {
                        int PositionOnField = Player.PositionOnField!.Value;
                        return ((!Moved && ActionPerformsLeft != 0) || AdditionalMovesLeft != 0)
                            && IsPlayersMove
                            && !IsStunned
                            && ((PositionOnField != 0 && Type == MoveType.LeftMove)
                            || (PositionOnField != Field.Length - 1 && Type == MoveType.RightMove));
                    }
                case MoveType.FirstAbility:
                case MoveType.SecondAbility:
                    {
                        var Ability = Type == MoveType.FirstAbility ? Player.Class.FirstAbility : Player.Class.SecondAbility;

                        return ActionPerformsLeft != 0 &&
                            !ActionPerformed
                            && IsPlayersMove
                            && !IsStunned
                            && Ability.MovesLeft == 0
                            && Distance <= Ability.Ability.Range;
                    }
                case MoveType.Attack:
                    {
                        return ActionPerformsLeft != 0 &&
                            !ActionPerformed &&
                            IsPlayersMove &&
                            Distance <= Player.Equipment.AttackRange &&
                            !IsStunned;
                    }
                case MoveType.EndMove:
                case MoveType.Surrender: return IsPlayersMove;
                default: return false;
            }
        }

        private async Task DeleteMessages()
        {
            while (true)
            {
                try
                {
                    if (FirstPlayer.Message is not null)
                        await FirstPlayer.Message.DeleteAsync();
					break;
				}
				catch
                {
                    await Task.Delay(2000);
                }
			}

            while (true)
            {
                try
                {
                    if (SecondPlayer.Message is not null)
                        await SecondPlayer.Message.DeleteAsync();
                    break;
                }
                catch
                {
                    await Task.Delay(2000);
                }
            }
        }
    }
    public enum MoveType
    {
        FirstConsumable,
        SecondConsumable,
        ThirdConsumable,
        FourthConsumable,
        FifthConsumable,

        RightMove,

        FirstAbility,
        Attack,
        SecondAbility,

        LeftMove,

        EndMove,
        Surrender,
        Start,
        TimeoutMove
    }
}
