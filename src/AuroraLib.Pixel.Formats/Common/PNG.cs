using AuroraLib.Core;
using AuroraLib.Core.Collections;
using AuroraLib.Core.Format;
using AuroraLib.Core.Format.Identifier;
using AuroraLib.Core.IO;
using AuroraLib.Pixel.Image;
using AuroraLib.Pixel.Metadata;
using AuroraLib.Pixel.PixelFormats;
using AuroraLib.Pixel.PixelProcessor.Helper;
using AuroraLib.Pixel.Processing;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Hashing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using I16 = AuroraLib.Pixel.PixelFormats.I<ushort>;
using I8 = AuroraLib.Pixel.PixelFormats.I<byte>;
using IA16 = AuroraLib.Pixel.PixelFormats.IA<byte>;
using IA32 = AuroraLib.Pixel.PixelFormats.IA<ushort>;
using RGB24 = AuroraLib.Pixel.PixelFormats.RGB<byte>;
using RGB48 = AuroraLib.Pixel.PixelFormats.RGB<ushort>;
using RGBA32 = AuroraLib.Pixel.PixelFormats.RGBA<byte>;
using RGBA64 = AuroraLib.Pixel.PixelFormats.RGBA<ushort>;
namespace AuroraLib.Pixel.Formats.Common
{
    /// <summary>
    /// Portable Network Graphics is a raster-graphics file format that supports lossless data compression.
    /// </summary>
    public sealed partial class PNG : IImageFormat
    {
        private static readonly Identifier64 _Identifier = new Identifier64(0x0A1A0A0D474E5089);

        /// <inheritdoc/>
        public IFormatInfo Info => _info;

        private static readonly IFormatInfo _info = new FormatInfo<PNG>("Portable Network Graphics", new MediaType(MIMEType.Image, "png"), ".png", _Identifier);

        /// <inheritdoc/>
        public bool IsMatch(Stream stream, ReadOnlySpan<char> fileNameAndExtension = default)
            => IsMatchStatic(stream, fileNameAndExtension);

        /// <inheritdoc cref="IsMatch(Stream, ReadOnlySpan{char})"/>
        public static bool IsMatchStatic(Stream stream, ReadOnlySpan<char> fileNameAndExtension = default)
            => stream.Length == 0x10 && stream.Peek(s => s.Match(_Identifier) && s.ReadUInt32(Endian.Big) == 0xD);

