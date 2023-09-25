using C3.SmartEnums;
using SoupArena.Models.SmartEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoupArena.Models.Player.DB
{
    public sealed class DBInventoryConsumable
    {
        [Key]
        public long ID { get; init; }
        public uint Amount { get; set; }
        public short ConsumableID { get; init; }
        [NotMapped]
        public Consumable Consumable => SmartEnumExtentions.GetByID<Consumable, short>(ConsumableID)!.Value;

        public long InventoryID { get; init; }
        [ForeignKey(nameof(InventoryID))]
        public DBInventory? Inventory { get; init; }

        public DBInventoryConsumable CloneToBattleConsumable() => new() { ID = -1, Amount = Amount, ConsumableID = ConsumableID, InventoryID = -1 };
        public override string ToString() => $"{Consumable.Emoji} {Amount} шт.";
    }
}
