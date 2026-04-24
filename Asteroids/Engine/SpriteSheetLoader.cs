using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Asteroids.Engine;

public static class SpriteSheetLoader
{
    public static BitmapSource ConvertBitmap(System.Drawing.Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public static CroppedBitmap[] Slice(BitmapSource sheet, int columns, int rows)
    {
        int frameWidth = sheet.PixelWidth / columns;
        int frameHeight = sheet.PixelHeight / rows;
        var frames = new List<CroppedBitmap>(columns * rows);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                var sourceRect = new Int32Rect(col * frameWidth, row * frameHeight, frameWidth, frameHeight);
                var frame = new CroppedBitmap(sheet, sourceRect);
                frame.Freeze();
                frames.Add(frame);
            }
        }

        return frames.ToArray();
    }
}
