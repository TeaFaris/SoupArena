using SoupArena.Models.Player.DB;
using System.Text;

namespace SoupArena.Models.Battle.Entities
{
    public sealed class BattleConsumable : BattleEntity
    {
        public required DBInventoryConsumable ConsumableItem { get; init; }

        public override async Task Move(BattlePlayer Player, BattlePlayer Enemy, BattleManager Battle, StringBuilder LogInfo)
        {
            if (ConsumableItem.Amount == 0 || MovesLeft != 0)
                return;

            ConsumableItem.Amount--;

            MovesLeft = ConsumableItem.Consumable.Cooldown;

            Battle.BattleMessageLog
                    .Append("Использовал: ")
                    .Append(ConsumableItem.Consumable.Name)
                    .Append('\n')
                    .Append(ConsumableItem.Consumable.UseMessageFormat);

            ConsumableItem.Consumable.Use(Player, Enemy, Battle);

            await Task.CompletedTask;
        }

        public override string ToString() => $"{ConsumableItem.Consumable.Emoji} {MovesLeft} ход.";
    }
}
