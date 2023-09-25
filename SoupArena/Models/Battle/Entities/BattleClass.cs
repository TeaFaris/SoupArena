using SoupArena.Models.SmartEnums.Classes;
using System.Text;

namespace SoupArena.Models.Battle.Entities
{
    public sealed class BattleClass : BattleEntity
    {
        public Class Class { get; init; }
        public BattleAbility FirstAbility { get; init; }
        public BattleAbility SecondAbility { get; init; }

        public BattleClass(Class Class)
        {
            this.Class = Class;

            FirstAbility = new BattleAbility() { Ability = Class.FirstAbility, MovesLeft = 0 };
            SecondAbility = new BattleAbility() { Ability = Class.SecondAbility, MovesLeft = 0 };
        }

        public override async Task Move(BattlePlayer Player, BattlePlayer Enemy, BattleManager Battle, StringBuilder LogInfo)
        {
            if (FirstAbility.MovesLeft != 0)
                FirstAbility.MovesLeft--;
            if (SecondAbility.MovesLeft != 0)
                SecondAbility.MovesLeft--;

            await Task.CompletedTask;
        }
    }
}
