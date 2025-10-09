using AuroraLib.Core.Format;
using AuroraLib.Core.IO;
using AuroraLib.Pixel;
using AuroraLib.Pixel.Formats;
using AuroraLib.Pixel.Formats.Common.Structs;
using AuroraLib.Pixel.Image;
using AuroraLib.Pixel.Metadata;
using AuroraLib.Pixel.PixelFormats;
using AuroraLib.Pixel.Processing;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABGR32 = AuroraLib.Pixel.PixelFormats.ABGR<byte>;
using ARGB32 = AuroraLib.Pixel.PixelFormats.ARGB<byte>;
using BGR24 = AuroraLib.Pixel.PixelFormats.BGR<byte>;
using BGRA32 = AuroraLib.Pixel.PixelFormats.BGRA<byte>;
using I8 = AuroraLib.Pixel.PixelFormats.I<byte>;
using RGBA32 = AuroraLib.Pixel.PixelFormats.RGBA<byte>;

namespace AuroraPixel.ImageFormats
{
    /// <summary>
    /// The BMP file format, or bitmap, is a raster graphics image file format by Microsoft.
    /// </summary>
    public sealed partial class BMP : IImageFormat
    {
        /// <inheritdoc/>
        public IFormatInfo Info => _info;

        private static readonly IFormatInfo _info = new FormatInfo<BMP>("Microsoft Windows Bitmap", new MediaType(MIMEType.Image, "bmp"), ".bmp");

        /// <inheritdoc/>
        public bool IsMatch(Stream stream, ReadOnlySpan<char> fileNameAndExtension = default)
            => IsMatchStatic(stream, fileNameAndExtension);

        /// <inheritdoc cref="IsMatch(Stream, ReadOnlySpan{char})"/>
        public static bool IsMatchStatic(Stream stream, ReadOnlySpan<char> fileNameAndExtension = default)
        {
            if (stream.Length - stream.Position < 0x10)
                return false;
            var fileHeader = stream.Read<FileHeader>();
            return Enum.IsDefined(typeof(Signatures), fileHeader.Signature) && stream.Length >= fileHeader.FileSize;
        }

        /// <summary>
        /// Get or sets the file identifier used during encoding.
        /// </summary>
        public Signatures Signatur { get; set; } = Signatures.Bitmap;

        /// <summary>
        /// Get or sets the bmp version used during encoding.
        /// </summary>
        public InfoHeaderVersionSize Version { get; set; } = InfoHeaderVersionSize.AdobeV3WithAlpha;

