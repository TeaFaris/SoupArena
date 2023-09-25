using C3.SmartEnums;
using SoupArena.Models.Battle;
using SoupArena.Models.Battle.Entities;
using System.Text;

namespace SoupArena.Models.SmartEnums.Classes
{
    public readonly record struct Ability : ISmartEnum<ushort>
    {
        public required ushort ID { get; init; }
        public required string Name { get; init; }

        public required string Description { get; init; }
        public required uint Cooldown { get; init; }
        public required decimal StartChance { get; init; }
        public required decimal ChanceDistanceDecreasing { get; init; }
        public required uint Range { get; init; }
        public required string UseMessageFormat { get; init; }
        public required string Emoji { get; init; }
        public required Func<BattlePlayer, BattlePlayer, BattleManager, Task<uint>> Use { get; init; }

        public string GetChancesString()
        {
            var SB = new StringBuilder();
            for (int i = 0; i < Range + 1; i++)
                SB.Append(i == 0 ? "Рядом" : $"{i} шаг").Append(" - ").Append(StartChance - (ChanceDistanceDecreasing * i)).Append('%').AppendLine();
            return SB.ToString();
        }

        public readonly static Ability Jump = new()
        {
            ID = 0,
            Name = "Прыжок",
            Cooldown = 4,
            StartChance = 80,
            ChanceDistanceDecreasing = 10,
            Range = 5,
            Description = """
                          прыгает на противника.
                          Наносит 10 урона и оглушает на 2 хода.
                          """,
            UseMessageFormat = """
                               Нанёс {0} урона.
                               Оглушил на 2 хода.
                               """,
            Emoji = "<:1075658156014833664:1123210971519909899>",
            Use = async (Player, Enemy, Battle) =>
            {
                await Battle.Field.Move(Player, Enemy.PositionOnField!.Value - Player.PositionOnField!.Value);
                uint Damage = 10;
                Enemy.GiveDamage(ref Damage, Player);
                Enemy.AddBuff(new BattleBuff()
                {
                    Type = BuffType.Stun,
                    IsDebuff = true,
                    MovesLeft = 2,
                    StringFormat = Paths.StunEmoji
                });
                return Damage;
            }
        };
        public readonly static Ability StunningWall = new()
        {
            ID = 1,
            Name = "Оглушающий заслон",
            Cooldown = 4,
            StartChance = 80,
            ChanceDistanceDecreasing = 10,
            Range = 2,
            Description = """
                          оглушает противника на 2 хода.
                          """,
            UseMessageFormat = """
                               Оглушил врага на 2 хода.
                               """,
            Emoji = "<:1075658191259586611:1123210974741135400>",
            Use = async (Player, Enemy, Battle) =>
            {
                Enemy.AddBuff(new BattleBuff()
                {
                    Type = BuffType.Stun,
                    IsDebuff = true,
                    MovesLeft = 2,
                    StringFormat = Paths.StunEmoji
                });

                return await Task.FromResult(0u);
            }
        };

        public readonly static Ability Hook = new()
        {
            ID = 2,
            Name = "Крюк",
            Cooldown = 4,
            StartChance = 80,
            ChanceDistanceDecreasing = 10,
            Range = 5,
            Description = """
                          притягивает противника к себе.
                          Наносит 30 урона и оглушает на 1 ход.
                          """,
            UseMessageFormat = """
                               Притянул противника к себе.
                               Нанёс {0} урона и оглушил на 1 ход.
                               """,
            Emoji = "<:1075658041300627476:1123210966235103242>",
            Use = async (Player, Enemy, Battle) =>
            {
                await Battle.Field.Move(Enemy, Player.PositionOnField!.Value - Enemy.PositionOnField!.Value);
                uint Damage = 30;
                Enemy.GiveDamage(ref Damage, Player);
                Enemy.AddBuff(new BattleBuff()
                {
                    Type = BuffType.Stun,
                    IsDebuff = true,
                    MovesLeft = 1,
                    StringFormat = Paths.StunEmoji
                });
                return Damage;
            }
        };
        public readonly static Ability WallOfShields = new()
        {
            ID = 3,
            Name = "Стена щитов",
            Cooldown = 4,
            StartChance = 80,
            ChanceDistanceDecreasing = 10,
            Range = 2,
            Description = """
                          отталкивает противника на 3 шага и наносит 20 урона.
                          """,
            UseMessageFormat = """
                               Оттолкнул противника на 3 шага.
                               Нанёс {0} урона
                               """,
            Emoji = "<:1075658104231952394:1123210969330483231>",
            Use = async (Player, Enemy, Battle) =>
            {
                if(await Battle.Field.Move(Enemy, 3)) { }
                else if(await Battle.Field.Move(Enemy, -3)) { }

                uint Damage = 20;
                Enemy.GiveDamage(ref Damage, Player);
                return Damage;
            }
        };
    }
}
