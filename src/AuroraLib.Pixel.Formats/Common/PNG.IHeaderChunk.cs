using AuroraLib.Core;
using AuroraLib.Core.Exceptions;
using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace AuroraLib.Pixel.Formats.Common
{
    public partial class PNG
    {
        [StructLayout(LayoutKind.Sequential, Size = 13)]
        private readonly struct IHeaderChunk
        {
            private readonly uint _width;
            public readonly uint Width => BinaryPrimitives.ReverseEndianness(_width);
            private readonly uint _height;
            public readonly uint Height => BinaryPrimitives.ReverseEndianness(_height);
            public readonly byte BitDepth;
            public readonly ColorTypes ColorType;
            public readonly byte CompressionMethod; // 0 == Deflate
            public readonly byte FilterMethod;
            public readonly InterlaceMethods InterlaceMethod;

            public IHeaderChunk(uint width, uint height, byte bitDepth, ColorTypes colorType, InterlaceMethods interlaceMethod = InterlaceMethods.None)
            {
                _width = BinaryPrimitives.ReverseEndianness(width);
                _height = BinaryPrimitives.ReverseEndianness(height);
                BitDepth = bitDepth;
                ColorType = colorType;
                CompressionMethod = 0;
                FilterMethod = 0;
                InterlaceMethod = interlaceMethod;
            }

            public readonly int BitPerPixel => BitDepth * Channels;

            public readonly int Channels => ColorType switch
            {
                ColorTypes.Grayscale => 1,
                ColorTypes.RGB => 3,
                ColorTypes.UsePalette => 1,
                ColorTypes.GrayscaleAlpha => 2,
                ColorTypes.RGBA => 4,
                _ => throw new NotImplementedException(),
            };

            public void Validate()
            {
                ThrowIf.Zero(Width);
                ThrowIf.Zero(Height);
                ThrowIf.Zero(BitDepth);
#if NET6_0_OR_GREATER
                if (!(BitDepth is 1 or 2 or 4 or 8 or 16))
#else
                if (!(BitDepth == 1 || BitDepth== 2 || BitDepth== 4 || BitDepth == 8 || BitDepth == 16))
#endif
                    throw new NotSupportedException($"Unsupported BitDepth: {BitDepth}");

                if (!Enum.IsDefined(typeof(ColorTypes), ColorType))
                    throw new NotSupportedException($"Unsupported ColorType: {ColorType}");

                if (CompressionMethod != 0)
                    throw new NotSupportedException($"Unsupported CompressionMethod: {CompressionMethod}");

                if (FilterMethod != 0)
                    throw new NotSupportedException($"Unsupported FilterMethod: {FilterMethod}");

                if (!Enum.IsDefined(typeof(InterlaceMethods), InterlaceMethod))
                    throw new NotSupportedException($"Unsupported InterlaceMethod: {InterlaceMethod}");
            }

            public enum InterlaceMethods : byte
            {
                None = 0,
                Adam7 = 1,
            }
        }
    }
}
