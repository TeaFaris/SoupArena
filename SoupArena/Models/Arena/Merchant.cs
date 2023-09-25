using C3.SmartEnums;
using SoupArena.Models.SmartEnums;

namespace SoupArena.Models.Arena
{
    public readonly record struct Merchant : ISmartEnum<byte>
    {
        public required byte ID { get; init; }
        public required string Name { get; init; }

        public required string Dialogue { get; init; }
        public required string ImageURL { get; init; }
        public required string InventoryImageURL { get; init; }
        public required Dictionary<Consumable, ulong> Products { get; init; }

        public readonly static Merchant Basic = new()
        {
            ID = 0,
            Name = "Торговец",
            Dialogue = "Ассортимент пока не велик в будущем Вас ждут новые товары.",
            ImageURL = "https://i.imgur.com/j2fwzKW.png",
            InventoryImageURL = "https://i.imgur.com/tl4V4wq.png",
            Products = new Dictionary<Consumable, ulong>()
            {
                { Consumable.Bondage, 5 },
                { Consumable.Hodgepodge, 20 },
                { Consumable.Speed, 10 },
                { Consumable.Razvey, 25 },
                { Consumable.ThrowableHammer, 15 }
            }
        };
    }
}
