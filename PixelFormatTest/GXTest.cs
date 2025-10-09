using AuroraLib.Pixel.Formats;
using AuroraLib.Pixel.Formats.Dolphin;
using AuroraLib.Pixel.Formats.Dolphin.BlockProcessor;
using AuroraLib.Pixel.Formats.Dolphin.PixelFormats;
using AuroraLib.Pixel.PixelFormats;
using AuroraLib.Pixel.Texture;

namespace PixelFormatTest
{
    [TestClass]
    public sealed class GXTest
    {

        public static IEnumerable<object[]> GetGXFormats()
        {
            yield return new object[] { new TPL() };
            yield return new object[] { new BTI() };
            yield return new object[] { new TXE() };
        }


        [TestMethod]
        [DynamicData(nameof(GetGXFormats), DynamicDataSourceType.Method)]
        public void I4(IImageEncoder format)
        {
            using var image = TestImage.Create(new GxI4Block());
            TestImage.TestFormat(format, image);
        }

        [TestMethod]
        [DynamicData(nameof(GetGXFormats), DynamicDataSourceType.Method)]
        public void I8(IImageEncoder format)
        {
            using var image = TestImage.Create(new GxI8Block());
            TestImage.TestFormat(format, image);
        }

        [TestMethod]
        [DynamicData(nameof(GetGXFormats), DynamicDataSourceType.Method)]
        public void IA4(IImageEncoder format)
        {
            using var image = TestImage.Create(new GxIA4Block());
            TestImage.TestFormat(format, image);
        }

        [TestMethod]
        [DynamicData(nameof(GetGXFormats), DynamicDataSourceType.Method)]
        public void IA8(IImageEncoder format)
        {
            using var image = TestImage.Create(new GxIA8Block());
            TestImage.TestFormat(format, image);
        }

        [TestMethod]
        [DynamicData(nameof(GetGXFormats), DynamicDataSourceType.Method)]
        public void RGB565(IImageEncoder format)
        {
            using var image = TestImage.Create(new GxRGB565Block());
            TestImage.TestFormat(format, image);
        }

        [TestMethod]
        [DynamicData(nameof(GetGXFormats), DynamicDataSourceType.Method)]
        public void RGB5A3(IImageEncoder format)
        {
            using var image = TestImage.Create(new GxRGB5A3Block());
            TestImage.TestFormat(format, image);
        }

        [TestMethod]
        [DynamicData(nameof(GetGXFormats), DynamicDataSourceType.Method)]
        public void RGBA32(IImageEncoder format)
        {
            using var image = TestImage.Create(new GxRGBA32Block());
            TestImage.TestFormat(format, image);
        }


        [TestMethod]
        [DynamicData(nameof(GetGXFormats), DynamicDataSourceType.Method)]
        public void C4_RGB565(IImageEncoder format)
        {
            using var image = TestImage.CreatePalette<I4, RGB565>(new GxI4Block());
            TestImage.TestFormat(format, image);
        }

        [TestMethod]
        [DynamicData(nameof(GetGXFormats), DynamicDataSourceType.Method)]
        public void C8_IA8(IImageEncoder format)
        {
            using var image = TestImage.CreatePalette<I<byte>, IA<byte>>(new GxI8Block());
            TestImage.TestFormat(format, image);
        }

        [TestMethod]
        [DynamicData(nameof(GetGXFormats), DynamicDataSourceType.Method)]
        public void C14_RGB5A3(IImageEncoder format)
        {
            using var image = TestImage.CreatePalette<I<ushort>, RGB5A3>(new GxI14Block());
            TestImage.TestFormat(format, image);
        }

        [TestMethod]
        [DynamicData(nameof(GetGXFormats), DynamicDataSourceType.Method)]
        public void CMPR(IImageEncoder format)
        {
            using var image = TestImage.Create(new GxCMPRBlock());
            TestImage.TestFormat(format, image);
        }


        [TestMethod]
        [DynamicData(nameof(GetGXFormats), DynamicDataSourceType.Method)]
        public void Texture_CMPR(IImageEncoder format)
        {
            using var image = new FlatTexture<RGBA<byte>>(TestImage.Create(new GxCMPRBlock()),2);
            TestImage.TestFormat(format, image);
        }
    }
}
