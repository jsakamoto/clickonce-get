using System;
using System.IO;
using System.Linq;
using ClickOnceGet.Server.Services;
using Xunit;

namespace ClickOnceGet.Test
{
    public class ClickOnceAppContentManagerTest
    {
        private string PathOf(string fileName) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", fileName);

        [Fact]
        public void GetIconSizes_Test()
        {
            using var iconStream = File.OpenRead(PathOf("z (16, 32, 48, 64, 128, 256px).ico"));
            var sizes = ClickOnceAppContentManager.GetIconSizes(iconStream);
            sizes
                .OrderBy(sz => sz.Width)
                .Select(sz => $"{sz.Width} x {sz.Height}")
                .Is("16 x 16",
                    "32 x 32",
                    "48 x 48",
                    "64 x 64",
                    "128 x 128",
                    "256 x 256");
        }

        [Theory]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(48)]
        [InlineData(64)]
        [InlineData(128)]
        [InlineData(256)]
        public void ConvertIconToPng_Test(int pxSize)
        {
            using var iconStream = File.OpenRead(PathOf("z (16, 32, 48, 64, 128, 256px).ico"));
            using var pngStream = new MemoryStream();
            ClickOnceAppContentManager.ConvertIconToPng(iconStream, pngStream, pxSize);
            var actualPngBytes = pngStream.ToArray();

            var expectedImagePath = $"z.{pxSize}.png";
            var expectedPngBytes = File.ReadAllBytes(PathOf(expectedImagePath));

            actualPngBytes.Is(expectedPngBytes);
        }

        [Theory]
        [InlineData(48)]
        [InlineData(256)]
        public void ConvertToPng_with_Scaling_Test(int pxSize)
        {
            using var iconStream = File.OpenRead(PathOf("x (16, 32, 64px).ico"));
            using var pngStream = new MemoryStream();
            ClickOnceAppContentManager.ConvertIconToPng(iconStream, pngStream, pxSize);
            var actualPngBytes = pngStream.ToArray();

            var expectedImagePath = $"x.{pxSize}.png";
            var expectedPngBytes = File.ReadAllBytes(PathOf(expectedImagePath));
            actualPngBytes.Is(expectedPngBytes);
        }
    }
}
