using AuroraLib.Pixel.Formats.Common;
using AuroraLib.Pixel.PixelFormats;
using AuroraPixel.ImageFormats;

namespace PixelFormatTest
{
    [TestClass]
    public sealed class BMPTest
    {
        [TestMethod]
        public void RGB555()
        {
            using var image = TestImage.Create<RGB555>();
            TestImage.TestFormat(new BMP(), image);
        }

        [TestMethod]
        public void RGBA24()
        {
            using var image = TestImage.Create<RGB<byte>>();
            TestImage.TestFormat(new BMP(), image);
        }

        [TestMethod]
        public void RGBA32()
        {
            using var image = TestImage.Create<RGBA<byte>>();
            TestImage.TestFormat(new BMP(), image);
        }

        [TestMethod]
        public void Bitfield16bit_ARGB1555()
        {
            using var image = TestImage.Create<ARGB1555>();
            TestImage.TestFormat(new BMP(), image);
        }

        [TestMethod]
        public void Bitfield32bit_BGRA1010102()
        {
            using var image = TestImage.Create<BGRA1010102>();
            TestImage.TestFormat(new BMP(), image);
        }

        [TestMethod]
        public void Palette_RGB24()
        {
            using var image = TestImage.CreatePalette<I<byte>, RGB<byte>>();
            TestImage.TestFormat(new PNG(), image);
        }

        [TestMethod]
        public void Palette_RGBA32()
        {
            using var image = TestImage.CreatePalette<I<byte>, RGBA<byte>>();
            TestImage.TestFormat(new PNG(), image);
        }

    }
}
