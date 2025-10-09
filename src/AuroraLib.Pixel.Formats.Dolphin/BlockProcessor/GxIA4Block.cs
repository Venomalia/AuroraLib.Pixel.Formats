using AuroraLib.Pixel.BlockProcessor;
using AuroraLib.Pixel.PixelFormats;
using System;
using System.Runtime.InteropServices;

namespace AuroraLib.Pixel.Formats.Dolphin.BlockProcessor
{
    /// <summary>
    /// Provides encoding and decoding for 8x4 blocks of IA4 (Intensity + Alpha, 4 bits each) pixel data.
    /// </summary>
    public sealed class GxIA4Block : IBlockProcessor<IA8>
    {
        private const int BWidth = 8, BHeight = 4, BPB = BWidth * BHeight;

        /// <inheritdoc/>
        public int BlockWidth => BWidth;

        /// <inheritdoc/>
        public int BlockHeight => BHeight;

        /// <inheritdoc/>
        public int BytesPerBlock => BPB;

        /// <inheritdoc/>
        public void DecodeBlock(ReadOnlySpan<byte> source, Span<IA8> target, int stride)
        {
            Span<byte> targetByte = MemoryMarshal.Cast<IA8, byte>(target);
            int i = 0;
            for (int y = 0; y < BHeight; y++)
            {
                int dst = y * stride;
                for (int x = 0; x < BWidth; x++)
                {
                    targetByte[dst + x] = source[i++];
                }
            }
        }

        /// <inheritdoc/>
        public void EncodeBlock(ReadOnlySpan<IA8> source, Span<byte> target, int stride)
        {
            ReadOnlySpan<byte> sourceByte = MemoryMarshal.Cast<IA8, byte>(source);
            int i = 0;
            for (int y = 0; y < BHeight; y++)
            {
                int src = y * stride;
                for (int x = 0; x < BWidth; x++)
                {
                    target[i++] = sourceByte[src + x];
                }
            }
        }
    }
}
