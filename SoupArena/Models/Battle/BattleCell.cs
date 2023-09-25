using C3.SmartEnums;

namespace SoupArena.Models.Battle
{
    //public enum BattleCell
    //{
    //    None = 0,
    //    Красный = 1,
    //    Зелёный = 2,
    //    RedAndGreen = 3
    //}
	public record struct BattleCell : ISmartEnum<byte>
	{
		public byte ID { get; init; }
		public string Name { get; init; }

		public string? Emoji { get; set; }

        public readonly static BattleCell None = new()
		{
            ID = 0,
            Name = nameof(None)
        };

        public readonly static BattleCell Red = new()
		{
            ID = 1,
            Name = "Красный",
            Emoji = "<:1091424768449851527:1123210988670419005>"
        };

        public readonly static BattleCell Green = new()
		{
            ID = 2,
            Name = "Зелёный",
            Emoji = "<:1091424771792715816:1123210991841312829>"
        };

        public readonly static BattleCell RedAndGreen = new()
        {
            ID = 3,
            Name = nameof(RedAndGreen),
            Emoji = "<:1091424771792715816:1123210991841312829>"
        };

        public override string ToString() => Name;
	}
	public static class BattleCellExtentions
    {
        public static BattleCell? Invert(this BattleCell Cell) => Cell == BattleCell.Green ? BattleCell.Red : Cell != BattleCell.Red ? null : BattleCell.Green;
    }
    public enum Direction
    {
        Left = -1,
        Right = 1
    }
}
