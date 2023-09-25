using SoupArena.Models.SmartEnums.Classes;
using System.Text;

namespace SoupArena.Models.Battle.Entities
{
    public class BattleAbility : BattleEntity
    {
        public required Ability Ability { get; init; }
        public override async Task Move(BattlePlayer Player, BattlePlayer Enemy, BattleManager Battle, StringBuilder LogInfo)
        {
            if (MovesLeft != 0)
                return;

            MovesLeft = Ability.Cooldown;

            decimal HitChance = Ability.StartChance - (Math.Abs(Player.PositionOnField!.Value - Enemy.PositionOnField!.Value) * Ability.ChanceDistanceDecreasing);

            int RandomNumber = Random.Shared.Next(0, 100);

            if (RandomNumber < HitChance)
            {
                uint Damage = await Ability.Use(Player, Enemy, Battle);
                LogInfo
                    .Append("Использовал: ")
                    .Append(Ability.Name)
                    .Append(" и попал!\n")
                    .AppendFormat(Ability.UseMessageFormat, Damage)
                    .Append("\n\nШанс попадания: ")
                    .Append(HitChance)
                    .Append("%\nВыпавший процент: ")
                    .Append(RandomNumber)
                    .Append("% (Выпавший процент должен быть меньше шанса попадания чтобы попасть во врага)");
            }
            else
            {
                LogInfo
                    .Append("Использовал: ")
                    .Append(Ability.Name)
                    .Append(" и промазал!")
                    .Append("\n\nШанс попадания: ")
                    .Append(HitChance)
                    .Append("%\nВыпавший процент: ")
                    .Append(RandomNumber)
                    .Append("% (Выпавший процент должен быть меньше шанса попадания чтобы попасть во врага)");
            }
        }

        public override string ToString() => $"{Ability.Emoji} {MovesLeft} ход.";
    }
}
