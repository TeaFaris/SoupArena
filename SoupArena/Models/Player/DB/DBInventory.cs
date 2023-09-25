using System.ComponentModel.DataAnnotations;

namespace SoupArena.Models.Player.DB
{
    public class DBInventory
    {
        [Key]
        public long ID { get; init; }

        public List<DBInventoryConsumable> Items { get; set; } = null!;

        public static DBInventory operator -(DBInventory Left, DBInventory Right)
        {
            Left
                .Items
                .ForEach(x => x.Amount -= Right
                                            .Items
                                            .Find(y => y.ConsumableID == x.ConsumableID)!
                                            .Amount);
            return Left;
        }

        public override string ToString() => /*if*/ Items.Count > 0 ?
                                                /*{*/
                                                    string.Join
                                                        (
                                                            '\n',
                                                            Items
                                                                .Where(x => x.Amount > 0)
                                                                .Select(x => $"{x.Consumable.Name}: {x.Amount} шт.")
                                                        )
                                                /*}*/
                                             /*else*/:
                                                    string.Empty;

    }
}
