using System.Text;

namespace SoupArena.Models.Battle.Entities
{
    public sealed class BattleBuff : BattleEntity
    {
        public required BuffType Type { get; init; }
        public required bool IsDebuff { get; init; }
        public required string StringFormat { get; init; }
        public event EventHandler<BattlePlayer> OnSpell = delegate { };
        public event EventHandler<BattlePlayer> OnDispell = delegate { };

        public override async Task Move(BattlePlayer Player, BattlePlayer Enemy, BattleManager Battle, StringBuilder LogInfo)
        {
            MovesLeft--;

            if (MovesLeft == 0)
            {
                OnDispell.Invoke(null, Player);
                Player.Buffs.Remove(this);
            }

            await Task.CompletedTask;
        }

        public void OnSpellInvoke(object? sender, BattlePlayer Player) => OnSpell.Invoke(sender, Player);

        public override string ToString() => StringFormat;
    }
    public enum BuffType
    {
        Default = 0,
        Stun = 1,
        Hodgepodge = 2
    }
}
