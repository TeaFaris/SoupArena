using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace SoupArena.ImageGenerators
{
    public static class InventoryGridImageGenerator
    {
        [SupportedOSPlatform("windows")]
        private static Stream FillGridAndGetImageStream(Bitmap GridImage, string[] Values, Point StartPos, Point Delta, uint GridWidth = 0)
        {
            Point Location = StartPos;

            using Graphics Graphics = Graphics.FromImage(GridImage);
            using Font Font = new("Arial", 7);

            for (int i = 0; i < Values.Length; i++)
            {
                string Value = Values[i];

                var AlignedPosition = new Point(Location.X - ((Value.Length - 1) * 7), Location.Y);
                Graphics.DrawString(Value, Font, Brushes.White, AlignedPosition);
                if((i + 1) % GridWidth == 0)
                {
                    Location.X = Delta.X;
                    Location.Y += Delta.Y;
                }
                else
                {
                    Location.X += Delta.X;
                }
            }

            var MS = new MemoryStream();
            Graphics.DrawImage(GridImage, 0, 0, GridImage.Width, GridImage.Height);
            GridImage.Save(MS, ImageFormat.Png);
            GridImage.Dispose();

            return MS;
        }

        [SupportedOSPlatform("windows")]
        public static async Task<Stream> FillGridAndGetImageStreamAsync(string GridFilePath, string[] Values, Point StartPos, Point Delta, uint GridWidth)
        {
            return await Task.FromResult(FillGridAndGetImageStream(new Bitmap(GridFilePath), Values, StartPos, Delta, GridWidth));
        }
    }
}
