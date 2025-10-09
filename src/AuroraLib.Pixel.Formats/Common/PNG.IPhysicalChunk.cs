using System.Buffers.Binary;

namespace AuroraLib.Pixel.Formats.Common
{
    public partial class PNG
    {
        private readonly struct IPhysicalChunk
        {
            private readonly uint _PixelsPerUnitX;
            public readonly uint PixelsPerUnitX => BinaryPrimitives.ReverseEndianness(_PixelsPerUnitX);
            private readonly uint _PixelsPerUnitY;
            public readonly uint PixelsPerUnitY => BinaryPrimitives.ReverseEndianness(_PixelsPerUnitY);
            public readonly byte UnitSpecifier;
            public readonly bool IsMeter => UnitSpecifier == 1;
        }
    }
}
