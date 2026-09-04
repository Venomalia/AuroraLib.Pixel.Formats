using AuroraLib.Pixel.BlockProcessor;
using AuroraLib.Pixel.PixelFormats;
using System;

namespace AuroraLib.Pixel.Formats.Dolphin.BlockProcessor
{
    /// <summary>
    /// A block processor for 4bpp (4 bits per pixel) tile data used on Game Boy Advance, GameCube, Wii.
    /// Each block represents an 8x8 pixel tile with 4-bit color indices.
    /// </summary>
    public sealed class GxI4Block : IBlockProcessor<I4>
    {
        private const int BPB = 4 * BlockSize * BlockSize / 8, BlockSize = 8;

        /// <inheritdoc/>
        public int BlockWidth => BlockSize;

        /// <inheritdoc/>
        public int BlockHeight => BlockSize;

        /// <inheritdoc/>
        public int BytesPerBlock => BPB;

        /// <inheritdoc/>
        public void DecodeBlock(ReadOnlySpan<byte> source, Span<I4> target, int stride)
        {
            int i = 0;
            for (int y = 0; y < BlockHeight; y++)
            {
                int dst = y * stride;
                for (int x = 0; x < BlockWidth; x += 2)
                {
                    byte packed = source[i++];
                    target[dst + x].I = (byte)(packed >> 4);
                    target[dst + x + 1].I = (byte)(packed & 0b1111);
                }
            }
        }

        /// <inheritdoc/>
        public void EncodeBlock(ReadOnlySpan<I4> source, Span<byte> target, int stride)
        {
            int i = 0;
            for (int y = 0; y < BlockSize; y++)
            {
                int src = y * stride;
                for (int x = 0; x < BlockSize; x += 2)
                {
                    byte packed = (byte)(source[src + x + 1].I & 0b1111);
                    packed |= (byte)(source[src + x].I << 4);
                    target[i++] = packed;
                }
            }
        }
    }
}
