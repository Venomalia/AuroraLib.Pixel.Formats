using AuroraLib.Pixel.BlockProcessor;
using AuroraLib.Pixel.Formats.Dolphin.PixelFormats;
using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace AuroraLib.Pixel.Formats.Dolphin.BlockProcessor
{
    /// <summary>
    /// Provides encoding and decoding for 4x4 blocks of RGB5A3 pixel data.
    /// </summary>
    public sealed class GxRGB5A3Block : IBlockProcessor<RGB5A3>
    {
        private const int BlockSize = 4, BPB = BlockSize * BlockSize * 2;

        /// <inheritdoc/>
        public int BlockWidth => BlockSize;

        /// <inheritdoc/>
        public int BlockHeight => BlockSize;

        /// <inheritdoc/>
        public int BytesPerBlock => BPB;

        /// <inheritdoc/>
        public void DecodeBlock(ReadOnlySpan<byte> source, Span<RGB5A3> target, int stride)
        {
            ReadOnlySpan<ushort> sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
            Span<ushort> targetUshort = MemoryMarshal.Cast<RGB5A3, ushort>(target);
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
        public void EncodeBlock(ReadOnlySpan<RGB5A3> source, Span<byte> target, int stride)
        {
            ReadOnlySpan<ushort> sourceUshort = MemoryMarshal.Cast<RGB5A3, ushort>(source);
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
