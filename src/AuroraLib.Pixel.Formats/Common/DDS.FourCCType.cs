using AuroraLib.Pixel.BlockProcessor;

namespace AuroraLib.Pixel.Formats.Common
{
    public sealed partial class DDS
    {
        /// <summary>
        /// Four-character codes for specifying compressed or custom formats.
        /// </summary>
        private enum FourCCType : uint
        {
            None = 0x0,
            /// <summary>Identical to <see cref="BC1Block{TColor}"/> </summary>
            DXT1 = 827611204,
            /// <summary>Identical to <see cref="BC2Block{TColor}"/> with premultiplied alpha.</summary>
            DXT2 = 844388420,
            /// <summary>Identical to <see cref="BC2Block{TColor}"/> </summary>
            DXT3 = 861165636,
            /// <summary>Identical to <see cref="BC3Block{TColor}"/> with premultiplied alpha.</summary>
            DXT4 = 877942852,
            /// <summary>Identical to <see cref="BC3Block{TColor}"/>.</summary>
            DXT5 = 894720068,

            /// <summary>
            /// Has a <see cref="DXT10Header"/>
            /// </summary>
            DX10 = 808540228,

            /// <summary>Identical to <see cref="BC4UBlock{TColor}"/> </summary>
            BC4U = 1429488450,
            /// <summary>Identical to <see cref="BC4SBlock{TColor}"/> </summary>
            BC4S = 1395934018,
            /// <summary>Identical to <see cref="BC5UBlock{TColor}"/> </summary>
            BC5U = 1429553986,
            /// <summary>Identical to <see cref="BC5SBlock{TColor}"/> </summary>
            BC5S = 1395999554,
            /// <summary>Identical to <see cref="BC4UBlock{TColor}"/> </summary>
            ATI1 = 826889281,
            /// <summary>Identical to <see cref="BC5UBlock{TColor}"/> </summary>
            ATI2 = 843666497,

            //RGBA32
            RGBG = 1195525970,
            //RGBA32
            GRGB = 1111970375,
            UYVY = 1498831189,
            YUY2 = 844715353,
            MET1 = 827606349,
            //RGBA64
            R16G16B16A16_UNORM = 36,
            R16G16B16A16_SNORM = 110,
            R16G16B16A16_FLOAT = 113,
            R16_FLOAT = 111,
            R16G16_FLOAT = 112,
            R32_FLOAT = 114,
            R32G32_FLOAT = 115,
            R32G32B32A32_FLOAT = 116,
            CxV8U8 = 117,
        }

    }
}
