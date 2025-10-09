using AuroraLib.Core.Format;
using AuroraLib.Core.IO;
using AuroraLib.Pixel.BlockProcessor;
using AuroraLib.Pixel.Formats.Dolphin.GXFormat;
using AuroraLib.Pixel.Image;
using System;
using System.Buffers.Binary;
using System.IO;

namespace AuroraLib.Pixel.Formats.Dolphin
{
    /// <summary>
    /// Dolphin TXE is a very rare and simple image format only found and used in Pikmin.
    /// </summary>
    // https://pikmintkb.com/wiki/TXE_file
    public sealed class TXE : IImageFormat
    {
        private const ushort OldTex = 2;
        private const string Extension = ".txe";
        /// <inheritdoc/>
        public IFormatInfo Info => _info;

        private static readonly IFormatInfo _info = new FormatInfo<TXE>("Nintendo Dolphin Texture", new MediaType(MIMEType.Image, "x-dolphin-txe"), Extension);

        /// <summary>
        /// Gets or sets a value indicating whether the TXE file uses the mTXE MOD file format variant.
        /// </summary>
        public bool UseModTex = false;

        /// <inheritdoc/>
        public bool IsMatch(Stream stream, ReadOnlySpan<char> fileNameAndExtension = default)
            => IsMatchStatic(stream, fileNameAndExtension);

        /// <inheritdoc cref="IsMatch(Stream, ReadOnlySpan{char})"/>
        public static bool IsMatchStatic(Stream stream, ReadOnlySpan<char> fileNameAndExtension = default)
        {
            return stream.Length - stream.Position >= 0x20 &&
                fileNameAndExtension.Contains(Extension.AsSpan(), StringComparison.InvariantCultureIgnoreCase) &&
                stream.Peek(s =>
            {
                ReadHeader(s, out GXTexInfo texInfo, out long imageOffset);
                return texInfo.IsValid && imageOffset != 0;
            });
        }

        /// <inheritdoc/>
        public IImage ReadImage(Stream source)
        {
            ReadHeader(source, out GXTexInfo texInfo, out long imageOffset);
            return texInfo.ReadGxTexture(source, imageOffset, 0);
        }

        private static void ReadHeader(Stream source, out GXTexInfo texInfo, out long imageOffset)
        {
            texInfo = new GXTexInfo();
            imageOffset = 0;

            texInfo.Width = source.ReadUInt16BigEndian();
            texInfo.Height = source.ReadUInt16BigEndian();
            ushort type = source.ReadUInt16BigEndian(); // ModTex = 0, OldTex = 2
            if (type != 0 && type != OldTex)
                return;

            texInfo.Format = ToGxFormat((TEXImageFormat)source.ReadUInt16BigEndian(), type == OldTex);
            if (type == 0)
            {
                _ = (float)source.ReadUInt32BigEndian(); // maxLOD, mips
                source.Seek(16, SeekOrigin.Current);
                imageOffset = source.Position + 4;
            }
            else
            {
                imageOffset = source.Position + 20 + 4;
            }
            int dataSize = source.ReadInt32BigEndian();
            texInfo.MipMapCount = (byte)texInfo.Format.GetBlockFormat().CalculateMipMapCount(texInfo.Width, texInfo.Height, dataSize);
        }

        public void WriteImage<TColor>(IReadOnlyImage<TColor> source, Stream destination) where TColor : unmanaged, IColor<TColor>
        {
            var gxInfo = GXTexInfo.Create(source);
            if (gxInfo.IsPaletteFormat)
                gxInfo.Format = gxInfo.PaletteFormat switch
                {
                    GXPaletteFormat.IA8 => GXImageFormat.IA8,
                    GXPaletteFormat.RGB565 => GXImageFormat.RGB565,
                    GXPaletteFormat.RGB5A3 => GXImageFormat.RGB5A3,
                    _ => throw new NotSupportedException(),
                };

            destination.Write((ushort)BinaryPrimitives.ReverseEndianness(gxInfo.Width));
            destination.Write((ushort)BinaryPrimitives.ReverseEndianness(gxInfo.Height));
            destination.Write((ushort)BinaryPrimitives.ReverseEndianness((ushort)(UseModTex ? 0 : OldTex)));
            destination.Write((ushort)BinaryPrimitives.ReverseEndianness((ushort)ToTEXFormat(gxInfo.Format, !UseModTex)));
            if (UseModTex)
            {
                destination.Write(BinaryPrimitives.ReverseEndianness((uint)gxInfo.MipMapCount));
                destination.Write(0, 16 / 4);
                destination.Write(BinaryPrimitives.ReverseEndianness((uint)gxInfo.CalculatedDataSize()));
            }
            else
            {
                destination.Write(BinaryPrimitives.ReverseEndianness((uint)gxInfo.CalculatedDataSize()));
                destination.Write(0, 20 / 4);
            }
            gxInfo.WriteTexture(destination, source, out var _);
        }

        private static GXImageFormat ToGxFormat(TEXImageFormat format, bool IsOld) => format switch
        {
            TEXImageFormat.RGB565 => IsOld ? GXImageFormat.RGB5A3 : GXImageFormat.RGB565,
            TEXImageFormat.CMPR => GXImageFormat.CMPR,
            TEXImageFormat.RGB5A3 => IsOld ? GXImageFormat.RGB565 : GXImageFormat.RGB5A3,
            TEXImageFormat.I4 => GXImageFormat.I4,
            TEXImageFormat.I8 => GXImageFormat.I8,
            TEXImageFormat.IA4 => GXImageFormat.IA4,
            TEXImageFormat.IA8 => GXImageFormat.IA8,
            TEXImageFormat.RGBA32 => GXImageFormat.RGBA32,
            _ => (GXImageFormat)byte.MaxValue,
        };

        private static TEXImageFormat ToTEXFormat(GXImageFormat format, bool IsOld) => format switch
        {
            GXImageFormat.I4 => TEXImageFormat.I4,
            GXImageFormat.I8 => TEXImageFormat.I8,
            GXImageFormat.IA4 => TEXImageFormat.IA4,
            GXImageFormat.IA8 => TEXImageFormat.IA8,
            GXImageFormat.RGB565 => IsOld ? TEXImageFormat.RGB5A3 : TEXImageFormat.RGB565,
            GXImageFormat.RGB5A3 => IsOld ? TEXImageFormat.RGB565 : TEXImageFormat.RGB5A3,
            GXImageFormat.RGBA32 => TEXImageFormat.RGBA32,
            GXImageFormat.CMPR => TEXImageFormat.CMPR,
            _ => throw new NotSupportedException(),
        };

        private enum TEXImageFormat : ushort
        {
            RGB565, // or old RGB5A3!
            CMPR,
            RGB5A3, // or old RGB565!
            I4,
            I8,
            IA4,
            IA8,
            RGBA32,
        }
    }
}
