using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xunit;

namespace ClickOnceGet.Test
{
    public class IconTest
    {
        private string PathOf(string fileName) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", fileName);

        [Fact]
        public void Icon_ctor_from_Stream_Test()
        {
            using var fs = new FileStream(PathOf("z.ico"), FileMode.Open);
            var icon = new Icon(fs, 128, 128);

            var iconBmp = icon.ToBitmap();
            iconBmp.Save(PathOf("z-from-ico.bmp"), ImageFormat.Bmp);

            var bmpZBytes = File.ReadAllBytes(PathOf("z.bmp"));
            var bmpZfromIcoBytes = File.ReadAllBytes(PathOf("z-from-ico.bmp"));
            bmpZfromIcoBytes.Is(bmpZBytes);
        }
    }
}
