using AuroraLib.Pixel.BlockProcessor;
using AuroraLib.Pixel.Formats.Common;
using AuroraLib.Pixel.PixelFormats;
using AuroraLib.Pixel.Texture;

namespace PixelFormatTest
{
    [TestClass]
    public sealed class DDSTest
    {

        [TestMethod]
        public void RGBA24()
        {
            using var image = TestImage.Create<RGB<byte>>();
            TestImage.TestFormat(new DDS(), image);
        }

        [TestMethod]
        public void RGBA32()
        {
            using var image = TestImage.Create<RGBA<byte>>();
            TestImage.TestFormat(new DDS(), image);
        }

        [TestMethod]
        public void RGBA24_DXT10()
        {
            using var image = TestImage.Create<RGB<byte>>();
            TestImage.TestFormat(new DDS() { ForceDXT10Header = true }, image);
        }

        [TestMethod]
        public void RGBA32_DXT10()
        {
            using var image = TestImage.Create<RGBA<byte>>();
            TestImage.TestFormat(new DDS() { ForceDXT10Header = true }, image);
        }

        [TestMethod]
        public void BC1()
        {
            using var image = TestImage.Create(new BC1Block<RGBA<byte>>());

            TestImage.TestFormat(new DDS(), image);
        }

        [TestMethod]
        public void BC2()
        {
            using var image = TestImage.Create(new BC2Block<RGBA<byte>>());
            TestImage.TestFormat(new DDS(), image);
        }

        [TestMethod]
        public void BC3()
        {
            using var image = TestImage.Create(new BC3Block<RGBA<byte>>());
            TestImage.TestFormat(new DDS(), image);
        }

        [TestMethod]
        public void BC4U()
        {
            using var image = TestImage.Create(new BC4UBlock<I<byte>>());
            TestImage.TestFormat(new DDS(), image);
        }

        [TestMethod]
        public void BC4S()
        {
            using var image = TestImage.Create(new BC4SBlock<I<sbyte>>());
            TestImage.TestFormat(new DDS(), image);
        }

        [TestMethod]
        public void BC5U()
        {
            using var image = TestImage.Create(new BC5UBlock<IA<byte>>());
            TestImage.TestFormat(new DDS(), image);
        }

        [TestMethod]
        public void BC5S()
        {
            using var image = TestImage.Create(new BC5SBlock<IA<sbyte>>());
            TestImage.TestFormat(new DDS(), image);
        }

        [TestMethod]
        public void FlatTexture_RGBA32()
        {
            using var image = TestImage.CreateTexture<RGBA<byte>>(3, 20, 20);
            TestImage.TestFormat(new DDS(), image);
        }

        [TestMethod]
        public void Cubemap_RGBA32()
        {
            using var image = new Cubemap<RGBA<byte>>(TestImage.CreateTexture<RGBA<byte>>(1), null, TestImage.CreateTexture<RGBA<byte>>(1), null, TestImage.CreateTexture<RGBA<byte>>(1), null);
            TestImage.TestFormat(new DDS(), image);
        }

        [TestMethod]
        public void VolumeTexture_RGBA32()
        {
            using var image = new VolumeTexture<RGBA<byte>>([TestImage.CreateTexture<RGBA<byte>>(1), TestImage.CreateTexture<RGBA<byte>>(1), TestImage.CreateTexture<RGBA<byte>>(1)]);
            TestImage.TestFormat(new DDS(), image);
        }

    }
}
