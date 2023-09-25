using C3.SmartEnums;
using SoupArena.Models.Battle;
using SoupArena.Models.Battle.Entities;

namespace SoupArena.Models.SmartEnums
{
    public readonly record struct Consumable : ISmartEnum<short>
    {
        public required short ID { get; init; }
        public required string Name { get; init; }

        public required string Description { get; init; }
        public required uint Cooldown { get; init; }
        public required uint UseDuration { get; init; }
        public required string ImageURL { get; init; }
        public required string Emoji { get; init; }
        public required string UseMessageFormat { get; init; }
        public required Func<BattlePlayer, BattlePlayer, BattleManager, bool> RequirmentsSatisfied { get; init; }
        public required Func<BattlePlayer, BattlePlayer, BattleManager, uint> Use { get; init; }

        public readonly static Consumable Bondage = new()
        {
            ID = 0,
            Name = "Бинт",
            Description = """
                          +10 здоровья
                          """,
            UseMessageFormat = """
                               Востановлено +10 здоровья
                               """,
            Cooldown = 1,
            UseDuration = 0,
            ImageURL = "https://i.imgur.com/rHB0qKL.png",
            Emoji = "<:1075657523811594291:1123210951081078784>",
            RequirmentsSatisfied = (Player, _, _) => Player.Health < Player.MaxHealth,
            Use = (Player, Enemy, Battle) =>
            {
                uint Heal = 10;
                Player.Heal(ref Heal, Player);
                return 0;
            }
        };
        public readonly static Consumable Hodgepodge = new()
        {
            ID = 1,
            Name = "Солянка",
            Description = """
                          +10 урон +10 броня
                          """,
            UseMessageFormat = """
                               Урон и броня +10
                               """,
            Cooldown = 5,
            UseDuration = 3,
            ImageURL = "https://i.imgur.com/J3MN00Z.png",
            Emoji = "<:1075657847691542570:1123210954109354104>",
            RequirmentsSatisfied = (_, _, _) => true,
            Use = (Player, Enemy, Battle) =>
            {
                BattleBuff Buff = new BattleBuff()
                {
                    Type = BuffType.Default,
                    IsDebuff = false,
                    MovesLeft = Hodgepodge.UseDuration,
                    StringFormat = "+10 <:1076221963967680634:1123210986132869224>"
                };

                Buff.OnSpell += (_, Player) =>
                {
                    Player.DamageBoost += 10;
                    Player.Defence += 10;
                };
                Buff.OnDispell += (_, Player) =>
                {
                    Player.DamageBoost -= 10;
                    Player.Defence -= 10;
                };

                Player.AddBuff(Buff);

                return 0;
            }
        };
        public readonly static Consumable Speed = new()
        {
            ID = 2,
            Name = "Скорость",
            Description = """
                          +3 шага
                          """,
            UseMessageFormat = """
                               +3 шага. Выбирай с умом, куда пойти!
                               """,
            Cooldown = 3,
            UseDuration = 0,
            ImageURL = "https://i.imgur.com/kXswUwK.png",
            Emoji = "<:1075657941316804638:1123210956911149119>",
            RequirmentsSatisfied = (_, _, _) => true,
            Use = (Player, Enemy, Battle) =>
            {
                Battle.AdditionalMovesLeft += 3;
                return 0;
            }
        };
        public readonly static Consumable Razvey = new()
        {
            ID = 3,
            Name = "Развей",
            Description = """
                          Сброс всех негативных эффектов
                          """,
            UseMessageFormat = """
                               Все негативные эффекты сняты
                               """,
            Cooldown = 2,
            UseDuration = 0,
            ImageURL = "https://i.imgur.com/BDp9z12.png",
            Emoji = "<:1075657979996684298:1123210960853803178>",
            RequirmentsSatisfied = (Player, _, _) => Player.Buffs.Any(x => x.IsDebuff),
            Use = (Player, Enemy, Battle) =>
            {
                Player.Buffs.RemoveAll(x => x.IsDebuff);
                return 0;
            }
        };
        public readonly static Consumable ThrowableHammer = new()
        {
            ID = 4,
            Name = "Метательный молот",
            Description = """
                          Оглушение врага +10 урона
                          Шанс попадания: 
                          
                          Рядом - 80%
                          1 шаг - 70%
                          2 шаг - 60%
                          3 шаг - 50%
                          4 шаг - 40%
                          5 шаг - 30%
                          """,
            UseMessageFormat = string.Empty,
            Cooldown = 5,
            UseDuration = 2,
            ImageURL = "https://i.imgur.com/TzKnOqd.png",
            Emoji = "<:1075658005359640656:1123210962812551218>",
            RequirmentsSatisfied = (Player, Enemy, _) => Math.Abs(Player.PositionOnField!.Value - Enemy.PositionOnField!.Value) <= 5,
            Use = (Player, Enemy, Battle) =>
            {
                if (Player.PositionOnField is null || Enemy.PositionOnField is null)
                    return 0;

                decimal HitChance = 80 - (Math.Abs(Player.PositionOnField!.Value - Enemy.PositionOnField!.Value) * 10);

                int RandomNumber = Random.Shared.Next(0, 100);

                if (RandomNumber < HitChance)
                {
                    uint Damage = 10;
                    Enemy.GiveDamage(ref Damage, Player);

                    Enemy.AddBuff(new BattleBuff()
                    {
                        Type = BuffType.Stun,
                        IsDebuff = true,
                        MovesLeft = ThrowableHammer.UseDuration,
                        StringFormat = Paths.StunEmoji
                    });

                    Battle.BattleMessageLog
                        .Append(" и попал!\n")
                        .AppendFormat("Нанёс {0} урона и оглушил на 2 хода.", Damage)
                        .Append("\n\nШанс попадания: ")
                        .Append(HitChance)
                        .Append("%\nВыпавший процент: ")
                        .Append(RandomNumber)
                        .Append("% (Выпавший процент должен быть меньше шанса попадания чтобы попасть во врага)");
                    return Damage;
                }
                else
                {
                    Battle.BattleMessageLog
                        .Append(" и промазал!")
                        .Append("\n\nШанс попадания: ")
                        .Append(HitChance)
                        .Append("%\nВыпавший процент: ")
                        .Append(RandomNumber)
                        .Append("% (Выпавший процент должен быть меньше шанса попадания чтобы попасть во врага)");
                    return 0;
                }
            }
    };
    }
}