        #region ReadImage (Decode)
        /// <inheritdoc/>
        public IImage ReadImage(Stream stream)
        {
            long headerStart = stream.Position;
            var fileHeader = stream.Read<FileHeader>();
            InfoHeader infoHeader = new InfoHeader(stream);

            stream.Position = headerStart + fileHeader.Offset;
            IImage image = infoHeader.Compression switch
            {
                BmpCompression.BI_RGB => infoHeader.BitsPerPixel switch
                {
                    32 => ApplyPixels(ReadImageData<BGRA32>(stream, infoHeader), SetAlphaMax),
                    24 => ReadImageData<BGR24>(stream, infoHeader),
                    16 => ReadImageData<RGB555>(stream, infoHeader),
                    _ => ReadPaletteImage(stream, infoHeader, fileHeader),
                },
                //BmpCompression.BI_RLE8 or BmpCompression.BI_RLE4 => throw new NotSupportedException(),
#if NET6_0_OR_GREATER

                BmpCompression.BI_BITFIELDS or BmpCompression.BI_ALPHABITFIELDS => ReadBitfieldImage(stream, infoHeader),
#else
                BmpCompression.BI_BITFIELDS => ReadBitfieldImage(stream, infoHeader),
                BmpCompression.BI_ALPHABITFIELDS => ReadBitfieldImage(stream, infoHeader),
#endif
                //BmpCompression.BI_JPEG => throw new NotImplementedException(),
                //BmpCompression.BI_PNG => throw new NotImplementedException(),
                //BmpCompression.BI_CMYK => throw new NotImplementedException(),
                //BmpCompression.BI_CMYKRLE8 => throw new NotImplementedException(),
                //BmpCompression.BI_CMYKRLE4 => throw new NotImplementedException(),
                //BmpCompression.RLE24 => throw new NotImplementedException(),
                _ => throw new NotImplementedException($"Compression type {infoHeader.Compression} is not supported."),
            };

            image.Metadata = new AuroraLib.Pixel.Metadata.ImageMetadata();
            Vector2 pixelsPerMeter = new Vector2(infoHeader.XPixelsPerMeter, infoHeader.YPixelsPerMeter);
            image.Metadata.PixelsPerCentimeter = pixelsPerMeter / 100f;

            if (infoHeader.HeaderSize >= InfoHeaderVersionSize.WinV4)
            {
                switch (infoHeader.ColorSpaceType)
                {
                    case BmpColorSpaceType.LCS_CalibratedRGB:
                        image.Metadata.ColorSpace = infoHeader.Endpoints.ToColorSpace();
                        image.Metadata.Gamma = (infoHeader.GammaRed + infoHeader.GammaBlue + infoHeader.GammaGreen) / 65536f / 3f;
                        break;
                    case BmpColorSpaceType.LCS_sRGB:
                    case BmpColorSpaceType.LCS_WindowsColorSpace:
                        image.Metadata.ColorSpace = AuroraLib.Pixel.Metadata.ColorSpace.sRGB;
                        break;
                    case BmpColorSpaceType.ProfileEmbedded:
                    case BmpColorSpaceType.ProfileLinked:
                    default:
                        break;
                }

                if (infoHeader.ProfileData != 0 && infoHeader.ProfileSize != 0)
                {
                    var icc = new byte[infoHeader.ProfileSize];
                    stream.At(headerStart + Unsafe.SizeOf<FileHeader>() + infoHeader.ProfileData, s => s.Read(icc, 0, icc.Length));
                    image.Metadata.Icc = icc;
                }
            }
            return image;
        }

        public delegate void PixelProcessor<TColor>(Span<TColor> pixels) where TColor : unmanaged, IColor<TColor>;

        private static void SetAlphaMax<TColor>(Span<TColor> pixels) where TColor : unmanaged, IColor<TColor>, IAlpha<byte>
        {
            for (int i = 0; i < pixels.Length; i++)
                pixels[i].A = byte.MaxValue;
        }

        private static IImage<TColor> ApplyPixels<TColor>(IImage<TColor> image, PixelProcessor<TColor> operation) where TColor : unmanaged, IColor<TColor>
        {
            if (image is IPaletteImage<TColor> p)
            {
                p.GetUsedPaletteRange(out int start, out int length);
                operation(p.Palette.Span.Slice(start, length));
            }
            else if (image is IDirectRowAccess<TColor> rowAccess)
            {
                for (int y = 0; y < image.Height; y++)
                    operation(rowAccess.GetWritableRow(y));
            }
            else
            {
                Span<TColor> row = stackalloc TColor[image.Width];

                for (int y = 0; y < image.Height; y++)
                {
                    image.GetPixel(0, y, row);
                    operation(row);
                    image.SetPixel(0, y, row);
                }
            }

            return image;
        }
        private static IImage ReadBitfieldImage(Stream stream, InfoHeader infoHeader)
        {
            PixelFormatInfo formatInfo = infoHeader.FormatInfo;
            switch (infoHeader.BitsPerPixel) // fast nativ read
            {
                case 32:
                    if (formatInfo.Equals(BGRA32.FormatInfo)) return ReadImageData<BGRA32>(stream, infoHeader);
                    if (formatInfo.Equals(RGBA32.FormatInfo)) return ReadImageData<RGBA32>(stream, infoHeader);
                    if (formatInfo.Equals(ARGB32.FormatInfo)) return ReadImageData<ARGB32>(stream, infoHeader);
                    if (formatInfo.Equals(ABGR32.FormatInfo)) return ReadImageData<ABGR32>(stream, infoHeader);
                    if (formatInfo.Equals(BGRA1010102.FormatInfo)) return ReadImageData<BGRA1010102>(stream, infoHeader);
                    if (formatInfo.Equals(RGBA1010102.FormatInfo)) return ReadImageData<RGBA1010102>(stream, infoHeader);
                    return ReadBitfieldImage32bit(stream, infoHeader);
                case 16:
                    if (formatInfo.Equals(BGR555.FormatInfo)) return ReadImageData<BGR555>(stream, infoHeader);
                    if (formatInfo.Equals(RGB555.FormatInfo)) return ReadImageData<RGB555>(stream, infoHeader);
                    if (formatInfo.Equals(RGB565.FormatInfo)) return ReadImageData<RGB565>(stream, infoHeader);
                    if (formatInfo.Equals(ARGB1555.FormatInfo)) return ReadImageData<ARGB1555>(stream, infoHeader);
                    if (formatInfo.Equals(ARGB16.FormatInfo)) return ReadImageData<ARGB16>(stream, infoHeader);
                    if (formatInfo.Equals(RGBA16.FormatInfo)) return ReadImageData<RGBA16>(stream, infoHeader);
                    return ReadBitfieldImage16bit(stream, infoHeader);
                default:
                    throw new NotSupportedException();
            }
        }

