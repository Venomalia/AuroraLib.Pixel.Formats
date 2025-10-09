using System;

namespace AuroraLib.Pixel.Formats.Common
{
    public sealed partial class DDS
    {
        private struct DXT10Header
        {
            /// <summary>
            /// The surface pixel format
            /// </summary>
            public DXGIFormats Format;
            /// <summary>
            /// Identifies the type of resource.
            /// </summary>
            public ResourceDimensionType ResourceDimension;
            /// <summary>
            /// Identifies other, less common options for resources.
            /// </summary>
            public MiscFlags Misc;
            /// <summary>
            /// The number of elements in the array.
            /// </summary>
            public uint ArraySize;
            /// <summary>
            /// Contains additional metadata (formerly was reserved). The lower 3 bits indicate the alpha mode of the associated resource. The upper 29 bits are reserved and are typically 0.
            /// </summary>
            public Misc2Flags Misc2;

            public DXT10Header(Header header, DXGIFormats format, Misc2Flags misc2)
            {
                Format = format;

                if (header.Height == 1)
                    ResourceDimension = ResourceDimensionType.TEXTURE1D;
                else if (header.Caps2 == Header.Caps2Flag.Volume)
                    ResourceDimension = ResourceDimensionType.TEXTURE3D;
                else
                    ResourceDimension = ResourceDimensionType.TEXTURE2D;

                Misc = header.Caps2.HasFlag(Header.Caps2Flag.Cubemap) ? MiscFlags.TEXTURECUBE : MiscFlags.None;
                ArraySize = 1;
                Misc2 = misc2;
            }

            public enum ResourceDimensionType : uint
            {
                None = 0x0,
                /// <summary>
                /// Resource is a buffer.
                /// </summary>
                BUFFER = 1,
                /// <summary>
                /// Resource is a 1D texture
                /// </summary>
                TEXTURE1D = 2,
                /// <summary>
                /// Resource is a 2D texture
                /// </summary>
                TEXTURE2D = 3,
                /// <summary>
                /// Resource is a 3D texture, <see cref="Header.Caps2Flag.Volume"/>
                /// </summary>
                TEXTURE3D = 4
            }

            [Flags]
            public enum MiscFlags : uint
            {
                None = 0x0,
                /// <summary>
                /// Indicates a 2D texture is a cube-map texture. <see cref="Header.Caps2Flag.Cubemap"/>
                /// </summary>
                TEXTURECUBE = 0x4,
            }

            public enum Misc2Flags : uint
            {
                /// <summary>
                /// Alpha channel content is unknown. This is the value for legacy files, which typically is assumed to be 'straight' alpha.
                /// </summary>
                None = 0x0,
                /// <summary>
                /// Any alpha channel content is presumed to use straight alpha.
                /// </summary>
                STRAIGHT = 0x1,
                /// <summary>
                /// Any alpha channel content is using premultiplied alpha. The only legacy file formats that indicate this information are 'DX2' and 'DX4'.
                /// </summary>
                PREMULTIPLIED = 0x2,
                /// <summary>
                /// Any alpha channel content is all set to fully opaque.
                /// </summary>
                OPAQUE = 0x3,
                /// <summary>
                /// Any alpha channel content is being used as a 4th channel and is not intended to represent transparency (straight or premultiplied).
                /// </summary>
                CUSTOM = 0x4,
            }
        }
    }
}
