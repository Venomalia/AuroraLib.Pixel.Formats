using AuroraLib.Pixel;
using AuroraLib.Pixel.Formats.Common.Structs;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace AuroraPixel.ImageFormats
{
    public sealed partial class BMP
    {
        private class InfoHeader
        {
            /// <summary>
            /// Size of the header, indicating the version of the DIB header.
            /// </summary>
            public InfoHeaderVersionSize HeaderSize;

            /// <summary>
            /// The width of the bitmap in pixels.
            /// </summary>
            public int Width;

            /// <summary>
            /// The height of the bitmap in pixels. Positive for bottom-up bitmaps, negative for top-down.
            /// </summary>
            private int height;

            /// <summary>
            /// The height of the bitmap in pixels.
            /// </summary>
            public int Height
            {
                get => Math.Abs(height);
                set => height = IsTopDown ? -Math.Abs(value) : Math.Abs(value);
            }

            /// <summary>
            /// Gets or sets a value indicating whether the bitmap is stored top-down.
            /// If true, the first row of the image data is the top row.
            /// </summary>
            public bool IsTopDown
            {
                get => height < 0;
                set => height = Height * (value ? -1 : 1);
            }

            /// <summary>
            /// Number of color planes. Must be set to 1.
            /// </summary>
            public ushort Planes;

            /// <summary>
            /// Number of bits per pixel, indicating color depth.
            /// </summary>
            public ushort BitsPerPixel;

            /// <summary>
            /// Compression method used for the image data.
            /// </summary>
            public BmpCompression Compression;

            /// <summary>
            /// The size of the image data in bytes. Can be 0 for <see cref="BmpCompression.BI_RGB"/> and <see cref="BmpCompression.BI_BITFIELDS"/>.
            /// </summary>
            public uint ImageSize;

            /// <summary>
            /// The horizontal resolution of the image, in pixels per meter.
            /// </summary>
            public int XPixelsPerMeter;

            /// <summary>
            /// The vertical resolution of the image, in pixels per meter.
            /// </summary>
            public int YPixelsPerMeter;

            /// <summary>
            /// The number of colors used in the bitmap. If set to 0, the maximum number of colors is used.
            /// </summary>
            public int ColorsUsed;

            /// <summary>
            /// The number of color indices considered important for display. If 0, all are important.
            /// </summary>
            public int ImportantColors;

            // DIB v2

            /// <summary>
            /// Color mask for the red channel (only if <see cref="BmpCompression.BI_BITFIELDS"/> or <see cref="BmpCompression.BI_ALPHABITFIELDS"/> is used).
            /// </summary>
            public uint RedMask;

            /// <summary>
            /// Color mask for the green channel (only if <see cref="BmpCompression.BI_BITFIELDS"/> or <see cref="BmpCompression.BI_ALPHABITFIELDS"/> is used).
            /// </summary>
            public uint GreenMask;

            /// <summary>
            /// Color mask for the blue channel (only if <see cref="BmpCompression.BI_BITFIELDS"/> or <see cref="BmpCompression.BI_ALPHABITFIELDS"/> is used).
            /// </summary>
            public uint BlueMask;

            // DIB v3

            /// <summary>
            /// Color mask for the alpha channel (transparency) (only if <see cref="BmpCompression.BI_ALPHABITFIELDS"/> is used).
            /// </summary>
            public uint AlphaMask;

            public PixelFormatInfo FormatInfo
            {
                get => new PixelFormatInfo((byte)BitsPerPixel, RedMask, GreenMask, BlueMask, AlphaMask);
                set
                {
                    BitsPerPixel = value.BitsPerPixel;
                    RedMask = (uint)value.RedChannelInfo.Mask;
                    GreenMask = (uint)value.GreenChannelInfo.Mask;
                    BlueMask = (uint)value.BlueChannelInfo.Mask;
                    AlphaMask = (uint)value.AlphaChannelInfo.Mask;
                }
            }

            // DIB v4

            /// <summary>
            /// Defines the color space of the bitmap
            /// </summary>
            public BmpColorSpaceType ColorSpaceType;

            /// <summary>
            /// The CIE XYZ coordinates of the three colors (red, green, blue) that make up the color space.
            /// </summary>
            public CIEXYZTRIPLE Endpoints;

            /// <summary>
            /// Gamma red coordinate scale value, used for color correction.
            /// </summary>
            public uint GammaRed;

            /// <summary>
            /// Gamma green coordinate scale value.
            /// </summary>
            public uint GammaGreen;

            /// <summary>
            /// Gamma blue coordinate scale value.
            /// </summary>
            public uint GammaBlue;

            // DIB v5
            /// <summary>
            /// Rendering intent. Specifies how colors should be matched to the output device.
            /// </summary>
            public uint Intent;

            /// <summary>
            /// Offset in bytes from the beginning of the DIB header to the start of the profile data.
            /// </summary>
            public uint ProfileData;

            /// <summary>
            /// Size in bytes of the embedded color profile data.
            /// </summary>
            public uint ProfileSize;

            public uint Reserved;

            public InfoHeader(InfoHeaderVersionSize headerSize, int width, int height, ushort bitCount, BmpCompression compression, uint imageSize = 0, int colorsUsed = 0)
            {
                HeaderSize = headerSize;
                Width = width;
                this.height = height;
                Planes = 1;
                BitsPerPixel = bitCount;
                Compression = compression;
                ImageSize = imageSize;
                XPixelsPerMeter = YPixelsPerMeter = 6000;
                ColorsUsed = colorsUsed;
                ImportantColors = ColorsUsed;
                ColorSpaceType = BmpColorSpaceType.LCS_sRGB;
            }

            public InfoHeader(Stream stream)
            {
                HeaderSize = (InfoHeaderVersionSize)(stream.ReadByte() | stream.ReadByte() << 8 | stream.ReadByte() << 16 | stream.ReadByte() << 24);
                if (HeaderSize < InfoHeaderVersionSize.WinV2 || (int)HeaderSize > byte.MaxValue)
                    throw new ArgumentException($"Invalid BMP.InfoHeader Size {HeaderSize}.");

#if NET6_0_OR_GREATER
                Span<byte> data = stackalloc byte[(int)HeaderSize-4];
                stream.Read(data);
#else
                byte[] dataArray = new byte[(int)HeaderSize-4];
                stream.Read(dataArray, 0, dataArray.Length);
                Span<byte> data = dataArray;
#endif

                if (HeaderSize == InfoHeaderVersionSize.WinV2)
                {
                    Width = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(0, 2));
                    height = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2, 2));
                    Planes = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(4, 2));
                    BitsPerPixel = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6, 2));
                    return;
                }

                Width = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(0, 4));
                height = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(4, 4));
                Planes = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(8, 2));
                BitsPerPixel = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(10, 2));

                if (HeaderSize < InfoHeaderVersionSize.IBMV2Short)
                    return;

                if (HeaderSize == InfoHeaderVersionSize.IBMV2)
                {
                    Compression = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(12, 4)) switch
                    {
                        0 => BmpCompression.BI_RGB,
                        1 => BmpCompression.BI_RLE8,
                        2 => BmpCompression.BI_RLE4,
                        // 3 => Huffman 1D
                        4 => BmpCompression.RLE24,
                        _ => throw new NotSupportedException(),
                    };
                }
                else
                {
                    Compression = (BmpCompression)BinaryPrimitives.ReadInt32LittleEndian(data.Slice(12, 4));
                }
                ImageSize = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(16, 4));
                XPixelsPerMeter = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(20, 4));
                YPixelsPerMeter = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(24, 4));
                ColorsUsed = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(28, 4));
                ImportantColors = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(32, 4));


                if (HeaderSize < InfoHeaderVersionSize.AdobeV3)
                    return;

                RedMask = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(36, 4));
                GreenMask = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(40, 4));
                BlueMask = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(44, 4));

                if (HeaderSize < InfoHeaderVersionSize.AdobeV3WithAlpha)
                    return;

                AlphaMask = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(48, 4));

                if (HeaderSize < InfoHeaderVersionSize.WinV4)
                    return;

                ColorSpaceType = (BmpColorSpaceType)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(52, 4));
                Endpoints = MemoryMarshal.Read<CIEXYZTRIPLE>(data.Slice(56, 36));
                GammaRed = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(92, 4));
                GammaGreen = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(96, 4));
                GammaBlue = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(100, 4));

                if (HeaderSize < InfoHeaderVersionSize.WinV5)
                    return;

                Intent = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(104, 4));
                ProfileData = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(108, 4));
                ProfileSize = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(112, 4));
                Reserved = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(116, 4));

            }

            public int GetCalculatedstride() => BMP.GetCalculatedstride(Width, BitsPerPixel);

            public void Write(Stream stream)
            {
#if NET6_0_OR_GREATER
                Span<byte> data = stackalloc byte[(int)HeaderSize];
#else
                byte[] dataArray = new byte[(int)HeaderSize];
                Span<byte> data = dataArray;
#endif
                BinaryPrimitives.WriteInt32LittleEndian(data.Slice(0, 4), (int)HeaderSize);

                if (HeaderSize == InfoHeaderVersionSize.WinV2)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(data.Slice(4, 2), (ushort)Width);
                    BinaryPrimitives.WriteUInt16LittleEndian(data.Slice(6, 2), (ushort)height);
                    BinaryPrimitives.WriteUInt16LittleEndian(data.Slice(8, 2), Planes);
                    BinaryPrimitives.WriteUInt16LittleEndian(data.Slice(10, 2), BitsPerPixel);
                }
                else
                {
                    BinaryPrimitives.WriteInt32LittleEndian(data.Slice(4, 4), Width);
                    BinaryPrimitives.WriteInt32LittleEndian(data.Slice(8, 4), height);
                    BinaryPrimitives.WriteUInt16LittleEndian(data.Slice(12, 2), Planes);
                    BinaryPrimitives.WriteUInt16LittleEndian(data.Slice(14, 2), BitsPerPixel);


                    if (HeaderSize > InfoHeaderVersionSize.IBMV2Short)
                    {
                        BinaryPrimitives.WriteInt32LittleEndian(data.Slice(16, 4), (int)Compression);
                        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(20, 4), ImageSize);
                        BinaryPrimitives.WriteInt32LittleEndian(data.Slice(24, 4), XPixelsPerMeter);
                        BinaryPrimitives.WriteInt32LittleEndian(data.Slice(28, 4), YPixelsPerMeter);
                        BinaryPrimitives.WriteInt32LittleEndian(data.Slice(32, 4), ColorsUsed);
                        BinaryPrimitives.WriteInt32LittleEndian(data.Slice(36, 4), ImportantColors);
                    }

                    // DIB v2
                    if (HeaderSize >= InfoHeaderVersionSize.AdobeV3)
                    {
                        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(40, 4), RedMask);
                        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(44, 4), GreenMask);
                        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(48, 4), BlueMask);
                    }

                    // DIB v3
                    if (HeaderSize >= InfoHeaderVersionSize.AdobeV3WithAlpha)
                    {
                        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(52, 4), AlphaMask);
                    }

                    // DIB v4
                    if (HeaderSize >= InfoHeaderVersionSize.WinV4)
                    {
                        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(56, 4), (uint)ColorSpaceType);
                        MemoryMarshal.Write(data.Slice(60, 36), ref Endpoints);
                        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(96, 4), GammaRed);
                        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(100, 4), GammaGreen);
                        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(104, 4), GammaBlue);
                    }

                    // DIB v5
                    if (HeaderSize >= InfoHeaderVersionSize.WinV5)
                    {
                        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(108, 4), Intent);
                        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(112, 4), ProfileData);
                        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(116, 4), ProfileSize);
                        BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(120, 4), Reserved);
                    }

                }

#if NET6_0_OR_GREATER
                stream.Write(data);
#else
                stream.Write(dataArray, 0, dataArray.Length);
#endif
            }
        }
    }
}