        private static MemoryImage<RGBA32> ReadBitfieldImage32bit(Stream stream, InfoHeader infoHeader)
        {
            MemoryImage<RGBA32> image = ReadImageData<RGBA32>(stream, infoHeader);

            ChannelInfo RedInfo = new ChannelInfo(infoHeader.RedMask);
            ChannelInfo GreenInfo = new ChannelInfo(infoHeader.GreenMask);
            ChannelInfo BlueInfo = new ChannelInfo(infoHeader.BlueMask);
            ChannelInfo AlphaInfo = new ChannelInfo(infoHeader.AlphaMask);
            bool hasAlpha = infoHeader.AlphaMask != 0;

            Span<RGBA32> pixel = image.Pixel;
            for (int i = 0; i < pixel.Length; i++)
            {
                uint data = pixel[i];
                pixel[i].R = Extract(data, RedInfo);
                pixel[i].G = Extract(data, GreenInfo);
                pixel[i].B = Extract(data, BlueInfo);
                pixel[i].A = hasAlpha ? Extract(data, AlphaInfo) : byte.MaxValue;
            }
            return image;
        }

        private static MemoryImage<RGBA32> ReadBitfieldImage16bit(Stream stream, InfoHeader infoHeader)
        {
            using MemoryImage<RGB565> image16bit = ReadImageData<RGB565>(stream, infoHeader);
            MemoryImage<RGBA32> image32bit = new MemoryImage<RGBA32>(infoHeader.Width, infoHeader.Height);

            ChannelInfo RedInfo = new ChannelInfo(infoHeader.RedMask);
            ChannelInfo GreenInfo = new ChannelInfo(infoHeader.GreenMask);
            ChannelInfo BlueInfo = new ChannelInfo(infoHeader.BlueMask);
            ChannelInfo AlphaInfo = new ChannelInfo(infoHeader.AlphaMask);
            bool hasAlpha = infoHeader.AlphaMask != 0;

            Span<RGB565> pixel16bit = image16bit.Pixel;
            Span<RGBA32> pixel32bit = image32bit.Pixel;
            for (int i = 0; i < pixel16bit.Length; i++)
            {
                uint data = pixel16bit[i];
                pixel32bit[i].R = Extract(data, RedInfo);
                pixel32bit[i].G = Extract(data, GreenInfo);
                pixel32bit[i].B = Extract(data, BlueInfo);
                pixel32bit[i].A = hasAlpha ? Extract(data, AlphaInfo) : byte.MaxValue;
            }
            return image32bit;
        }

        static byte Extract(uint data, ChannelInfo info)
        {
            int maxValue = (1 << info.BitDepth) - 1;
            int extractedValue = (int)((data & info.Mask) >> info.Shift);
            return (byte)((extractedValue * 255f) / maxValue);
        }

