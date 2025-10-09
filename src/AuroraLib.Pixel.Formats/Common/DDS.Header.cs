using System;

namespace AuroraLib.Pixel.Formats.Common
{
    public sealed partial class DDS
    {
        private struct Header
        {
            //public Identifier32 Magic;
            /// <summary>
            /// Size of structure.This member must be set to 124.
            /// </summary>
            public uint HeaderSize;
            /// <summary>
            /// Flags to indicate which members contain valid data.
            /// </summary>
            public Flags Flag;
            /// <summary>
            /// Surface height (in pixels).
            /// </summary>
            public uint Height;
            /// <summary>
            /// Surface width (in pixels).
            /// </summary>
            public uint Width;
            /// <summary>
            /// The pitch or number of bytes per scan line in an uncompressed texture;
            /// the total number of bytes in the top level texture for a compressed texture.
            /// </summary>
            public uint PitchOrLinearSize;
            /// <summary>
            /// Depth of a volume texture (in pixels), otherwise unused.
            /// </summary>
            public uint Depth;
            /// <summary>
            /// Number of mipmap levels, otherwise unused.
            /// </summary>
            public uint MipMapCount;

            #region Reserved1
            /// <summary>
            /// Unused.
            /// </summary>
            public unsafe Span<uint> Reserved1
            {
                get
                {
                    fixed (uint* Ptr = &_Reserved1_0)
                    {
                        return new Span<uint>(Ptr, 11);
                    }
                }
            }

            private uint _Reserved1_0;
            private uint _Reserved1_1;
            private uint _Reserved1_2;
            private uint _Reserved1_3;

            private uint _Reserved1_4;
            private uint _Reserved1_5;
            private uint _Reserved1_6;
            private uint _Reserved1_7;

            private uint _Reserved1_8;
            private uint _Reserved1_9;
            private uint _Reserved1_10;
            #endregion

            /// <summary>
            /// Pixel format
            /// </summary>
            public PixelFormat Format;
            /// <summary>
            /// Specifies the complexity of the surfaces stored.
            /// </summary>
            public Caps1Flag Caps1;
            /// <summary>
            /// Additional detail about the surfaces stored.
            /// </summary>
            public Caps2Flag Caps2;
            /// <summary>
            /// Unused.
            /// </summary>
            public uint Caps3;
            /// <summary>
            /// Unused.
            /// </summary>
            public uint Caps4;
            /// <summary>
            /// Unused.
            /// </summary>
            public uint Reserved2;

            public Header(uint width, uint height, PixelFormat pixelFormat, uint pitchOrLinearSize, uint mipMapCount, Caps2Flag caps2, uint depth) : this()
            {
                HeaderSize = 0x7C;
                Width = width;
                Height = height;
                PitchOrLinearSize = pitchOrLinearSize;
                Format = pixelFormat;
                Flag = Flags.Caps | Flags.Height | Flags.Width | Flags.PixelFormat;
                Flag |= pixelFormat.RGBBitCount != 0 ? Flags.Pitch : Flags.LinearSize;
                Caps1 = Caps1Flag.Texture;
                Caps2 = caps2;

                if (mipMapCount != 0)
                {
                    Flag |= Flags.MipMapCount;
                    Caps1 |= Caps1Flag.Complex | Caps1Flag.MipMap;
                    MipMapCount = mipMapCount + 1;
                }

                if (caps2 == Caps2Flag.Volume)
                {
                    Flag |= Flags.Depth;
                    Depth = depth;
                }

                if (Caps2 != Caps2Flag.None)
                    Caps1 |= Caps1Flag.Complex;
            }

            public readonly bool HasMipmaps()
                => Flag.HasFlag(Flags.MipMapCount) && Caps1.HasFlag(Caps1Flag.MipMap);

            [Flags]
            public enum Flags : uint
            {
                None = 0x0,
                /// <summary>
                /// Required in every .dds file.
                /// </summary>
                Caps = 0x1,

                /// <summary>
                /// Required in every .dds file.
                /// </summary>
                Height = 0x2,

                /// <summary>
                /// Required in every .dds file.
                /// </summary>
                Width = 0x4,

                /// <summary>
                /// Required when pitch is provided for an uncompressed texture.
                /// </summary>
                Pitch = 0x8,

                /// <summary>
                /// Required in every .dds file.
                /// </summary>
                PixelFormat = 0x1000,

                /// <summary>
                /// Required in a mipmapped texture.
                /// </summary>
                MipMapCount = 0x20000,

                /// <summary>
                /// Required when pitch is provided for a compressed texture.
                /// </summary>
                LinearSize = 0x80000,

                /// <summary>
                /// Required in a depth texture.
                /// </summary>
                Depth = 0x800000
            }

            [Flags]
            public enum Caps1Flag : uint
            {
                None = 0x0,
                /// <summary>
                /// Optional; must be used on any file that contains more than one surface (a mipmap, a cubic environment map, or mipmapped volume texture).
                /// </summary>
                Complex = 0x8,

                /// <summary>
                /// Optional; should be used for a mipmap.
                /// </summary>
                MipMap = 0x400000,

                /// <summary>
                /// Required.
                /// </summary>
                Texture = 0x1000
            }

            [Flags]
            public enum Caps2Flag : uint
            {
                None = 0x0,
                /// <summary>
                /// Required for a cube map.
                /// </summary>
                Cubemap = 0x200,

                /// <summary>
                /// Required when these surfaces are stored in a cube map.
                /// </summary>
                CubemapPositiveX = 0x400,

                /// <summary>
                /// Required when these surfaces are stored in a cube map.
                /// </summary>
                CubemapNegativeX = 0x800,

                /// <summary>
                /// Required when these surfaces are stored in a cube map.
                /// </summary>
                CubemapPositiveY = 0x1000,

                /// <summary>
                /// Required when these surfaces are stored in a cube map.
                /// </summary>
                CubemapNegativeY = 0x2000,

                /// <summary>
                /// Required when these surfaces are stored in a cube map.
                /// </summary>
                CubemapPositiveZ = 0x4000,

                /// <summary>
                /// Required when these surfaces are stored in a cube map.
                /// </summary>
                CubemapNegativeZ = 0x8000,

                /// <summary>
                /// Required for a volume texture.
                /// </summary>
                Volume = 0x200000
            }
        }

    }
}
