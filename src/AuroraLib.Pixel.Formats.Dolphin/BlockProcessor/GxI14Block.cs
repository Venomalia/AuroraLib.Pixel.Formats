using AuroraLib.Pixel.BlockProcessor;
using AuroraLib.Pixel.PixelFormats;
using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace AuroraLib.Pixel.Formats.Dolphin.BlockProcessor
{
    /// <summary>
    /// Provides block-based encoding and decoding for I14 (Intensity 14-bit) pixels. Each block has a size of 4x4 pixels.
    /// Used on GameCube and Wii.
    /// </summary>
    public sealed class GxI14Block : IBlockProcessor<I<ushort>>
    {
        private const int BPB = 2 * BlockSize * BlockSize, BlockSize = 4;
        /// <inheritdoc />
        public int BlockWidth => BlockSize;

        /// <inheritdoc />
        public int BlockHeight => BlockSize;

        /// <inheritdoc />
        public int BytesPerBlock => BPB;

        public void DecodeBlock(ReadOnlySpan<byte> source, Span<I<ushort>> target, int stride)
        {
            ReadOnlySpan<ushort> sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
            int i = 0;
            for (int y = 0; y < BlockSize; y++)
            {
                int dst = y * stride;
                for (int x = 0; x < BlockSize; x++)
                {
                    target[dst + x] = (ushort)(BinaryPrimitives.ReverseEndianness(sourceUshort[i++]) & 0x3FFF);
                }
            }
        }

        public void EncodeBlock(ReadOnlySpan<I<ushort>> source, Span<byte> target, int stride)
        {
            Span<ushort> targetUshort = MemoryMarshal.Cast<byte, ushort>(target);
            int i = 0;
            for (int y = 0; y < BlockSize; y++)
            {
                int src = y * stride;
                for (int x = 0; x < BlockSize; x++)
                {
                    targetUshort[i++] = BinaryPrimitives.ReverseEndianness((ushort)(source[src + x] & 0x3FFF));
                }
            }
        }
    }
}