        private static IImage ReadPaletteImage(Stream stream, InfoHeader infoHeader, FileHeader fileHeader)
        {
            int Colors = infoHeader.ColorsUsed == 0 ? 1 << infoHeader.BitsPerPixel : infoHeader.ColorsUsed;
            int paletteSize = (int)(fileHeader.Offset - (int)infoHeader.HeaderSize - Unsafe.SizeOf<FileHeader>());
            int bytesPerColor = fileHeader.Signature == Signatures.Bitmap ? Math.Max(3, paletteSize / Colors) : 3;

            stream.Position = fileHeader.Offset - paletteSize;
#if NET6_0_OR_GREATER
            Span<byte> palette = stackalloc byte[paletteSize];
            stream.Read(palette);
#else
            byte[] palette = new byte[paletteSize];
            stream.Read(palette, 0, paletteSize);
#endif
            if (bytesPerColor == 3)
                return ReadPaletteImage<BGR24>(stream, infoHeader, MemoryMarshal.Cast<byte, BGR24>(palette));
            else
                return ReadPaletteImage<BGRA32>(stream, infoHeader, MemoryMarshal.Cast<byte, BGRA32>(palette));
        }

        private static IPaletteImage<TColor> ReadPaletteImage<TColor>(Stream stream, InfoHeader infoHeader, ReadOnlySpan<TColor> paletteColors) where TColor : unmanaged, IColor<TColor>, IRGB<byte>
        {
            MemoryImage<I8> image;
            if (infoHeader.BitsPerPixel == 8)
            {
                image = ReadImageData<I8>(stream, infoHeader);
            }
            else
            {
                int pixelsPerByte = 8 / infoHeader.BitsPerPixel;
                int rowBytes = (infoHeader.Width + pixelsPerByte - 1) / pixelsPerByte;
                int padding = (4 - (rowBytes % 4)) % 4;

                image = new MemoryImage<I8>(infoHeader.Width, infoHeader.Height);
                Span<byte> pixel = MemoryMarshal.Cast<I8, byte>(image.Pixel);

                byte[] buffer = ArrayPool<byte>.Shared.Rent(rowBytes + padding);

                try
                {
                    for (int y = 0; y < infoHeader.Height; y++)
                    {
                        int destY = infoHeader.IsTopDown ? y : infoHeader.Height - 1 - y;
                        int destIndex = destY * infoHeader.Width;

                        int bytesRead = stream.Read(buffer, 0, rowBytes + padding);
                        if (bytesRead < rowBytes + padding)
                            throw new EndOfStreamException();

                        ImageStreamHelper.UnpackPixels(buffer, pixel.Slice(destIndex), rowBytes, infoHeader.BitsPerPixel, infoHeader.Width);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            return new PaletteImage<I8, TColor>(image, paletteColors);
        }

        private static MemoryImage<TColor> ReadImageData<TColor>(Stream stream, InfoHeader infoHeader) where TColor : unmanaged, IColor<TColor>
        {
            if (infoHeader.IsTopDown)
                return ImageStreamHelper.ReadImage<TColor>(stream, infoHeader.Width, infoHeader.Height);

            int byteStride = infoHeader.GetCalculatedstride();

            MemoryImage<TColor> image = new MemoryImage<TColor>(infoHeader.Width, infoHeader.Height, byteStride / Unsafe.SizeOf<TColor>());

            Span<byte> pixelData = MemoryMarshal.Cast<TColor, byte>(image.Pixel);
            int height = image.Height;
#if NET6_0_OR_GREATER
            for (int i = height - 1; i >= 0; i--)
            {
                Span<byte> row = pixelData.Slice(i * byteStride, byteStride);
                if (stream.Read(row) != byteStride)
                    throw new EndOfStreamException();
            }
#else
            byte[] buffer = ArrayPool<byte>.Shared.Rent(byteStride);
            ReadOnlySpan<byte> bufferSpan = buffer.AsSpan(0, byteStride);
            try
            {

                for (int i = height - 1; i >= 0; i--)
                {
                    if (stream.Read(buffer, 0, byteStride) != byteStride)
                        throw new NotSupportedException();

                    bufferSpan.CopyTo(pixelData.Slice(i * byteStride, byteStride));
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
#endif
            return image;
        }

        internal static int GetCalculatedstride(int width, int bitsPerPixel) => ((width * bitsPerPixel + 31) / 32) * 4;

        #endregion

        #region WriteImage (Encode)
        /// <inheritdoc/>
        public void WriteImage<TColor>(IReadOnlyImage<TColor> source, Stream destination) where TColor : unmanaged, IColor<TColor>
        {
            if (source is IEnumerable<IImage<TColor>> images)
                source = images.First();

            FileHeader header = new FileHeader(Signatur, 0, (uint)Version + 14);
            InfoHeader infoHeader = new InfoHeader(Version, source.Width, -source.Height, (ushort)(Unsafe.SizeOf<TColor>() * 8), BmpCompression.BI_RGB);

            long start = destination.Position;
            uint imageOffset = header.Offset;

            // temporary
            destination.Write(header);
            infoHeader.Write(destination);

            var formatInfo = default(TColor).FormatInfo;
            if (source is IReadOnlyPaletteImage<TColor> paletteImage && paletteImage.Palette.Length <= 256)
            {
                if (header.Signature == Signatures.Bitmap && default(TColor).FormatInfo.HasAlpha)
                    imageOffset += WritePaletteImage<TColor, BGRA32>(paletteImage, destination, infoHeader);
                else
                    imageOffset += WritePaletteImage<TColor, BGR24>(paletteImage, destination, infoHeader);
            }
            else if (typeof(TColor) == typeof(BGR24) || typeof(TColor) == typeof(RGB555))
            {
                ImageStreamHelper.WriteImage(destination, source);
            }
            else if (InfoHeaderVersionSize.AdobeV3 > Version)
            {
                if (typeof(TColor) == typeof(BGRA32))
                {
                    ImageStreamHelper.WriteImage(destination, source);
                }
                else
                {
                    using var buffer = source.CloneAs<BGR24>();
                    ImageStreamHelper.WriteImage(destination, buffer);
                    infoHeader.BitsPerPixel = 24;
                }
            }
            else
            {
                if ((infoHeader.BitsPerPixel == 32 || infoHeader.BitsPerPixel == 16) && formatInfo.Type == PixelFormatInfo.ChannelType.Unsigned && formatInfo.ColorSpace == PixelFormatInfo.ColorSpaceType.RGB)
                {
                    infoHeader.Compression = BmpCompression.BI_BITFIELDS;
                    ImageStreamHelper.WriteImage(destination, source);
                }
                else
                {
                    if (formatInfo.HasAlpha && InfoHeaderVersionSize.AdobeV3WithAlpha <= Version)
                    {
                        infoHeader.Compression = BmpCompression.BI_BITFIELDS;
                        formatInfo = ((IColor)default(BGRA32)).FormatInfo;
                        using var buffer = source.CloneAs<BGRA32>();
                        ImageStreamHelper.WriteImage(destination, buffer);
                    }
                    else
                    {
                        formatInfo = ((IColor)default(BGR24)).FormatInfo;
                        using var buffer = source.CloneAs<BGR24>();
                        ImageStreamHelper.WriteImage(destination, buffer);
                    }
                }
                infoHeader.RedMask = (uint)formatInfo.RedChannelInfo.Mask;
                infoHeader.GreenMask = (uint)formatInfo.GreenChannelInfo.Mask;
                infoHeader.BlueMask = (uint)formatInfo.BlueChannelInfo.Mask;
                infoHeader.AlphaMask = (uint)formatInfo.AlphaChannelInfo.Mask;
                infoHeader.BitsPerPixel = formatInfo.BitsPerPixel;
            }

            if (source.Metadata != null)
            {
                infoHeader.XPixelsPerMeter = (int)(source.Metadata.PixelsPerCentimeter.X * 100);
                infoHeader.YPixelsPerMeter = (int)(source.Metadata.PixelsPerCentimeter.Y * 100);
                if (infoHeader.HeaderSize >= InfoHeaderVersionSize.WinV4)
                {
                    if (source.Metadata.ColorSpace != null && !ColorSpace.sRGB.Equals(source.Metadata.ColorSpace))
                    {
                        infoHeader.ColorSpaceType = BmpColorSpaceType.LCS_CalibratedRGB;
                        infoHeader.Endpoints = new CIEXYZTRIPLE(source.Metadata.ColorSpace);
                        uint gamma = (uint)(source.Metadata.Gamma * 65536);
                        infoHeader.GammaBlue = infoHeader.GammaGreen = infoHeader.GammaBlue = gamma;
                    }

                    var icc = source.Metadata.Icc;
                    if (icc != null)
                    {
                        infoHeader.ProfileData = (uint)(destination.Position - start - Unsafe.SizeOf<FileHeader>());
                        infoHeader.ProfileSize = (uint)icc.Length;
                        destination.Write(icc, 0, icc.Length);
                    }
                }
            }
            // update header
            header = new FileHeader(Signatur, (uint)(destination.Position - start), imageOffset);
            destination.At(start, s =>
            {
                destination.Write(header);
                infoHeader.Write(destination);
            });
        }

        private static uint WritePaletteImage<TColor, TColor2>(IReadOnlyPaletteImage<TColor> source, Stream destination, InfoHeader infoHeader) where TColor : unmanaged, IColor<TColor> where TColor2 : unmanaged, IColor<TColor2>
        {
            var paletteRef = source.PaletteRefCounts;
            int colorsUsed = 0, importantColors = 0;
            bool first = false;
            for (int i = paletteRef.Length - 1; i >= 0; i--)
            {
                if (paletteRef[i] != 0)
                {
                    importantColors++;
                    if (!first)
                    {
                        first = true;
                        colorsUsed = i + 1;
                    }
                }
            }
            infoHeader.ColorsUsed = colorsUsed;
            infoHeader.ImportantColors = importantColors;

            if (colorsUsed <= 4)
                infoHeader.BitsPerPixel = 2;
            else if (colorsUsed <= 16)
                infoHeader.BitsPerPixel = 4;
            else
                infoHeader.BitsPerPixel = 8;

            int Colors = infoHeader.HeaderSize < InfoHeaderVersionSize.IBMV2Short ? 1 << infoHeader.BitsPerPixel : colorsUsed;

            int paletteSize = Colors * Unsafe.SizeOf<TColor2>();
            byte[] palette = new byte[paletteSize];
            source.Palette.Slice(0, colorsUsed).To(MemoryMarshal.Cast<byte, TColor2>(palette.AsSpan()).Slice(0, colorsUsed));
            destination.Write(palette, 0, paletteSize);

            if (infoHeader.BitsPerPixel == 8 && source is IPaletteImage<TColor2> image && image.GetBuffer() is IReadOnlyImage<I8> bufferImage)
            {
                ImageStreamHelper.WriteImage(destination, bufferImage);
            }
            else
            {
                int pixelsPerByte = 8 / infoHeader.BitsPerPixel;
                int rowBytes = (infoHeader.Width + pixelsPerByte - 1) / pixelsPerByte;
                int padding = (4 - (rowBytes % 4)) % 4;

                byte[] buffer = ArrayPool<byte>.Shared.Rent(rowBytes + padding);

                for (int y = 0; y < source.Height; y++)
                {
                    int x = 0;
                    for (int i = 0; i < rowBytes; i++)
                    {
                        byte b = 0;

                        for (int shift = 8 - infoHeader.BitsPerPixel; shift >= 0; shift -= infoHeader.BitsPerPixel)
                        {
                            if (x >= source.Width)
                                break;

                            int index = source.GetPixelIndex(x++, y);
                            b |= (byte)(index << shift);
                        }
                        buffer[i] = b;
                    }

                    destination.Write(buffer, 0, rowBytes + padding);
                }
            }
            return (uint)paletteSize;
        }
        #endregion
    }
}
