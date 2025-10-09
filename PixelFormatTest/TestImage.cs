using AuroraLib.Pixel;
using AuroraLib.Pixel.BlockProcessor;
using AuroraLib.Pixel.Formats;
using AuroraLib.Pixel.Formats.Common;
using AuroraLib.Pixel.Image;
using AuroraLib.Pixel.PixelFormats;
using AuroraLib.Pixel.Processing;
using AuroraLib.Pixel.Texture;
using System.Numerics;

namespace PixelFormatTest
{
    public static class TestImage
    {
        public static IImage<TColor> Create<TColor>(int width = 10, int height = 4) where TColor : unmanaged, IColor<TColor>
        {
            var image = new MemoryImage<TColor>(width, height);
            FillRandom(image);

            return image;
        }

        public static IImage<TColor> CreatePalette<TIndex, TColor>(int width = 10, int height = 4) where TIndex : unmanaged, IIndexColor, IColor<TIndex> where TColor : unmanaged, IColor<TColor>
        {
            var image = new PaletteImage<TIndex, TColor>(width, height);
            FillRandom(image);

            return image;
        }

        public static IImage<TColor> Create<TColor>(IBlockProcessor<TColor> processor, int width = 10, int height = 4) where TColor : unmanaged, IColor<TColor>
        {
            var image = new BlockImage<TColor>(processor, width, height);
            FillRandom(image);
            return image;
        }

        public static IImage<TColor> CreatePalette<TIndex, TColor>(IBlockProcessor<TIndex> processor, int width = 10, int height = 4) where TIndex : unmanaged, IIndexColor, IColor<TIndex> where TColor : unmanaged, IColor<TColor>
        {
            var image = new PaletteImage<TIndex, TColor>(new BlockImage<TIndex>(processor, width, height));
            FillRandom(image);

            return image;
        }

        public static FlatTexture<TColor> CreateTexture<TColor>(int mipmaps, int width = 10, int height = 4) where TColor : unmanaged, IColor<TColor>
            => new FlatTexture<TColor>(Create<TColor>(width, height), mipmaps);

        private static void FillRandom(IImage image)
        {
            Random r = new();
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    image[x, y] = NextVector4();
                }
            }

            Vector4 NextVector4()
            {
#if NET6_0_OR_GREATER
                return new Vector4(r.NextSingle(), r.NextSingle(), r.NextSingle(), r.NextSingle());
#else
                return new Vector4((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());
#endif
            }
        }

        public static void TestFormat(IImageEncoder encoder, IReadOnlyImage expected)
        {
            using var buffer = new MemoryStream();
            encoder.WriteImage(expected, buffer);
            buffer.Position = 0;
            if (encoder is IImageDecoder decoder)
            {
                using var newImage = decoder.ReadImage(buffer);
                AssertImageEqual(expected, newImage);
            }
            else if (encoder is IImageContainerFormat containerFormat)
            {
                var images = containerFormat.ReadImages(buffer);
                foreach (var image in images)
                {
                    AssertImageEqual(expected, image);
                    image.Dispose();
                }
            }
        }

        private static void AssertImageEqual(IReadOnlyImage expected, IReadOnlyImage actual)
        {
            if (expected is IEnumerable<IImage> expectedImages)
            {
                Assert.IsInstanceOfType(actual, typeof(IEnumerable<IImage>));

                var actualImages = ((IEnumerable<IImage>)actual).ToList();
                var expectedList = expectedImages.ToList();

                Assert.HasCount(expectedList.Count, actualImages);

                for (int i = 0; i < expectedList.Count; i++)
                    AssertImageEqual(expectedList[i], actualImages[i]);

                return;
            }

            Assert.AreEqual(expected.GetBounds(), actual.GetBounds());

            for (int y = 0; y < expected.Height; y++)
            {
                for (int x = 0; x < expected.Width; x++)
                {
                    Assert.AreEqual(expected[x, y], actual[x, y], $"Pixel mismatch at ({x}, {y}).");
                }
            }
        }
    }
}
