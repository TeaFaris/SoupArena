using Discord;
using SoupArena.Discord;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;
using Image = System.Drawing.Image;

namespace SoupArena.Models.Battle
{
    public sealed class BattleField
    {
        private const string FieldImagePath = "Images/Battle/Field.png";
        private const string GreenPlayerImagePath = "Images/Battle/Green.png";
        private const string RedPlayerImagePath = "Images/Battle/Red.png";
        private const string GreenAndRedPlayerImagePath = "Images/Battle/GreenAndRed.png";

        public string ImageURL { get; private set; }
        public int Length => Field.Length;
        private readonly BattleCell[] Field = new BattleCell[10];

        public BattleField()
        {
            Field[0] = BattleCell.Green;

            for (int i = 1; i < Field.Length - 1; i++)
                Field[i] = BattleCell.None;

            Field[^1] = BattleCell.Red;
        }

        public async Task<bool> Move(BattlePlayer Player, int Direction)
        {
			int CellPosition = Player.PositionOnField!.Value;

            int Destination = CellPosition + Direction;
            if (Destination < 0 || Destination >= Field.Length)
                return false;

            Field[CellPosition] = Field[CellPosition] == BattleCell.RedAndGreen ? Player.Cell.Invert()!.Value : BattleCell.None;
            Field[Destination] = Field[Destination] != BattleCell.None ? BattleCell.RedAndGreen : Player.Cell;

            Stream ImageStream = await GetImageStream();
#if DEBUG
            var SW = Stopwatch.StartNew();
#endif

            var Msg = await DiscordBot
                .Instance
                .Client!
                .GetGuild(1123201415137992744)
                .GetTextChannel(1123201917946957954)
                .SendFileAsync(new FileAttachment(ImageStream, $"{Guid.NewGuid()}.png"), string.Empty);

#if DEBUG
            SW.Stop();
            Console.WriteLine($"Field image sended in {SW.ElapsedMilliseconds} ms.");
#endif

            ImageURL = Msg.Attachments.ElementAt(0).Url;
            return true;
        }

        public int GetPosition(BattlePlayer Player)
        {
            int Position = Array.IndexOf(Field, BattleCell.RedAndGreen);
            return Position == -1 ? Array.IndexOf(Field, Player.Cell) : Position;
        }

        [SupportedOSPlatform("windows")]
        private async Task<Stream> GetImageStream()
        {
#if DEBUG
            var SW = Stopwatch.StartNew();
#endif

            using Image OgImage = Image.FromFile(FieldImagePath);
            using Graphics Graph = Graphics.FromImage(OgImage);

            Point StartPoint = new(40, 10);
            const int DeltaX = 229;

            var GreenPlayerImage = Image.FromFile(GreenPlayerImagePath);
            var RedPlayerImage = Image.FromFile(RedPlayerImagePath);
            var GreenAndRedPlayerImage = Image.FromFile(GreenAndRedPlayerImagePath);
            Size IconSize = new(146, 203);

            for (int i = 0; i < Field.Length; i++)
            {
                var Cell = Field[i];

                Point DrawPoint = StartPoint;
                DrawPoint.X += DeltaX * i;

                if (Cell == BattleCell.None)
                    continue;

                Rectangle DrawRect = new(DrawPoint, IconSize);

                // TODO: Change ImagePathes to property in BattleCell
                Graph.DrawImage(Cell switch
                {
                    var T when T == BattleCell.Green => GreenPlayerImage,
                    var T when T == BattleCell.Red => RedPlayerImage,
                    var T when T == BattleCell.RedAndGreen => GreenAndRedPlayerImage,
                    _ => throw new NotImplementedException()
                }, DrawRect);
            }

            var MemoryStream = new MemoryStream();
            OgImage.Save(MemoryStream, System.Drawing.Imaging.ImageFormat.Png);

#if DEBUG
            SW.Stop();
            Console.WriteLine($"Field image generated in {SW.ElapsedMilliseconds} ms.");
#endif

            return await Task.FromResult(MemoryStream);
        }
    }
}
