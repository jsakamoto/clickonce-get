using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Xunit;

namespace ClickOnceGet.Test.FromMono.System.Drawing
{
    public class IconTest
    {
        private string PathOf(string fileName) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", fileName);

        [Fact]
        public void Icon_ctor_from_Stream_Test()
        {
            using (var fs = new FileStream(PathOf("z.ico"), FileMode.Open))
            {
                var icon = new global::FromMono.System.Drawing.Icon(fs, 128, 128);

                var iconBmp = icon.ToBitmap();
                iconBmp.Save(@"C:\tmp\xyz.bmp", ImageFormat.Bmp);
            }
        }
    }
}
