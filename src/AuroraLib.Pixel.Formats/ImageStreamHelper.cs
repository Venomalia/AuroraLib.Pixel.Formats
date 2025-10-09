using AuroraLib.Core.IO;
using AuroraLib.Pixel.BlockProcessor;
using AuroraLib.Pixel.Image;
using AuroraLib.Pixel.Texture;
using System;
using System.Buffers;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AuroraLib.Pixel.Formats
{
    /// <summary>
    /// Provides helper methods for reading and writing images and textures to streams.
    /// </summary>
    public static class ImageStreamHelper
    {

        /// <summary>
        /// Reads a texture with mipmaps from the specified stream.
        /// </summary>
        /// <typeparam name="TColor">The pixel color type.</typeparam>
        /// <param name="source">The stream to read from.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="mipmaps">The number of mipmaps to read.</param>
        /// <param name="block">The block processor used to decode the image.</param>
        /// <param name="strideAlignment">The alignment of each image row in bytes.</param>
        /// <returns>The decoded texture.</returns>
        public static FlatTexture<TColor> ReadTexture<TColor>(Stream source, int width, int height, int mipmaps, IBlockProcessor<TColor>? block = null, int strideAlignment = 4) where TColor : unmanaged, IColor<TColor>
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "The width must be greater than zero.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "The height must be greater than zero.");

            int maxMipmaps = GetMaxMipmaps(width, height);
            if (mipmaps < 0 || mipmaps > maxMipmaps)
                throw new ArgumentOutOfRangeException(nameof(mipmaps), mipmaps, $"The mipmap count must be between 0 and {maxMipmaps} for an image of {width}x{height}.");

            var tex = new FlatTexture<TColor>(ReadImage(source, width, height, block, strideAlignment));
            try
            {
                for (int i = 1; i <= mipmaps; i++)
                {
                    height >>= 1;
                    width >>= 1;
                    tex.Add(ReadImage(source, width, height, block, strideAlignment));
                }
            }
            catch
            {
                tex.Dispose();
                throw;
            }
            return tex;
        }

        /// <summary>
        /// Reads a texture with mipmaps from the specified stream.
        /// </summary>
        /// <typeparam name="TIndex">The palette index color type.</typeparam>
        /// <typeparam name="TColor">The palette color type.</typeparam>
        /// <param name="source">The stream to read from.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="mipmaps">The number of mipmaps to read.</param>
        /// <param name="palette">The palette used to decode the texture.</param>
        /// <param name="block">The block processor used to decode the image.</param>
        /// <param name="strideAlignment">The alignment of each image row in bytes.</param>
        /// <returns>The decoded texture.</returns>
        public static FlatTexture<TColor> ReadTexture<TIndex, TColor>(Stream source, int width, int height, int mipmaps, Memory<TColor> palette, IBlockProcessor<TIndex>? block = null, int strideAlignment = 4) where TIndex : unmanaged, IIndexColor, IColor<TIndex> where TColor : unmanaged, IColor<TColor>
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "The width must be greater than zero.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "The height must be greater than zero.");

            int maxMipmaps = GetMaxMipmaps(width, height);
            if (mipmaps < 0 || mipmaps > maxMipmaps)
                throw new ArgumentOutOfRangeException(nameof(mipmaps), mipmaps, $"The mipmap count must be between 0 and {maxMipmaps} for an image of {width}x{height}.");

            var tex = new FlatTexture<TColor>(new PaletteImage<TIndex, TColor>(ReadImage(source, width, height, block, strideAlignment), palette));
            try
            {
                for (int i = 1; i <= mipmaps; i++)
                {
                    height >>= 1;
                    width >>= 1;
                    tex.Add(new PaletteImage<TIndex, TColor>(ReadImage(source, width, height, block, strideAlignment), palette));
                }
            }
            catch
            {
                tex.Dispose();
                throw;
            }
            return tex;
        }

        private static int GetMaxMipmaps(int width, int height)
#if NET6_0_OR_GREATER
            => BitOperations.Log2((uint)Math.Min(width, height));
#else
        {
            int mipmaps = 0;

            while (width > 1 && height > 1)
            {
                width >>= 1;
                height >>= 1;
                mipmaps++;
            }

            return mipmaps;
        }
