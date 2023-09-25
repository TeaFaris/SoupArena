using C3.SmartEnums;

namespace SoupArena.Models.SmartEnums.Classes
{
    public readonly record struct Class : ISmartEnum<byte>
    {
        public required byte ID { get; init; }
        public required string Name { get; init; }

        public required uint Health { get; init; }
        public required Ability FirstAbility { get; init; }
        public required Ability SecondAbility { get; init; }

        public readonly static Class Bully = new()
        {
            ID = 0,
            Name = "Громила",
            Health = 200,
            FirstAbility = Ability.Jump,
            SecondAbility = Ability.StunningWall
        };
        public readonly static Class Duelant = new()
        {
            ID = 1,
            Name = "Дуэлянт",
            Health = 200,
            FirstAbility = Ability.Hook,
            SecondAbility = Ability.WallOfShields
        };
    }
}