        #region ReadImage (Decode)
        /// <inheritdoc/>
        public IImage ReadImage(Stream source)
        {
            source.MatchThrow(_Identifier);
            uint chunkSize = source.ReadUInt32(Endian.Big);
            ChunkTypes chunk = (ChunkTypes)source.ReadUInt32();

            if (chunk != ChunkTypes.Header)
                throw new InvalidDataException("First chunk must be IHDR.");
            if (chunkSize != 0xD)
                throw new InvalidDataException($"IHDR chunk must be 13 bytes long, but was {chunkSize}.");

            IHeaderChunk header = source.Read<IHeaderChunk>();
            header.Validate();
            _ = source.ReadUInt32(Endian.Big); // CRC32

            using ConcatStream imageData = new ConcatStream();
            Span<RGB24> palette = Span<RGB24>.Empty;
            Span<byte> transparency = Span<byte>.Empty;
            ImageMetadata metadata = new ImageMetadata();

            while (true)
            {
                chunkSize = source.ReadUInt32(Endian.Big);
                chunk = (ChunkTypes)source.ReadUInt32();
                long chunkStart = source.Position;
                switch (chunk)
                {
                    case ChunkTypes.Header:
                        throw new InvalidDataException("Multiple IHDR chunks not allowed.");
                    case ChunkTypes.Palette:
                        byte[] paletteData = new byte[chunkSize];
                        source.Read(paletteData, 0, paletteData.Length);
                        palette = MemoryMarshal.Cast<byte, RGB24>(paletteData.AsSpan());
                        break;
                    case ChunkTypes.Data:
                        imageData.Enqueue(new SubStream(source, (int)chunkSize));
                        break;
                    case ChunkTypes.End:
                        _ = source.ReadUInt32(Endian.Big); // CRC32
                        long end = source.Position;
                        IImage image = DecodeImageData(header, imageData, palette, transparency);
                        image.Metadata = metadata;
                        source.Position = end;
                        return image;
                    case ChunkTypes.Transparency:
                        byte[] transparencyData = new byte[chunkSize];
                        source.Read(transparencyData, 0, transparencyData.Length);
                        transparency = transparencyData;
                        break;
                    case ChunkTypes.StandardRGB:
                        metadata.ColorSpace = ColorSpace.sRGB;
                        break;
                    case ChunkTypes.Gamma:
                        metadata.Gamma = source.ReadUInt32(Endian.Big) / 100000f;
                        break;
                    case ChunkTypes.Physical:
                        Vector2 pixelsPerUnit = new Vector2(source.ReadUInt32(Endian.Big), source.ReadUInt32(Endian.Big));
                        byte unit = source.ReadUInt8();
                        if (unit == 1)
                            metadata.PixelsPerCentimeter = pixelsPerUnit / 100;
                        else
                            metadata.PixelsPerInch = pixelsPerUnit;
                        break;
                    case ChunkTypes.Text:
                    case ChunkTypes.InternationalText:
                    case ChunkTypes.CompressedText:
                        byte[] textData = new byte[chunkSize];
                        source.Read(textData, 0, textData.Length);
                        AddTextChunk(metadata.Text, textData, chunk);
                        break;
                    case ChunkTypes.ICC_Profile:
                        byte[] icc = new byte[chunkSize];
                        source.Read(icc, 0, icc.Length);
                        metadata.Icc = icc;
                        break;
                    case ChunkTypes.EXIF_Metadata:
                        byte[] exif = new byte[chunkSize];
                        source.Read(exif, 0, exif.Length);
                        metadata.Exif = exif;
                        break;
                    case ChunkTypes.PrimaryChromaticities:
                        Vector2 whitePoint = new Vector2(source.ReadUInt32(Endian.Big), source.ReadUInt32(Endian.Big)) / 100000f;
                        Vector2 redPoint = new Vector2(source.ReadUInt32(Endian.Big), source.ReadUInt32(Endian.Big)) / 100000f;
                        Vector2 greenPoint = new Vector2(source.ReadUInt32(Endian.Big), source.ReadUInt32(Endian.Big)) / 100000f;
                        Vector2 bluePoint = new Vector2(source.ReadUInt32(Endian.Big), source.ReadUInt32(Endian.Big)) / 100000f;
                        metadata.ColorSpace = new ColorSpace(whitePoint, redPoint, greenPoint, bluePoint);
                        break;
                    case ChunkTypes.BackgroundColor:
                    case ChunkTypes.SignificantBits:
                    case ChunkTypes.SuggestedPalette:
                    case ChunkTypes.PaletteHistogram:
                    case ChunkTypes.LastModification:
                        break;
                    default:
                        Trace.WriteLine($"PNG parser encountered an unknown chunk type: {chunk}");
                        break;
                }
                source.Seek(chunkStart + chunkSize, SeekOrigin.Begin);
                _ = source.ReadUInt32(Endian.Big); // CRC32
            }

            static void AddTextChunk(Dictionary<string, string> pairs, byte[] data, ChunkTypes type)
            {
                int sep = Array.IndexOf(data, (byte)0);
                if (sep <= 0)
                    return;

                string key = Latin1.GetString(data, 0, sep);
                Encoding encoder = Latin1;

                switch (type)
                {
                    case ChunkTypes.Text:
                        sep++;
                        break;
                    case ChunkTypes.CompressedText:
                        if (sep + 2 > data.Length)
                            return;

                        byte compressionMethod = data[(int)(sep + 1)];
                        if (compressionMethod != 0)
                            return;

                        using (var input = new MemoryStream(data, (int)(sep + 2), (int)(data.Length - sep - 2)))
                        using (var zlib = new ZLibStream(input, CompressionMode.Decompress))
                            data = zlib.ToArray();
                        sep = 0;
                        break;
                    case ChunkTypes.InternationalText:
                        encoder = Encoding.UTF8;
                        sep++;

                        if (sep + 2 > data.Length)
                            return;

                        byte compressionFlag = data[sep++];
                        if (compressionFlag > 1)
                            return;

                        byte iTxtCompressionMethod = data[sep++];

                        sep = Array.IndexOf(data, (byte)0, sep);
                        if (sep < 0)
                            return;

                        sep = Array.IndexOf(data, (byte)0, sep + 1);
                        if (sep < 0)
                            return;

                        sep++;
                        if (compressionFlag == 1)
                        {
                            if (iTxtCompressionMethod != 0)
                                return;

                            using (var input = new MemoryStream(data, sep, data.Length - sep))
                            using (var zlib = new ZLibStream(input, CompressionMode.Decompress))
                            {
                                data = zlib.ToArray();
                                sep = 0;
                            }
                        }
                        break;
                }
                string value = encoder.GetString(data, sep, data.Length - sep);
                pairs.TryAdd(key, value);
            }


            static IImage DecodeImage<TColor>(IHeaderChunk header, Stream imageData, Span<TColor> palette) where TColor : unmanaged, IColor<TColor>
            {
                return header.ColorType switch
                {
                    ColorTypes.Grayscale
                    => header.BitDepth switch
                    {
                        16 => DecodeImageData<I16>(header, imageData),
                        8 => DecodeImageData<I8>(header, imageData),
                        _ => DecodeImageData<I4>(header, imageData),
                    },
                    ColorTypes.UsePalette
                    => header.BitDepth switch
                    {
                        16 => new PaletteImage<I16, TColor>(DecodeImageData<I16>(header, imageData), palette),
                        _ => new PaletteImage<I8, TColor>(DecodeImageData<I8>(header, imageData), palette),
                    },
                    ColorTypes.RGB
                    => header.BitDepth switch
                    {
                        8 => DecodeImageData<RGB24>(header, imageData),
                        16 => DecodeImageData<RGB48>(header, imageData),
                        _ => throw new NotSupportedException(),
                    },
                    ColorTypes.GrayscaleAlpha
                    => header.BitDepth switch
                    {
                        8 => DecodeImageData<IA16>(header, imageData),
                        16 => DecodeImageData<IA32>(header, imageData),
                        _ => throw new NotSupportedException(),
                    },
                    ColorTypes.RGBA
                    => header.BitDepth switch
                    {
                        8 => DecodeImageData<RGBA32>(header, imageData),
                        16 => DecodeImageData<RGBA64>(header, imageData),
                        _ => throw new NotSupportedException(),
                    },
                    _ => throw new NotImplementedException(),
                };
            }

            static IImage DecodeImageData(IHeaderChunk header, Stream zlibData, Span<RGB24> palette, Span<byte> transparency)
            {
                using var imageData = new ZLibStream(zlibData, CompressionMode.Decompress, true);
                if (!transparency.IsEmpty && header.ColorType == ColorTypes.UsePalette && transparency.Length == palette.Length)
                {
                    Span<RGBA32> paletteFull = stackalloc RGBA32[transparency.Length];
                    palette.To(paletteFull);
                    for (int i = 0; i < paletteFull.Length; i++)
                        paletteFull[i].A = transparency[i];
                    return DecodeImage(header, imageData, paletteFull);
                }
                else
                {
                    return DecodeImage(header, imageData, palette);
                }
            }
        }

#if NET6_0_OR_GREATER
        private static Encoding Latin1 = Encoding.Latin1;
#else
        private static Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");
#endif

