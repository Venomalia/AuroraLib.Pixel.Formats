using AuroraLib.Pixel.BlockProcessor;
using AuroraLib.Pixel.Formats.Dolphin.BlockProcessor;
using AuroraLib.Pixel.Formats.Dolphin.PixelFormats;
using AuroraLib.Pixel.Image;
using AuroraLib.Pixel.Metadata;
using AuroraLib.Pixel.PixelFormats;
using AuroraLib.Pixel.Texture;
using System;

namespace AuroraLib.Pixel.Formats.Dolphin.GXFormat
{
    public static class GXFormatExtensions
    {
        /// <summary>
        /// Determines whether the GX image format uses a color palette (TLUT).
        /// </summary>
        /// <param name="format">The GX image format.</param>
        /// <returns><see langword="true"/> if the format is paletted; otherwise, <see langword="false"/>.</returns>
        public static bool IsPaletteFormat(this GXImageFormat format) => format == GXImageFormat.C4 || format == GXImageFormat.C8 || format == GXImageFormat.C14X2;

        /// <summary>
        /// Gets the maximum number of palette entries supported by the specified GX image format.
        /// </summary>
        /// <param name="format">The GX image format.</param>
        /// <returns>The maximum palette capacity, or <c>0</c> if the format is not paletted.</returns>
        public static int GetMaxPaletteCapacity(this GXImageFormat format) => format switch
        {
            GXImageFormat.C4 => 0x10,
            GXImageFormat.C8 => 0x100,
            GXImageFormat.C14X2 => 0x4000,
            _ => 0,
        };

        /// <summary>
        /// Gets the block format implementation used by the specified GX image format.
        /// </summary>
        /// <param name="format">The GX image format.</param>
        /// <returns>The corresponding <see cref="IBlockFormat"/> instance.</returns>
        public static IBlockFormat GetBlockFormat(this GXImageFormat format)
        {
            int index = (int)format;
            if ((uint)index >= BlockFormats.Length || !(BlockFormats[index] is IBlockFormat block))
                throw new NotSupportedException($"Unsupported GX image format: {format}");

            return block;
        }

        private static readonly IBlockFormat?[] BlockFormats =
        {
            new GxI4Block(),      // 0 I4
            new GxI8Block(),      // 1 I8
            new GxIA4Block(),     // 2 IA4
            new GxIA8Block(),     // 3 IA8
            new GxRGB565Block(),  // 4 RGB565
            new GxRGB5A3Block(),  // 5 RGB5A3
            new GxRGBA32Block(),  // 6 RGBA32
            null,                 // 7
            new GxI4Block(),      // 8 C4
            new GxI8Block(),      // 9 C8
            new GxI14Block(),     // A C14X2
            null,                 // B
            null,                 // C
            null,                 // D
            new GxCMPRBlock(),    // E CMPR
        };

        /// <summary>
        /// Creates a GX texture with the specified format and dimensions.
        /// </summary>
        /// <param name="format">The GX image format.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="mipMapCount">The number of mipmaps.</param>
        /// <param name="paletteFormat">The palette format for indexed textures.</param>
        /// <param name="requestedPaletteSize">The requested palette size for indexed textures.</param>
        /// <returns>The created texture.</returns>
        public static IImage CreateGxTexture(this GXImageFormat format, ushort width, ushort height, byte mipMapCount = 0, GXPaletteFormat paletteFormat = GXPaletteFormat.RGB5A3, int requestedPaletteSize = 2048)
        {
            return format switch
            {
                GXImageFormat.I4 => new FlatTexture<I4>(new BlockImage<I4>(new GxI4Block(), width, height), mipMapCount),
                GXImageFormat.I8 => new FlatTexture<I<byte>>(new BlockImage<I<byte>>(new GxI8Block(), width, height), mipMapCount),
                GXImageFormat.IA4 => new FlatTexture<IA8>(new BlockImage<IA8>(new GxIA4Block(), width, height), mipMapCount),
                GXImageFormat.IA8 => new FlatTexture<IA<byte>>(new BlockImage<IA<byte>>(new GxIA8Block(), width, height), mipMapCount),
                GXImageFormat.RGB565 => new FlatTexture<RGB565>(new BlockImage<RGB565>(new GxRGB565Block(), width, height), mipMapCount),
                GXImageFormat.RGB5A3 => new FlatTexture<RGB5A3>(new BlockImage<RGB5A3>(new GxRGB5A3Block(), width, height), mipMapCount),
                GXImageFormat.RGBA32 => new FlatTexture<RGBA<byte>>(new BlockImage<RGBA<byte>>(new GxRGBA32Block(), width, height), mipMapCount),
                GXImageFormat.C4 => CreatePaletteTexture(new GxI4Block()),
                GXImageFormat.C8 => CreatePaletteTexture(new GxI8Block()),
                GXImageFormat.C14X2 => CreatePaletteTexture(new GxI14Block()),
                GXImageFormat.CMPR => new FlatTexture<RGBA<byte>>(new BlockImage<RGBA<byte>>(new GxCMPRBlock(), width, height), mipMapCount),
                _ => throw new NotSupportedException(),
            };

            IImage CreatePaletteTexture<TIndex>(IBlockProcessor<TIndex> blockProcessor) where TIndex : unmanaged, IIndexColor, IColor<TIndex> => paletteFormat switch
            {
                GXPaletteFormat.IA8 => new FlatTexture<IA<byte>>(new PaletteImage<TIndex, IA<byte>>(new BlockImage<TIndex>(blockProcessor, width, height), requestedPaletteSize), mipMapCount),
                GXPaletteFormat.RGB565 => new FlatTexture<RGB565>(new PaletteImage<TIndex, RGB565>(new BlockImage<TIndex>(blockProcessor, width, height), requestedPaletteSize), mipMapCount),
                GXPaletteFormat.RGB5A3 => new FlatTexture<RGB5A3>(new PaletteImage<TIndex, RGB5A3>(new BlockImage<TIndex>(blockProcessor, width, height), requestedPaletteSize), mipMapCount),
                _ => throw new NotSupportedException(),
            };
        }

        /// <summary>
        /// Gets the transparency mode associated with the specified texture format.
        /// </summary>
        /// <param name="format">The GX image format.</param>
        /// <param name="paletteFormat">The palette format for indexed textures.</param>
        /// <returns>The transparency mode associated with the format.</returns>
        public static TransparencyMode GetTransparencyMode(this GXImageFormat format, GXPaletteFormat paletteFormat = GXPaletteFormat.RGB5A3) => format switch
        {
            GXImageFormat.I4 => TransparencyMode.Opaque,
            GXImageFormat.I8 => TransparencyMode.Opaque,
            GXImageFormat.IA4 => TransparencyMode.Straight,
            GXImageFormat.IA8 => TransparencyMode.Straight,
            GXImageFormat.RGB565 => TransparencyMode.Opaque,
            GXImageFormat.RGB5A3 => TransparencyMode.Straight,
            GXImageFormat.RGBA32 => TransparencyMode.Straight,
            GXImageFormat.CMPR => TransparencyMode.Cutout,
            _ => paletteFormat == GXPaletteFormat.RGB565 ? TransparencyMode.Opaque : TransparencyMode.Straight,
        };
    }
}
