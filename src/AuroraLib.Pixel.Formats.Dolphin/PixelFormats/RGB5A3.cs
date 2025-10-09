using AuroraLib.Pixel.PixelFormats;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace AuroraLib.Pixel.Formats.Dolphin.PixelFormats
{
    /// <summary>
    /// Represents a packed 16-bit RGBA color in the RGB5A3 format used by Nintendo GameCube and Wii.
    /// It dynamically switches between two modes: <see cref="RGB555"/> for opaque pixels and <see cref="RGBA16"/> for transparent pixels.
    /// </summary>
    public struct RGB5A3 : IRGBA<byte>, IColor<RGB5A3>
    {
        /// <inheritdoc cref="IColor.FormatInfo"/>
        public static readonly PixelFormatInfo FormatInfo = new PixelFormatInfo(16, 5, 10, 5, 5, 5, 0, 1, 15, PixelFormatInfo.ColorSpaceType.RGB, PixelFormatInfo.ChannelType.Dynamic);
        readonly PixelFormatInfo IColor.FormatInfo => FormatInfo;

        private const ushort AlphaMask = 0x8000;

        private ushort PackedValue;

        private readonly bool HasAlpha => (PackedValue & AlphaMask) == 0;

        /// <inheritdoc/>
        public byte R
        {
            readonly get => HasAlpha
                ? Expand4BitTo8Bit(PackedValue >> 8 & 0b1111)
                : Expand5BitTo8Bit(PackedValue >> 10 & 0b11111);
            set => PackedValue = PackRGBA(value, G, B, A);
        }

        /// <inheritdoc/>
        public byte G
        {
            readonly get => HasAlpha
                ? Expand4BitTo8Bit(PackedValue >> 4 & 0b1111)
                : Expand5BitTo8Bit(PackedValue >> 5 & 0b11111);
            set => PackedValue = PackRGBA(R, value, B, A);
        }

        /// <inheritdoc/>
        public byte B
        {
            readonly get => HasAlpha
                ? Expand4BitTo8Bit(PackedValue & 0b1111)
                : Expand5BitTo8Bit(PackedValue & 0b11111);
            set => PackedValue = PackRGBA(R, G, value, A);
        }

        /// <inheritdoc/>
        public byte A
        {
            readonly get => HasAlpha
                ? Expand3BitTo8Bit(PackedValue >> 12 & 0b111)
                : byte.MaxValue;
            set => PackedValue = PackRGBA(R, G, B, value);
        }

        float IColor.Mask
        {
            readonly get => (float)A / byte.MaxValue;
            set => A = (byte)(value * byte.MaxValue);
        }

        readonly byte IRGB<byte>.A => A;

        private static ushort PackRGBA(byte r, byte g, byte b, byte a)
            => a == byte.MaxValue
                // 1RRR RRGG GGGB BBBB
                ? (ushort)(AlphaMask | (r & 0b1111_1000) << 7 | (g & 0b1111_1000) << 2 | b >> 3)
                // 0AAA RRRR GGGG BBBB
                : (ushort)((a & 0b11100000) << 7 | (r & 0b11110000) << 4 | g & 0b11110000 | b >> 4);

        public RGB5A3(byte r, byte g, byte b, byte a) => PackedValue = PackRGBA(r, g, b, a);

        /// <inheritdoc/>
        public readonly bool Equals(RGB5A3 other) => PackedValue == other.PackedValue;

        /// <inheritdoc/>
        public readonly Vector4 ToScaledVector4() => new Vector4(R, G, B, A) / ByteMaxF;

        /// <inheritdoc/>
        public void FromScaledVector4(Vector4 vector)
        {
            vector *= ByteMaxF;
            PackedValue = PackRGBA((byte)vector.X, (byte)vector.Y, (byte)vector.Z, (byte)vector.W);
        }

        /// <inheritdoc/>
        public readonly void ToRGBA<TColor>(ref TColor value) where TColor : IRGBA<byte>
        {
            value.R = R;
            value.G = G;
            value.B = B;
            value.A = A;
        }

        /// <inheritdoc/>
        public void From8Bit<TColor>(TColor value) where TColor : IRGB<byte>
            => PackedValue = PackRGBA(value.R, value.G, value.B, value.A);

        /// <inheritdoc/>
        public void From16Bit<TColor>(TColor value) where TColor : IRGB<ushort>
            => PackedValue = PackRGBA((byte)(value.R >> 8), (byte)(value.G >> 8), (byte)(value.B >> 8), (byte)(value.A >> 8));

        /// <inheritdoc/>
        public override readonly string ToString()
            => HasAlpha ? $"{nameof(RGB5A3)}({R >> 4}, {G >> 4}, {B >> 4}, {A >> 5})" : $"{nameof(RGB5A3)}({R >> 3}, {G >> 3}, {B >> 3})";

        public static implicit operator ushort(RGB5A3 pixel) => pixel.PackedValue;

        public static implicit operator RGB5A3(ushort value) => Unsafe.As<ushort, RGB5A3>(ref value);

        // helper
        private static byte Expand3BitTo8Bit(int value) => (byte)(value << 5 | value << 2 | value >> 1);
        private static byte Expand4BitTo8Bit(int value) => (byte)(value << 4 | value);
        private static byte Expand5BitTo8Bit(int value) => (byte)(value << 3 | value >> 2);
        private const float ByteMaxF = byte.MaxValue;
    }
}
