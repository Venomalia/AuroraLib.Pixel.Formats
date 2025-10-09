using AuroraLib.Core.Collections;
using AuroraLib.Core.IO;
using AuroraLib.Pixel.BlockProcessor;
using AuroraLib.Pixel.Formats.Dolphin.BlockProcessor;
using AuroraLib.Pixel.Formats.Dolphin.PixelFormats;
using AuroraLib.Pixel.Image;
using AuroraLib.Pixel.Metadata;
using AuroraLib.Pixel.PixelFormats;
using AuroraLib.Pixel.Processing;
using AuroraLib.Pixel.Processing.Analyzer;
using AuroraLib.Pixel.Processing.Quantizer;
using AuroraLib.Pixel.Processing.Resampler;
using AuroraLib.Pixel.Texture;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace AuroraLib.Pixel.Formats.Dolphin.GXFormat
{
    /// <summary>
    /// Describes all texture settings for a GX texture object used by the Nintendo GameCube/Wii graphics pipeline.
    /// </summary>
    public sealed class GXTexInfo
    {
        /// <summary>
        /// Gets or sets the width of the texture in pixels.
        /// </summary>
        /// <remarks>
        /// Maximum supported size is 1024.
        /// </remarks>
        public ushort Width { get; set; }

        /// <summary>
        /// Gets or sets the height of the texture in pixels.
        /// </summary>
        /// <remarks>
        /// Maximum supported size is 1024.
        /// </remarks>
        public ushort Height { get; set; }

        /// <summary>
        /// Gets or sets the number of palette entries or palette blocks used by indexed textures.
        /// </summary>
        /// <remarks>
        /// This value is only relevant for paletted image formats such as <see cref="GXImageFormat.C4"/>, <see cref="GXImageFormat.C8"/>, or <see cref="GXImageFormat.C14X2"/>.
        /// </remarks>
        public ushort PaletteCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether mipmaps are enabled.
        /// A value of <see langword="null"/> means that the mipmap usage is determined automatically.
        /// </summary>
        public bool? EnableMips { get; set; }

        /// <summary>
        /// Gets or sets the number of mipmap levels stored for this texture.
        /// A value of 0 means only the base image is present.
        /// </summary>
        public byte MipMapCount { get; set; }

        /// <summary>
        /// Gets or sets the GX image format used to store the texture data.
        /// </summary>
        public GXImageFormat Format { get; set; }

        /// <summary>
        /// Gets or sets the palette format used for indexed textures.
        /// This value is ignored for non-paletted textures.
        /// </summary>
        public GXPaletteFormat PaletteFormat { get; set; }

        ///  <inheritdoc cref="GXFormatExtensions.IsPaletteFormat(GXImageFormat)"/>
        public bool IsPaletteFormat => Format.IsPaletteFormat();

        ///  <inheritdoc cref="GXFormatExtensions.GetMaxPaletteCapacity(GXImageFormat)"/>
        public int MaxPaletteCapacity => Format.GetMaxPaletteCapacity();

        /// <summary>
        /// Calculates the size of the texture data in bytes.
        /// </summary>
        /// <returns>The size of the texture data in bytes.</returns>
        public int CalculatedDataSize() => Format.GetBlockFormat().CalculatedDataSize(Width, Height, MipMapCount);

        /// <summary>
        /// Gets a value indicating whether the texture settings are valid.
        /// </summary>
        public bool IsValid => Width != 0 && Height != 0 && Width <= 1024 && Height <= 1024 && MipMapCount <= 10 &&
            Enum.IsDefined(typeof(GXImageFormat), Format) &&
            (!IsPaletteFormat || (Enum.IsDefined(typeof(GXPaletteFormat), PaletteFormat) && PaletteCount != 0 && PaletteCount <= MaxPaletteCapacity));

        public GXTexInfo()
        { }

        /// <summary>
        /// Reads a GX texture from the specified stream using the given image and palette offsets.
        /// </summary>
        /// <param name="source">The stream to read from.</param>
        /// <param name="imageOffset">The offset of the image data.</param>
        /// <param name="paletteOffset">The offset of the palette data.</param>
        /// <returns>The decoded texture.</returns>
        public IImage ReadGxTexture(Stream source, long imageOffset, long paletteOffset)
        {
            Span<byte> paletteData = !IsPaletteFormat ? default : stackalloc byte[Math.Min(PaletteCount, MaxPaletteCapacity) * 2];
            if (!paletteData.IsEmpty)
            {
                source.Seek(paletteOffset, SeekOrigin.Begin);
                source.ReadExactly(paletteData);
            }
            source.Seek(imageOffset, SeekOrigin.Begin);
            return ReadGxTexture(source, paletteData);
        }

        /// <summary>
        /// Reads a GX texture from the specified stream.
        /// </summary>
        /// <param name="source">The stream to read from.</param>
        /// <param name="paletteData">The palette data used to decode the texture.</param>
        /// <returns>The decoded texture.</returns>
        public IImage ReadGxTexture(Stream source, ReadOnlySpan<byte> paletteData = default)
        {
            int size = Format.GetBlockFormat().CalculatedDataSize(Width, Height, MipMapCount);
            if (source.Length - source.Position < size)
                throw new EndOfStreamException($"Not enough data for GX texture at offset 0x{source.Position:x}. Expected {size} bytes, available {source.Length - source.Position}.");

            //  Read texture
            IImage tex = Format switch
            {
                GXImageFormat.I4 => ImageStreamHelper.ReadTexture(source, Width, Height, MipMapCount, new GxI4Block()),
                GXImageFormat.I8 => ImageStreamHelper.ReadTexture(source, Width, Height, MipMapCount, new GxI8Block()),
                GXImageFormat.IA4 => ImageStreamHelper.ReadTexture(source, Width, Height, MipMapCount, new GxIA4Block()),
                GXImageFormat.IA8 => ImageStreamHelper.ReadTexture(source, Width, Height, MipMapCount, new GxIA8Block()),
                GXImageFormat.RGB565 => ImageStreamHelper.ReadTexture(source, Width, Height, MipMapCount, new GxRGB565Block()),
                GXImageFormat.RGB5A3 => ImageStreamHelper.ReadTexture(source, Width, Height, MipMapCount, new GxRGB5A3Block()),
                GXImageFormat.RGBA32 => ImageStreamHelper.ReadTexture(source, Width, Height, MipMapCount, new GxRGBA32Block()),
                GXImageFormat.C4 => ReadGxTexture(source, new GxI4Block(), paletteData),
                GXImageFormat.C8 => ReadGxTexture(source, new GxI8Block(), paletteData),
                GXImageFormat.C14X2 => ReadGxTexture(source, new GxI14Block(), paletteData),
                GXImageFormat.CMPR => ImageStreamHelper.ReadTexture(source, Width, Height, MipMapCount, new GxCMPRBlock()),
                _ => throw new NotSupportedException(),
            };

            //  Calculate dolphin texture hash
            ImageMetadata metadata = new ImageMetadata();
            GetDolphinHash(tex, out ulong baseHash, out ulong tlutHash);
            metadata.Text.TryAdd("DolphinHash", GetDolphinHash(baseHash, tlutHash));
            tex.Metadata = metadata;
            return tex;

        }

        private IImage ReadGxTexture<TIndex>(Stream source, IBlockProcessor<TIndex> blockProcessor, ReadOnlySpan<byte> paletteData) where TIndex : unmanaged, IIndexColor, IColor<TIndex>
        => PaletteFormat switch
        {
            GXPaletteFormat.IA8 => ImageStreamHelper.ReadTexture(source, Width, Height, MipMapCount, CreatePalette<IA<byte>>(paletteData).AsMemory(), blockProcessor),
            GXPaletteFormat.RGB565 => ImageStreamHelper.ReadTexture(source, Width, Height, MipMapCount, CreatePalette<RGB565>(paletteData).AsMemory(), blockProcessor),
            GXPaletteFormat.RGB5A3 => ImageStreamHelper.ReadTexture(source, Width, Height, MipMapCount, CreatePalette<RGB5A3>(paletteData).AsMemory(), blockProcessor),
            _ => throw new NotSupportedException(),
        };

        private static TColor[] CreatePalette<TColor>(ReadOnlySpan<byte> source) where TColor : unmanaged, IColor<TColor>
        {
            TColor[] colors = new TColor[source.Length / 2];
            ReverseEndianness_Ushort(source, MemoryMarshal.Cast<TColor, byte>(colors.AsSpan()));
            return colors;
        }

        private static void ReverseEndianness_Ushort(ReadOnlySpan<byte> source, Span<byte> target)
        {
            ReadOnlySpan<ushort> sourceUshort = MemoryMarshal.Cast<byte, ushort>(source);
            Span<ushort> targetUshort = MemoryMarshal.Cast<byte, ushort>(target);
#if NET8_0_OR_GREATER
            BinaryPrimitives.ReverseEndianness(sourceUshort, targetUshort);
#else
            for (int i = 0; i < sourceUshort.Length; i++)
            {
                targetUshort[i] = BinaryPrimitives.ReverseEndianness(sourceUshort[i]);
            }
#endif
        }

        /// <summary>
        /// Encodes and writes a GX texture to the specified stream.
        /// </summary>
        /// <param name="destination">The stream to write to.</param>
        /// <param name="source">The image to encode.</param>
        /// <param name="palette">The generated palette data, or <see langword="null"/> if the texture does not use a palette.</param>
        public void WriteTexture(Stream destination, IReadOnlyImage source, out byte[]? palette)
        {
            if (source.Width != Width || source.Height != Height)
                throw new ArgumentException();

            palette = null;
            switch (Format)
            {
                case GXImageFormat.I4:
                    WriteTexture(destination, source, new GxI4Block(), MipMapCount);
                    break;
                case GXImageFormat.I8:
                    WriteTexture(destination, source, new GxI8Block(), MipMapCount);
                    break;
                case GXImageFormat.IA4:
                    WriteTexture(destination, source, new GxIA4Block(), MipMapCount);
                    break;
                case GXImageFormat.IA8:
                    WriteTexture(destination, source, new GxIA8Block(), MipMapCount);
                    break;
                case GXImageFormat.RGB565:
                    WriteTexture(destination, source, new GxRGB565Block(), MipMapCount);
                    break;
                case GXImageFormat.RGB5A3:
                    WriteTexture(destination, source, new GxRGB5A3Block(), MipMapCount);
                    break;
                case GXImageFormat.RGBA32:
                    WriteTexture(destination, source, new GxRGBA32Block(), MipMapCount);
                    break;
                case GXImageFormat.C4:
                    palette = WritePaletteTexture(destination, source, new GxI4Block());
                    break;
                case GXImageFormat.C8:
                    palette = WritePaletteTexture(destination, source, new GxI8Block());
                    break;
                case GXImageFormat.C14X2:
                    palette = WritePaletteTexture(destination, source, new GxI14Block());
                    break;
                case GXImageFormat.CMPR:
                    WriteTexture(destination, source, new GxCMPRBlock(), MipMapCount);
                    break;
                default:
                    throw new NotSupportedException();
            }

            byte[] WritePaletteTexture<TIndex>(Stream destination, IReadOnlyImage source, IBlockProcessor<TIndex> format)
                where TIndex : unmanaged, IIndexColor, IColor<TIndex>
                => PaletteFormat switch
                {
                    GXPaletteFormat.IA8 => WriteTexture<IA<byte>, TIndex>(destination, source, format, MipMapCount, PaletteCount),
                    GXPaletteFormat.RGB565 => WriteTexture<RGB565, TIndex>(destination, source, format, MipMapCount, PaletteCount),
                    GXPaletteFormat.RGB5A3 => WriteTexture<RGB5A3, TIndex>(destination, source, format, MipMapCount, PaletteCount),
                    _ => throw new NotSupportedException(),
                };
        }

        private static byte[] WriteTexture<TColor, TIndex>(Stream destination, IReadOnlyImage source, IBlockProcessor<TIndex> format, int mipMapCount, int paletteCount) where TIndex : unmanaged, IIndexColor, IColor<TIndex> where TColor : unmanaged, IColor<TColor>
        {
            if (Unsafe.SizeOf<TColor>() != sizeof(ushort))
                throw new InvalidOperationException("GX palette colors must be 16-bit.");

            int processed = 0;
            FlatTexture<TColor>? texture = source as FlatTexture<TColor>;
            IReadOnlyImage last = texture != null ? texture.Levels[0] : source;
            byte[] rawPalette = new byte[paletteCount * 2];
            Span<TColor> palette = MemoryMarshal.Cast<byte, TColor>(rawPalette.AsSpan());

            if (last is PaletteImage<TIndex, TColor> paletteImage && paletteImage.GetBuffer() is BlockImage<TIndex> blockImage && blockImage.BlockFormat.GetType() == format.GetType() && paletteImage.Palette.Length <= paletteCount)
            {
                paletteImage.Palette.Span.CopyTo(palette);

                if (texture != null)
                {
                    int levelCount = Math.Min(texture.MipMapCount, mipMapCount);

                    for (int i = 0; i <= levelCount; i++)
                    {
                        IBlockImage level = (IBlockImage)((PaletteImage<TIndex, TColor>)texture.Levels[i]).GetBuffer();
                        destination.Write(level.Raw);
                    }
                    last = texture.Levels[levelCount];
                    processed = levelCount + 1;
                }
                else
                {
                    destination.Write(blockImage.Raw);
                    processed = 1;
                }
            }

            for (int i = processed; i <= mipMapCount; i++)
            {
                int width = source.Width >> i;
                int height = source.Height >> i;

                IColorQuantizer<TColor>? quantizer = i != 0 ? new NearestPaletteColorPicker<TColor>() : null;
                using var convertet = new PaletteImage<TIndex, TColor>(new BlockImage<TIndex>(format, width, height), palette, quantizer);

                convertet.ResizeFrom(last, Resamplers.Box);
                destination.Write(((IBlockImage)convertet.GetBuffer()).Raw);
            }

            ReverseEndianness_Ushort(rawPalette, rawPalette);
            return rawPalette;
        }

        private static void WriteTexture<TColor>(Stream destination, IReadOnlyImage source, IBlockProcessor<TColor> format, int mipMapCount) where TColor : unmanaged, IColor<TColor>
        {
            int processed = 0;
            FlatTexture<TColor>? texture = source as FlatTexture<TColor>;
            IReadOnlyImage last = texture != null ? texture.Levels[0] : source;

            if (last is BlockImage<TColor> blockImage && blockImage.BlockFormat.GetType() == format.GetType())
            {
                if (texture != null)
                {
                    int levelCount = Math.Min(texture.MipMapCount, mipMapCount);

                    for (int i = 0; i <= levelCount; i++)
                    {
                        IBlockImage level = (IBlockImage)texture.Levels[i];
                        destination.Write(level.Raw);
                    }

                    last = texture.Levels[levelCount];
                    processed = levelCount + 1;
                }
                else
                {
                    destination.Write(blockImage.Raw);
                    processed = 1;
                }
            }

            for (int i = processed; i <= mipMapCount; i++)
            {
                int width = source.Width >> i;
                int height = source.Height >> i;

                using var convertet = new BlockImage<TColor>(format, width, height);
                convertet.ResizeFrom(last, Resamplers.Box);
                destination.Write(convertet.Raw);
            }
        }

        /// <summary>
        /// Attempts to create GX texture information from an image using its native texture format.
        /// </summary>
        /// <param name="image">The image to analyze.</param>
        /// <param name="info">Receives the generated GX texture information.</param>
        /// <returns><see langword="true"/> if the image uses a supported native GX texture format; otherwise, <see langword="false"/>.</returns>
        public static bool TryCreate(IReadOnlyImage image, out GXTexInfo info)
        {
            info = new GXTexInfo();
            info.Width = (ushort)image.Width;
            info.Height = (ushort)image.Height;

            // get mips from texture
            if (image is IEnumerable<IImage> mips)
            {
                int count = mips.Count();
                if (count == 0) throw new ArgumentException();

                info.MipMapCount = Math.Min((byte)(count - 1), (byte)10);
                image = mips.First();
            }

            if (image is IReadOnlyPaletteImage pI)
            {
                if (pI.GetBuffer() is IBlockImage bI)
                {
                    if (bI.BlockFormat is GxI4Block) info.Format = GXImageFormat.C4;
                    else if (bI.BlockFormat is GxI8Block) info.Format = GXImageFormat.C8;
                    else if (bI.BlockFormat is GxI14Block) info.Format = GXImageFormat.C14X2;
                    else return false;

                    if (pI is IReadOnlyPaletteImage<IA<byte>> ia8)
                    {
                        info.PaletteFormat = GXPaletteFormat.IA8;
                        info.PaletteCount = (ushort)ia8.Palette.Length;
                    }
                    else if (pI is IReadOnlyPaletteImage<RGB565> rgb565)
                    {
                        info.PaletteFormat = GXPaletteFormat.RGB565;
                        info.PaletteCount = (ushort)rgb565.Palette.Length;
                    }
                    else if (pI is IReadOnlyPaletteImage<RGB5A3> rgb5a3)
                    {
                        info.PaletteFormat = GXPaletteFormat.RGB5A3;
                        info.PaletteCount = (ushort)rgb5a3.Palette.Length;
                    }
                    else return false;
                    return true;
                }
            }
            else if (image is IBlockImage bI)
            {
                if (bI.BlockFormat is GxI4Block) info.Format = GXImageFormat.I4;
                else if (bI.BlockFormat is GxI8Block) info.Format = GXImageFormat.I8;
                else if (bI.BlockFormat is GxIA4Block) info.Format = GXImageFormat.IA4;
                else if (bI.BlockFormat is GxIA8Block) info.Format = GXImageFormat.IA8;
                else if (bI.BlockFormat is GxRGB565Block) info.Format = GXImageFormat.RGB565;
                else if (bI.BlockFormat is GxRGB5A3Block) info.Format = GXImageFormat.RGB5A3;
                else if (bI.BlockFormat is GxRGBA32Block) info.Format = GXImageFormat.RGBA32;
                else if (bI.BlockFormat is GxCMPRBlock) info.Format = GXImageFormat.CMPR;
                //else if (bI.BlockFormat is BC1Block<RGBA<byte>>) info.Format = GXImageFormat.CMPR; // use BC1?
                else return false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates GX texture information for the specified image.
        /// </summary>
        /// <param name="image">The image to analyze.</param>
        /// <returns>The generated GX texture information.</returns>
        public static GXTexInfo Create(IReadOnlyImage image)
        {
            if (TryCreate(image, out GXTexInfo info))
                return info;

            var pf = image.PixelFormat;

            if (image is IReadOnlyPaletteImage p)
            {
                // Round the palette size up to the next multiple of 16.
                info.PaletteCount = (ushort)((p.GetUsedColors() + 15) & ~15);
                info.Format = info.PaletteCount <= 0x10 + 1 ? GXImageFormat.C4 : info.PaletteCount <= 0x100 ? GXImageFormat.C8 : GXImageFormat.C14X2;
                if (pf.HasAlpha)
                {
                    if (pf.IsGrayscale)
                        info.PaletteFormat = GXPaletteFormat.IA8;
                    else
                        info.PaletteFormat = GXPaletteFormat.RGB5A3;
                }
                else if (!pf.IsGrayscale)
                {
                    info.PaletteFormat = GXPaletteFormat.RGB565;
                }
                else
                {
                    info.Format = GXImageFormat.I8;
                    info.PaletteCount = 0;
                }
            }
            else
            {
                if (pf.IsGrayscale)
                {
                    if (!pf.HasAlpha)
                    {
                        if (image is IReadOnlyImage<I4>)
                            info.Format = GXImageFormat.I4;
                        else
                            info.Format = GXImageFormat.I8;
                    }
                    else
                    {
                        if (image is IReadOnlyImage<IA8>)
                            info.Format = GXImageFormat.IA4;
                        else
                            info.Format = GXImageFormat.IA8;
                    }
                }
                else
                {
                    if (!pf.HasAlpha)
                    {
                        if (image is IReadOnlyImage<RGB565> || image is IReadOnlyImage<RGB555> || image is IReadOnlyImage<BGR555>)
                            info.Format = GXImageFormat.RGB565;
                        else
                            info.Format = GXImageFormat.CMPR;
                    }
                    else
                    {
                        if (image is IReadOnlyImage<RGB5A3>)
                            info.Format = GXImageFormat.RGB5A3;
                        else
                        {
                            var transparency = image.Apply(new TransparencyAnalyzer());
                            info.Format = transparency switch
                            {
                                TransparencyMode.Opaque => GXImageFormat.CMPR,
                                TransparencyMode.Cutout => GXImageFormat.CMPR,
                                _ => pf.AlphaChannelInfo.BitDepth < 6 ? GXImageFormat.RGB5A3 : GXImageFormat.RGBA32,
                            };
                        }
                    }
                }

            }

            return info;
        }

        /// <summary>
        /// Attempts to calculate the Dolphin texture hash for the specified image.
        /// </summary>
        /// <param name="image">The image to calculate the hash for.</param>
        /// <param name="hash">Receives the calculated Dolphin texture hash.</param>
        /// <returns><see langword="true"/> if the image uses a supported GX texture format; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetDolphinHash(IReadOnlyImage image, out string hash)
        {
            if (TryCreate(image, out GXTexInfo info))
            {
                GetDolphinHash(image, out ulong baseHash, out ulong tlutHash);
                hash = info.GetDolphinHash(baseHash, tlutHash);
                return true;
            }

            hash = string.Empty;
            return false;
        }

        /// <summary>
        /// Creates a Dolphin texture hash from the specified texture and palette hashes.
        /// </summary>
        /// <param name="baseHash">The hash of the texture data.</param>
        /// <param name="tlutHash">The hash of the texture lookup table.</param>
        /// <returns>The formatted Dolphin texture hash.</returns>
        public string GetDolphinHash(ulong baseHash, ulong tlutHash)
        {
            bool hasMip = EnableMips ?? MipMapCount != 0;
            string tlutString = IsPaletteFormat ? tlutHash == 0 ? "$_" : $"{tlutHash:x16}_" : string.Empty;
            return $"tex1_{Width}x{Height}_{(hasMip ? "m_" : "")}{baseHash:x16}_{tlutString}{(int)Format}";
        }

        /// <summary>
        /// Calculates the Dolphin texture and palette hashes for an image in a supported GX texture format.
        /// </summary>
        /// <param name="image">The GX texture image to calculate the hashes for.</param>
        /// <param name="baseHash">Receives the hash of the texture data.</param>
        /// <param name="tlutHash">Receives the hash of the texture lookup table, or zero if no palette is used.</param>
        public static void GetDolphinHash(IReadOnlyImage image, out ulong baseHash, out ulong tlutHash)
        {
            baseHash = tlutHash = 0;

            if (image is IEnumerable<IImage> mips)
            {
                using var e = mips.GetEnumerator();
                e.MoveNext();
                image = e.Current;
            }

            if (image is IReadOnlyPaletteImage pI)
            {
                tlutHash = pI switch
                {
                    IReadOnlyPaletteImage<IA<byte>> ia8 => CalculateTlutHash(ia8),
                    IReadOnlyPaletteImage<RGB565> rgb565 => CalculateTlutHash(rgb565),
                    IReadOnlyPaletteImage<RGB5A3> rgb5a3 => CalculateTlutHash(rgb5a3),
                    _ => 0
                };

                image = pI.GetBuffer();
            }

            if (image is IBlockImage bI)
                baseHash = XxHash64.HashToUInt64(bI.Raw);

            static ulong CalculateTlutHash<TColor>(IReadOnlyPaletteImage<TColor> pImage) where TColor : unmanaged, IColor<TColor>
            {
                pImage.GetUsedPaletteRange(out int start, out int length);

                if (start + length > pImage.Palette.Length)
                    return 0;

                ReadOnlySpan<byte> data = MemoryMarshal.Cast<TColor, byte>(pImage.Palette.Slice(start, length));
                Span<byte> nativ = stackalloc byte[data.Length];
                ReverseEndianness_Ushort(data, nativ);
                return XxHash64.HashToUInt64(nativ);
            }
        }

    }
}
