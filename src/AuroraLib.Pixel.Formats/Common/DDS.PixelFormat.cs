using System;

namespace AuroraLib.Pixel.Formats.Common
{
    public sealed partial class DDS
    {
        private struct PixelFormat
        {
            /// <summary>
            /// Size of structure.This member must be set to 32 (bytes).
            /// </summary>
            public uint HeaderSize;
            /// <summary>
            /// Values which indicate what type of data is in the surface.
            /// </summary>
            public Flags Flag;
            /// <summary>
            /// Four-character codes for specifying compressed or custom formats.
            /// Possible values include: DXT1, DXT2, DXT3, DXT4, or DXT5. A FourCC of DX10 indicates the prescense of the DDS_HEADER_DXT10 extended header, and the dxgiFormat member of that structure indicates the true format.
            /// When using a four-character code, Flags must include <see cref="Flags.FourCC"/>.
            /// </summary>
            public FourCCType FourCC;
            /// <summary>
            /// Number of bits in an RGB (possibly including alpha) format. Valid when <see cref="Flag"/> includes <see cref="Flags.RGB"/>, <see cref="Flags.Luminance"/>, or <see cref="Flags.YUV"/>.
            /// </summary>
            public uint RGBBitCount;
            /// <summary>
            /// Red (or luminance or Y) mask for reading color data. For instance, given the A8R8G8B8 format, the red mask would be 0x00ff0000.
            /// </summary>
            public uint RBitMask;
            /// <summary>
            /// Green (or U) mask for reading color data. For instance, given the A8R8G8B8 format, the green mask would be 0x0000ff00.
            /// </summary>
            public uint GBitMask;
            /// <summary>
            /// Blue (or V) mask for reading color data. For instance, given the A8R8G8B8 format, the blue mask would be 0x000000ff.
            /// </summary>
            public uint BBitMask;
            /// <summary>
            /// Alpha mask for reading alpha data. <see cref="Flag"/> must include <see cref="Flags.AlphaPixels"/> or <see cref="Flags.Alpha"/>. For instance, given the A8R8G8B8 format, the alpha mask would be 0xff000000.
            /// </summary>
            public uint ABitMask;

            public PixelFormat(FourCCType fourCC) : this()
            {
                HeaderSize = 32;
                FourCC = fourCC;
            }

            public PixelFormat(PixelFormatInfo FormatInfo)
            {
                if (FormatInfo.Type != PixelFormatInfo.ChannelType.Unsigned)
                    throw new NotSupportedException();

                if (FormatInfo.BitsPerPixel > 32)
                    throw new NotSupportedException();

                HeaderSize = 32;
                FourCC = FourCCType.None;

                Flag = FormatInfo.ColorSpace switch
                {
                    PixelFormatInfo.ColorSpaceType.RGB => Flags.RGB,
                    PixelFormatInfo.ColorSpaceType.YUV => Flags.YUV,
                    _ => throw new NotSupportedException(FormatInfo.ColorSpace.ToString()),
                };

                if (FormatInfo.IsGrayscale)
                    Flag |= Flags.Luminance;

                if (FormatInfo.HasAlpha)
                    Flag |= Flags.AlphaPixels;

                RGBBitCount = FormatInfo.BitsPerPixel;
                RBitMask = (uint)FormatInfo.RedChannelInfo.Mask;
                GBitMask = (uint)FormatInfo.GreenChannelInfo.Mask;
                BBitMask = (uint)FormatInfo.BlueChannelInfo.Mask;
                ABitMask = (uint)FormatInfo.AlphaChannelInfo.Mask;
            }

            [Flags]
            public enum Flags : uint
            {
                /// <summary>
                /// None.
                /// </summary>
                None = 0,

                /// <summary>
                /// Texture contains alpha data; ABitMask contains valid data.
                /// </summary>
                AlphaPixels = 0x1,

                /// <summary>
                /// Used in some older DDS files for alpha channel only uncompressed data (RGBBitCount contains the alpha channel bitcount; ABitMask contains valid data)
                /// </summary>
                Alpha = 0x2,

                /// <summary>
                /// Texture contains compressed RGB data; FourCC contains valid data.
                /// </summary>
                FourCC = 0x4,

                /// <summary>
                /// Texture contains uncompressed RGB data; RGBBitCount and the RGB masks (dwRBitMask, dwRBitMask, dwRBitMask) contain valid data.
                /// </summary>
                RGB = 0x40,

                /// <summary>
                /// Texture contains uncompressed RGB and alpha data; RGBBitCount and all of the masks (RBitMask, RBitMask, RBitMask, RGBAlphaBitMask) contain valid data.
                /// </summary>
                RGBA = RGB | AlphaPixels,

                /// <summary>
                /// Used in some older DDS files for YUV uncompressed data (RGBBitCount contains the YUV bit count; RBitMask contains the Y mask, GBitMask contains the U mask, BBitMask contains the V mask)
                /// </summary>
                YUV = 0x200,

                /// <summary>
                /// Used in some older DDS files for single channel color uncompressed data (RGBBitCount contains the luminance channel bit count; RBitMask contains the channel mask). Can be combined with DDPF_ALPHAPIXELS for a two channel DDS file.
                /// </summary>
                Luminance = 0x20000
            }

            public PixelFormatInfo FormatInfo => Flag.HasFlag(Flags.Luminance)
                   ? new PixelFormatInfo((byte)RGBBitCount, RBitMask, RBitMask, RBitMask, ABitMask)
                   : new PixelFormatInfo((byte)RGBBitCount, RBitMask, GBitMask, BBitMask, ABitMask, Flag.HasFlag(Flags.YUV) ? PixelFormatInfo.ColorSpaceType.YUV : PixelFormatInfo.ColorSpaceType.RGB);
        }
    }
}