        private static MemoryImage<TColor> DecodeImageData<TColor>(IHeaderChunk header, Stream source)
            where TColor : unmanaged, IColor<TColor>
        {
            int width = (int)header.Width;
            int height = (int)header.Height;
            var image = new MemoryImage<TColor>(width, height, width);
            Span<TColor> pixelSpan = image.Pixel;
            Span<byte> rawPixel = MemoryMarshal.Cast<TColor, byte>(pixelSpan);
            DecodeImageData(header, source, rawPixel);
            return image;
        }

        private static void DecodeImageData(IHeaderChunk header, Stream source, Span<byte> rawPixel)
        {
            if (header.InterlaceMethod != 0)
                throw new NotSupportedException("Interlaced PNGs (Adam7) are not supported.");

            int width = (int)header.Width;
            int height = (int)header.Height;

            int bytesPerPixel = (header.BitPerPixel + 7) / 8;
            int pixelBytesPerRow = (width * header.BitPerPixel + 7) / 8;
            int stride = 1 + pixelBytesPerRow; // 1 byte filter + pixel data

            // buffers.
            byte[] scanline = ArrayPool<byte>.Shared.Rent(stride);
            byte[] prevScanline = ArrayPool<byte>.Shared.Rent(stride);
            try
            {
                for (int y = 0; y < height; y++)
                {
                    source.ReadExactly(scanline, 0, stride);
                    // Decode scanline
                    switch ((FilterTypes)scanline[0])
                    {
                        case FilterTypes.None:
                            break;
                        case FilterTypes.Sub:
                            for (int i = bytesPerPixel; i < pixelBytesPerRow; i++)
                            {
                                int rawByte = scanline[1 + i];
                                int left = scanline[1 + i - bytesPerPixel];
                                scanline[1 + i] = (byte)((rawByte + left) & 0xFF);
                            }
                            break;
                        case FilterTypes.Up:
                            for (int i = 0; i < pixelBytesPerRow; i++)
                            {
                                int rawByte = scanline[1 + i];
                                int up = prevScanline[1 + i];
                                scanline[1 + i] = (byte)((rawByte + up) & 0xFF);
                            }
                            break;
                        case FilterTypes.Average:
                            for (int i = 0; i < pixelBytesPerRow; i++)
                            {
                                int rawByte = scanline[1 + i];
                                int left = (i - bytesPerPixel) >= 0 ? scanline[1 + i - bytesPerPixel] : 0;
                                int up = prevScanline[1 + i];
                                scanline[1 + i] = (byte)((rawByte + ((left + up) >> 1)) & 0xFF);
                            }
                            break;
                        case FilterTypes.Paeth:
                            for (int i = 0; i < pixelBytesPerRow; i++)
                            {
                                int rawByte = scanline[1 + i];
                                int a = (i - bytesPerPixel) >= 0 ? scanline[1 + i - bytesPerPixel] : 0;       // left
                                int b = prevScanline[1 + i];                                                  // above
                                int c = (i - bytesPerPixel) >= 0 ? prevScanline[1 + i - bytesPerPixel] : 0;   // upper-left
                                scanline[1 + i] = (byte)((rawByte + PaethPredictor(a, b, c)) & 0xFF);
                            }
                            break;
                        default:
                            throw new NotSupportedException($"Unknown PNG filter type: {(FilterTypes)scanline[0]}");
                    }

                    if (header.BitDepth >= 8)
                    {
                        int dstRowOffset = y * pixelBytesPerRow;
                        scanline.AsSpan(1, pixelBytesPerRow).CopyTo(rawPixel.Slice(dstRowOffset, pixelBytesPerRow));
                    }
                    else
                    {
                        int dstRowOffset = y * width;
                        ImageStreamHelper.UnpackPixels(scanline.AsSpan(1, pixelBytesPerRow), rawPixel.Slice(dstRowOffset), pixelBytesPerRow, header.BitDepth, width);
                    }
                    // swap ref of scanline <-> prevScanline for next line.
                    (scanline, prevScanline) = (prevScanline, scanline);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(scanline);
                ArrayPool<byte>.Shared.Return(prevScanline);
            }
        }

        private static int PaethPredictor(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);

            if (pa <= pb && pa <= pc) return a;
            if (pb <= pc) return b;
            return c;
        }
        #endregion

