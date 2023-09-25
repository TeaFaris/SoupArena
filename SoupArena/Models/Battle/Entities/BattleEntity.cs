using System.Text;

namespace SoupArena.Models.Battle.Entities
{
    public abstract class BattleEntity
    {
        public required uint MovesLeft { get; set; }

        public abstract Task Move(BattlePlayer Player, BattlePlayer Enemy, BattleManager Battle, StringBuilder LogInfo);
    }
}
