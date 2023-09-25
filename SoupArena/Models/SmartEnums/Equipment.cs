using C3.SmartEnums;

namespace SoupArena.Models.SmartEnums
{
    public readonly record struct Equipment : ISmartEnum<byte>
    {
        public required byte ID { get; init; }
        public required string Name { get; init; }

        public required uint Defence { get; init; }
        public required uint Damage { get; init; }
        public required string ArenaEnteranceImagePath { get; init; }
        public required string ImageURL { get; init; }
        public required uint AttackRange { get; init; }

        public readonly static Equipment HeavyArmourAndDragonSword = new()
        {
            ID = 0,
            Name = "Тяжёлая броня и Драконий меч",
            Damage = 20,
            Defence = 0,
            AttackRange = 1,
            ArenaEnteranceImagePath = $"Images/ArenaEnterance/{nameof(HeavyArmourAndDragonSword)}.png",
            ImageURL = "https://i.imgur.com/UjmZebP.png"
        };
    }
}