        #region WriteImage (Encode)

        /// <inheritdoc/>
        public void WriteImage<TColor>(IReadOnlyImage<TColor> source, Stream destination) where TColor : unmanaged, IColor<TColor>
        {
            using var imageData = new MemoryPoolStream();
            ZLibStream zLibStream = new ZLibStream(imageData, CompressionLevel.Optimal, true);
            EncodeImage(source, zLibStream, out byte bitDepth, out ColorTypes colorTyp, out ReadOnlySpan<TColor> iPalette);
            zLibStream.Dispose();
            IHeaderChunk header = new IHeaderChunk((uint)source.Width, (uint)source.Height, bitDepth, colorTyp);

            // Encode Palette
            Span<RGB24> palette = default;
            Span<byte> transparency = default;
            if (!iPalette.IsEmpty)
            {
                palette = new RGB24[iPalette.Length];
                iPalette.To(palette);
                if (iPalette[0].FormatInfo.HasAlpha)
                {
                    transparency = new byte[iPalette.Length];
                    iPalette.To(MemoryMarshal.Cast<byte, A<byte>>(transparency));
                }
            }

            // Write Imag
            Crc32 crc32 = new Crc32();
            destination.Write(_Identifier);
            WriteChunk(destination, crc32, ChunkTypes.Header, header.AsBytes());
            WriteMetadataChunks(source, destination, crc32);
            if (!palette.IsEmpty) WriteChunk(destination, crc32, ChunkTypes.Palette, MemoryMarshal.AsBytes(palette));
            if (!transparency.IsEmpty) WriteChunk(destination, crc32, ChunkTypes.Transparency, transparency);
            WriteImageDataChunks(destination, crc32, imageData);
            WriteChunk(destination, crc32, ChunkTypes.End);
        }

