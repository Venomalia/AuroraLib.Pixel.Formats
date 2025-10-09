using AuroraLib.Pixel.BlockProcessor;
using AuroraLib.Pixel.PixelFormats;
using System;
using System.Buffers.Binary;

namespace AuroraLib.Pixel.Formats.Dolphin.BlockProcessor
{
    /// <summary>
    /// Encodes and decodes CMPR (S3TC/DXT1-like) texture blocks used on GameCube and Wii.
    /// </summary>
    public sealed class GxCMPRBlock : IBlockProcessor<RGBA<byte>>
    {
        private const int BPB = 4 * 8, BlockSize = 8;

        /// <inheritdoc/>
        public int BlockWidth => BlockSize;

        /// <inheritdoc/>
        public int BlockHeight => BlockSize;

        /// <inheritdoc/>
        public int BytesPerBlock => BPB;

        public GxCMPRBlock() : this(BC1Block<RGBA<byte>>.DefaultSelector)
        { }

        public GxCMPRBlock(BC1Block<RGBA<byte>>.IColorSelector colorEncoder)
            => _colorEncoder = colorEncoder;

        private readonly BC1Block<RGBA<byte>>.IColorSelector _colorEncoder;

        /// <inheritdoc/>
        public void DecodeBlock(ReadOnlySpan<byte> source, Span<RGBA<byte>> target, int stride)
        {
            Span<RGBA<byte>> interpolatedColors = stackalloc RGBA<byte>[4];

            for (int subblock = 0; subblock < 4; subblock++)
            {
                int subblockX = (subblock & 1) * 4;
                int subblockY = (subblock >> 1) * 4;

                RGB565 color0 = (RGB565)BinaryPrimitives.ReadUInt16BigEndian(source.Slice(subblock * 8, 2));
                RGB565 color1 = (RGB565)BinaryPrimitives.ReadUInt16BigEndian(source.Slice(subblock * 8 + 2, 2));
                int colorIndexes = BinaryPrimitives.ReadInt32BigEndian(source.Slice(subblock * 8 + 4, 4));

                GetInterpolatedColours(color0, color1, interpolatedColors);
                for (int pixel = 0; pixel < 16; pixel++)
                {
                    int x = subblockX + (pixel & 3);
                    int y = subblockY + (pixel >> 2);
                    int colorIndex = colorIndexes >> (15 - pixel) * 2 & 3;
                    target[y * stride + x] = interpolatedColors[colorIndex];
                }
            }
        }

        /// <inheritdoc/>
        public void EncodeBlock(ReadOnlySpan<RGBA<byte>> source, Span<byte> target, int stride)
        {
            Span<RGBA<byte>> interpolatedColors = stackalloc RGBA<byte>[4];
            Span<RGBA<byte>> subblockPixels = stackalloc RGBA<byte>[16];

            for (int subblock = 0; subblock < 4; subblock++)
            {
                int subblockX = (subblock & 1) * 4;
                int subblockY = (subblock >> 1) * 4;

                _colorEncoder.GetEndpoints(source.Slice(subblockY * stride + subblockX), stride, out RGB565 color0, out RGB565 color1);
                GetInterpolatedColours(color0, color1, interpolatedColors);


                int colorIndexes = 0;
                for (int pixel = 0; pixel < 16; pixel++)
                {
                    int x = subblockX + (pixel & 3);
                    int y = subblockY + (pixel >> 2);
                    int index = y * stride + x;
                    int colorIndex = GetColorIndex(source[index], interpolatedColors, _colorEncoder.AlphaThreshold);
                    colorIndexes |= colorIndex << (15 - pixel) * 2;
                }

                BinaryPrimitives.WriteUInt16BigEndian(target.Slice(subblock * 8, 2), (ushort)color0);
                BinaryPrimitives.WriteUInt16BigEndian(target.Slice(subblock * 8 + 2, 2), (ushort)color1);
                BinaryPrimitives.WriteInt32BigEndian(target.Slice(subblock * 8 + 4, 4), colorIndexes);
            }
        }

        static void GetInterpolatedColours(RGB565 left, RGB565 right, Span<RGBA<byte>> colors)
        {
            left.ToRGBA(ref colors[0]);
            right.ToRGBA(ref colors[1]);

            //Needs Alpha Color?
            if ((ushort)left > (ushort)right)
            {
                colors[2].R = (byte)((2 * left.R + right.R) / 3);
                colors[2].G = (byte)((2 * left.G + right.G) / 3);
                colors[2].B = (byte)((2 * left.B + right.B) / 3);
                colors[2].A = byte.MaxValue;

                colors[3].R = (byte)((left.R + 2 * right.R) / 3);
                colors[3].G = (byte)((left.G + 2 * right.G) / 3);
                colors[3].B = (byte)((left.B + 2 * right.B) / 3);
                colors[3].A = byte.MaxValue;
            }
            else
            {
                colors[2].R = (byte)(left.R + right.R >> 1);
                colors[2].G = (byte)(left.G + right.G >> 1);
                colors[2].B = (byte)(left.B + right.B >> 1);
                colors[2].A = byte.MaxValue;
                colors[3].A = byte.MinValue;
            }
        }

        static int GetColorIndex(RGBA<byte> pixel, ReadOnlySpan<RGBA<byte>> colors, byte alphaThreshold)
        {
            int minDistance = int.MaxValue, colorIndex = 0;

            //Needs Alpha Color?
            if (pixel.A < alphaThreshold && colors[colors.Length - 1].A == 0)
                return colors.Length - 1;

            for (int i = 0; i < colors.Length; i++)
            {
                if (colors[i].A == 0)
                    continue;

                int distance = CalculateColorDistance(pixel, colors[i]);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    colorIndex = i;
                }
            }

            return colorIndex;

            static int CalculateColorDistance(RGBA<byte> c1, RGBA<byte> c2)
            {
                int dr = c1.R - c2.R;
                int dg = c1.G - c2.G;
                int db = c1.B - c2.B;
                return dr * dr + dg * dg + db * db;
            }
        }
    }
}
