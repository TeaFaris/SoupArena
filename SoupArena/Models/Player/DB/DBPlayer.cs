using C3.SmartEnums;
using SoupArena.Models.SmartEnums;
using SoupArena.Models.SmartEnums.Classes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoupArena.Models.Player.DB
{
    public class DBPlayer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public required ulong DiscordID { get; init; }
        public ulong Silver { get; set; }
        public ulong Wins { get; set; }
        public ulong Loses { get; set; }
        public byte? EquipmentID { get; set; }
        [NotMapped]
        public Equipment? Equipment => SmartEnumExtentions.GetByID<Equipment, byte>(EquipmentID ?? byte.MaxValue);
        public byte? ClassID { get; set; }
        [NotMapped]
        public Class? Class => SmartEnumExtentions.GetByID<Class, byte>(ClassID ?? byte.MaxValue);

        public DateTime? LastTimeClaimedBonus { get; set; }

        public long ConsumablesEquipedInventoryID { get; init; }
        [ForeignKey(nameof(ConsumablesEquipedInventoryID))]
        public required DBInventory ConsumablesEquiped { get; set; }

        public long ConsumablesInventoryID { get; init; }
        [ForeignKey(nameof(ConsumablesInventoryID))]
        public required DBInventory Consumables { get; set; }
    }
}
