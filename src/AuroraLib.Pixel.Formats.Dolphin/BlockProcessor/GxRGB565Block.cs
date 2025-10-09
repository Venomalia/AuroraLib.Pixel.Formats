using AuroraLib.Pixel.BlockProcessor;
using AuroraLib.Pixel.PixelFormats;
using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace AuroraLib.Pixel.Formats.Dolphin.BlockProcessor
{
    /// <summary>
    /// Provides encoding and decoding for 4x4 blocks of RGB565 pixel data.
    /// </summary>
    public sealed class GxRGB565Block : IBlockProcessor<RGB565>
    {
        private const int BlockSize = 4, BPB = BlockSize * BlockSize * 2;

        /// <inheritdoc/>
        public int BlockWidth => BlockSize;

        /// <inheritdoc/>
        public int BlockHeight => BlockSize;

        /// <inheritdoc/>
        public int BytesPerBlock => BPB;

        /// <inheritdoc/>
        public void DecodeBlock(ReadOnlySpan<byte> source, Span<RGB565> target, int stride)
        {
            ReadOnlySpan<ushort> sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
            Span<ushort> targetUshort = MemoryMarshal.Cast<RGB565, ushort>(target);
            int i = 0;
            for (int y = 0; y < BlockSize; y++)
            {
                int dst = y * stride;
                for (int x = 0; x < BlockSize; x++)
                {
                    targetUshort[dst + x] = BinaryPrimitives.ReverseEndianness(sourceUshort[i++]);
                }
            }
        }

        /// <inheritdoc/>
        public void EncodeBlock(ReadOnlySpan<RGB565> source, Span<byte> target, int stride)
        {
            ReadOnlySpan<ushort> sourceUshort = MemoryMarshal.Cast<RGB565, ushort>(source);
            Span<ushort> targetUshort = MemoryMarshal.Cast<byte, ushort>(target);
            int i = 0;
            for (int y = 0; y < BlockSize; y++)
            {
                int src = y * stride;
                for (int x = 0; x < BlockSize; x++)
                {
                    targetUshort[i++] = BinaryPrimitives.ReverseEndianness(sourceUshort[src + x]);
                }
            }
        }
    }
}