        private static void WriteMetadataChunks<TColor>(IReadOnlyImage<TColor> source, Stream destination, Crc32 crc32) where TColor : unmanaged, IColor<TColor>
        {
            if (source.Metadata == null)
                return;

            Span<byte> buffer = stackalloc byte[32];
            Vector2 pixelsPerUnit = source.Metadata.PixelsPerCentimeter * 100;
            BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)pixelsPerUnit.X);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(4), (uint)pixelsPerUnit.Y);
            buffer[8] = 1;
            WriteChunk(destination, crc32, ChunkTypes.Physical, buffer.Slice(0, 9));

            if (source.Metadata.Icc != null)
            {
                WriteChunk(destination, crc32, ChunkTypes.ICC_Profile, source.Metadata.Icc);
            }
            else
            {
                if (ColorSpace.sRGB.Equals(source.Metadata.ColorSpace))
                {
                    buffer[0] = 0;
                    WriteChunk(destination, crc32, ChunkTypes.StandardRGB, buffer.Slice(0, 1));
                    BinaryPrimitives.WriteUInt32BigEndian(buffer, 45455);
                    WriteChunk(destination, crc32, ChunkTypes.Gamma, buffer.Slice(0, 4));
                }
                else
                {
                    BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)(source.Metadata.Gamma * 100000));
                    WriteChunk(destination, crc32, ChunkTypes.Gamma, buffer.Slice(0, 4));
                    if (source.Metadata.ColorSpace != null)
                    {
                        Vector2 whitePoint = source.Metadata.ColorSpace.WhitePoint * 100000f;
                        Vector2 redPoint = source.Metadata.ColorSpace.Red * 100000f;
                        Vector2 greenPoint = source.Metadata.ColorSpace.Green * 100000f;
                        Vector2 bluePoint = source.Metadata.ColorSpace.Blue * 100000f;
                        BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)whitePoint.X);
                        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(4), (uint)whitePoint.Y);
                        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(8), (uint)redPoint.X);
                        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(12), (uint)redPoint.Y);
                        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(16), (uint)greenPoint.X);
                        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(20), (uint)greenPoint.Y);
                        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(24), (uint)bluePoint.X);
                        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(28), (uint)bluePoint.Y);
                        WriteChunk(destination, crc32, ChunkTypes.PrimaryChromaticities, buffer.Slice(0, 32));
                    }
                }
            }

            if (source.Metadata.Exif != null)
                WriteChunk(destination, crc32, ChunkTypes.EXIF_Metadata, source.Metadata.Exif);

            if (source.Metadata.Text != null)
            {
                byte[] textBuffer = ArrayPool<byte>.Shared.Rent(1024);
                try
                {
                    foreach (var entry in source.Metadata.Text)
                    {
                        if (entry.Key.Length > 79 || entry.Value.Length + entry.Key.Length + 1 >= 1024)
                            continue;

                        int i = Latin1.GetBytes(entry.Key, 0, entry.Key.Length, textBuffer, 0);
                        textBuffer[i++] = 0;
                        i += Latin1.GetBytes(entry.Value, 0, entry.Value.Length, textBuffer, i);

                        WriteChunk(destination, crc32, ChunkTypes.Text, textBuffer.AsSpan(0, i));
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(textBuffer);
                }
            }
        }

        static void EncodeImage<TColor>(IReadOnlyImage<TColor> source, Stream dest, out byte bitDepth, out ColorTypes colorTyp, out ReadOnlySpan<TColor> palette) where TColor : unmanaged, IColor<TColor>
        {
            if (source is IEnumerable<IImage<TColor>> images)
                source = images.First();

            palette = default;

            var formatInfo = default(TColor).FormatInfo;

            colorTyp = formatInfo.IsGrayscale
                ? formatInfo.HasAlpha ? ColorTypes.GrayscaleAlpha : ColorTypes.Grayscale
                : formatInfo.HasAlpha ? ColorTypes.RGBA : ColorTypes.RGB;

            int bpp = formatInfo.BitsPerPixel;

            if (source is IReadOnlyPaletteImage<TColor> paletteImage && paletteImage.GetBuffer() is IReadOnlyImage<I8> indexes)
            {

                var paletteRef = paletteImage.PaletteRefCounts;
                for (int i = paletteRef.Length - 1; i >= 0; i--)
                {
                    if (paletteRef[i] != 0)
                    {
                        palette = paletteImage.Palette.Slice(0, i + 1);
                        break;
                    }
                }

                colorTyp = ColorTypes.UsePalette | ColorTypes.RGB;
                bitDepth = palette.Length <= 4 ? (byte)2 : palette.Length <= 16 ? (byte)4 : (byte)8;
                EncodeImageData<I8, I8>(indexes, dest, bitDepth);
                return;
            }


            switch (colorTyp)
            {
                case ColorTypes.Grayscale:
                    if (bpp <= 8)
                    {
                        bitDepth = (byte)bpp;
                        EncodeImageData<TColor, I8>(source, dest, bpp);
                    }
                    else
                    {
                        bitDepth = 16;
                        EncodeImageData<TColor, I16>(source, dest, 16);
                    }
                    break;
                case ColorTypes.GrayscaleAlpha:
                    if (bpp <= 16)
                    {
                        bitDepth = 8;
                        EncodeImageData<TColor, IA16>(source, dest, 16);
                    }
                    else
                    {
                        bitDepth = 16;
                        EncodeImageData<TColor, IA32>(source, dest, 32);
                    }
                    break;
                case ColorTypes.RGB:
                    if (bpp <= 24)
                    {
                        bitDepth = 8;
                        EncodeImageData<TColor, RGB24>(source, dest, 24);
                    }
                    else
                    {
                        bitDepth = 16;
                        EncodeImageData<TColor, RGB48>(source, dest, 48);
                    }
                    break;
                default:
                    colorTyp = ColorTypes.RGBA;
                    if (bpp <= 32)
                    {
                        bitDepth = 8;
                        EncodeImageData<TColor, RGBA32>(source, dest, 32);
                    }
                    else
                    {
                        bitDepth = 16;
                        EncodeImageData<TColor, RGBA64>(source, dest, 64);
                    }
                    break;
            }
        }

        private static void EncodeImageData<TFrom, TTo>(IReadOnlyImage<TFrom> source, Stream dest, int BitPerPixel) where TFrom : unmanaged, IColor<TFrom> where TTo : unmanaged, IColor<TTo>
        {
            ReadOnlyRowAccessor<TFrom> rowAccessor = new ReadOnlyRowAccessor<TFrom>(source, 0, source.Width);

            int width = (int)source.Width;
            int height = (int)source.Height;

            int pixelBytesPerRow = (width * BitPerPixel + 7) / 8;
            int stride = 1 + pixelBytesPerRow; // 1 byte filter + pixel data

            byte[] scanline = ArrayPool<byte>.Shared.Rent(stride);
            try
            {
                var pixel = MemoryMarshal.Cast<byte, TTo>(scanline.AsSpan(1, pixelBytesPerRow));
                for (int y = 0; y < height; y++)
                {
                    if (BitPerPixel >= 8)
                    {
                        rowAccessor[y].To(pixel);
                    }
                    else
                    {
                        ImageStreamHelper.PackPixels(MemoryMarshal.Cast<TFrom, byte>(rowAccessor[y]), MemoryMarshal.Cast<TTo, byte>(pixel), BitPerPixel, width);
                    }

                    scanline[0] = (byte)FilterTypes.None; // skip for now.
                    dest.Write(scanline, 0, stride);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(scanline);
            }
        }



        private static void WriteImageDataChunks(Stream destination, Crc32 crc32, MemoryPoolStream imageData)
        {
            const int ChunkSize = 0x20000;
            ReadOnlySpan<byte> imageDataSpan = imageData.UnsafeAsSpan();
            while (!imageDataSpan.IsEmpty)
            {
                int len = Math.Min(ChunkSize, imageDataSpan.Length);
                WriteChunk(destination, crc32, ChunkTypes.Data, imageDataSpan.Slice(0, len));
                imageDataSpan = imageDataSpan.Slice(len);
            }
        }

        private static void WriteChunk(Stream destination, Crc32 crc32, ChunkTypes chunkType, ReadOnlySpan<byte> data = default)
        {
            // Build chunk header
            Span<byte> header = stackalloc byte[8];
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(4), (int)chunkType);
            BinaryPrimitives.WriteInt32BigEndian(header.Slice(0), data.Length);

            // Calculate CRC32
            crc32.Reset();
            crc32.Append(header.Slice(4));
            crc32.Append(data);
            uint hash = crc32.GetCurrentHashAsUInt32();

            // Write data
            destination.Write(header);
            destination.Write(data);
            destination.Write(hash, Endian.Big);
        }
        #endregion
    }
}
