using AuroraLib.Pixel.Formats.Common;
using AuroraLib.Pixel.PixelFormats;

namespace PixelFormatTest
{
    [TestClass]
    public sealed class PNGTest
    {
        [TestMethod]
        public void RGBA32()
        {
            using var image = TestImage.Create<RGBA<byte>>();
            TestImage.TestFormat(new PNG(), image);
        }

        [TestMethod]
        public void RGB24()
        {
            using var image = TestImage.Create<RGB<byte>>();
            TestImage.TestFormat(new PNG(), image);
        }

        [TestMethod]
        public void RGBA64()
        {
            using var image = TestImage.Create<RGBA<ushort>>();
            TestImage.TestFormat(new PNG(), image);
        }

        [TestMethod]
        public void RGB48()
        {
            using var image = TestImage.Create<RGB<ushort>>();
            TestImage.TestFormat(new PNG(), image);
        }

        [TestMethod]
        public void I4()
        {
            using var image = TestImage.Create<I4>();
            TestImage.TestFormat(new PNG(), image);
        }

        [TestMethod]
        public void I8()
        {
            using var image = TestImage.Create<I<byte>>();
            TestImage.TestFormat(new PNG(), image);
        }

        [TestMethod]
        public void IA16()
        {
            using var image = TestImage.Create<IA<byte>>();
            TestImage.TestFormat(new PNG(), image);
        }

        [TestMethod]
        public void I16()
        {
            using var image = TestImage.Create<I<ushort>>();
            TestImage.TestFormat(new PNG(), image);
        }

        [TestMethod]
        public void IA32()
        {
            using var image = TestImage.Create<IA<ushort>>();
            TestImage.TestFormat(new PNG(), image);
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