#endif
        /// <summary>
        /// Reads an image from the specified stream.
        /// </summary>
        /// <typeparam name="TColor">The pixel color type.</typeparam>
        /// <param name="source">The stream to read from.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="block">The block processor used to decode the image.</param>
        /// <param name="strideAlignment">The alignment of each image row in bytes.</param>
        /// <returns>The decoded image.</returns>
        public static IImage<TColor> ReadImage<TColor>(Stream source, int width, int height, IBlockProcessor<TColor>? block, int strideAlignment = 4) where TColor : unmanaged, IColor<TColor>
        {
            if (block == null)
            {
                return ReadImage<TColor>(source, width, height, strideAlignment);
            }
            else
            {
                BlockImage<TColor> image = new BlockImage<TColor>(block, width, height);
                source.ReadExactly(image.Raw);
                return image;
            }
        }

        /// <summary>
        /// Reads an image from the specified stream.
        /// </summary>
        /// <typeparam name="TColor">The pixel color type.</typeparam>
        /// <param name="source">The stream to read from.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="strideAlignment">The alignment of each image row in bytes.</param>
        /// <returns>The decoded image.</returns>
        public static MemoryImage<TColor> ReadImage<TColor>(Stream source, int width, int height, int strideAlignment = 4) where TColor : unmanaged, IColor<TColor>
        {
            int pixelSize = Unsafe.SizeOf<TColor>();
            int byteStride = GetCalculatedByteStride(width, pixelSize, strideAlignment);
            int stride = byteStride / pixelSize;
            bool aligned = byteStride == stride * pixelSize;
            MemoryImage<TColor> image = new MemoryImage<TColor>(width, height, stride);

            Span<byte> pixelData = MemoryMarshal.Cast<TColor, byte>(image.Pixel);
            if (aligned)
            {
                source.ReadExactly(pixelData);
            }
            else
            {
                int realStride = stride * pixelSize;
#if NET6_0_OR_GREATER
                // The padding overlaps the following row and is overwritten by the next read.
                Span<byte> row;
                for (int i = 0; i < height - 1; i++)
                {
                    row = pixelData.Slice(i * realStride, byteStride);
                    source.ReadExactly(row);
                }
                // Read the last line without its padding.
                row = pixelData[^realStride..];
                source.ReadExactly(row);
                source.Skip(byteStride - realStride);
#else
                byte[] buffer = ArrayPool<byte>.Shared.Rent(byteStride);
                ReadOnlySpan<byte> bufferSpan = buffer.AsSpan(0, realStride);
                try
                {
                    for (int i = 0; i < height; i++)
                    {
                        source.ReadExactly(buffer, 0, byteStride);
                        bufferSpan.CopyTo(pixelData.Slice(i * realStride, realStride));
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
#endif
            }
            return image;
        }


        public static void WriteTexture<TColor>(Stream destination, IReadOnlyImage<TColor> source, int strideAlignment = 4) where TColor : unmanaged, IColor<TColor>
        {
            if (source is FlatTexture<TColor> tex)
            {
                foreach (var level in tex)
                {
                    if (level is IBlockImage lb)
                        destination.Write(lb.Raw);
                    else
                        WriteImage(destination, level, strideAlignment);
                }
                return;
            }

            if (source is IBlockImage b)
                destination.Write(b.Raw);
            else
                WriteImage(destination, source, strideAlignment);
        }

        public static void WriteImage<TColor>(Stream destination, IReadOnlyImage<TColor> source, int strideAlignment = 4) where TColor : unmanaged, IColor<TColor>
        {
            int pixelSize = Unsafe.SizeOf<TColor>();
            int byteStride = GetCalculatedByteStride(source.Width, pixelSize, strideAlignment);

            if (source is MemoryImage<TColor> mi && mi.Stride * pixelSize == byteStride)
            {
                Span<byte> pixelData = MemoryMarshal.Cast<TColor, byte>(mi.Pixel);
                destination.Write(pixelData);
            }
            else
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(byteStride);
                Span<TColor> pixelbuffer = MemoryMarshal.Cast<byte, TColor>(buffer.AsSpan(0, source.Width * pixelSize));
                try
                {
                    for (int y = 0; y < source.Height; y++)
                    {
                        source.GetPixel(0, y, pixelbuffer);
                        destination.Write(buffer, 0, byteStride);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        internal static int GetCalculatedByteStride(int width, int bytesPerPixel, int alignment) => (width * bytesPerPixel + alignment - 1) / alignment * alignment;

        internal static void UnpackPixels(ReadOnlySpan<byte> buffer, Span<byte> pixel, int count, int bitDepth, int width)
        {
            int mask = (1 << bitDepth) - 1;
            int pixelsPerByte = 8 / bitDepth;

            int x = 0;
            for (int i = 0; i < count && x < width; i++)
            {
                byte b = buffer[i];
                int shift = 8 - bitDepth;
                for (int p = 0; p < pixelsPerByte && x < width; p++, shift -= bitDepth)
                {
                    int value = (b >> shift) & mask;
                    pixel[x++] = (byte)value;
                }
            }
        }

        internal static void PackPixels(ReadOnlySpan<byte> pixels, Span<byte> buffer, int bitDepth, int width)
        {
            int mask = (1 << bitDepth) - 1;
            int pixelsPerByte = 8 / bitDepth;

            int x = 0;
            for (int i = 0; i < buffer.Length && x < width; i++)
            {
                byte b = 0;
                int shift = 8 - bitDepth;

                for (int p = 0; p < pixelsPerByte && x < width; p++, shift -= bitDepth)
                {
                    b |= (byte)((pixels[x++] & mask) << shift);
                }

                buffer[i] = b;
            }
        }
    }
}
