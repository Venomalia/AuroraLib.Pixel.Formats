using AuroraLib.Core.Format;
using AuroraLib.Core.Format.Identifier;
using AuroraLib.Core.IO;
using AuroraLib.Pixel.Formats.Dolphin.GXFormat;
using AuroraLib.Pixel.Image;
using AuroraLib.Pixel.Metadata;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AuroraLib.Pixel.Formats.Dolphin
{
    /// <summary>
    /// Nintendo Texture Palette Library
    /// </summary>
    public class TPL : IImageContainerFormat
    {
        private static readonly Identifier32 Identifier = new Identifier32(0x00, 0x20, 0xAF, 0x30);

        /// <inheritdoc/>
        public IFormatInfo Info => _info;

        private static readonly IFormatInfo _info = new FormatInfo<TPL>("Nintendo Texture Palette Library", new MediaType(MIMEType.Image, "x-nintendo-tpl"), ".tpl", Identifier);

        /// <inheritdoc/>
        public bool IsMatch(Stream stream, ReadOnlySpan<char> fileNameAndExtension = default)
            => IsMatchStatic(stream, fileNameAndExtension);

        /// <inheritdoc cref="IsMatch(Stream, ReadOnlySpan{char})"/>
        public static bool IsMatchStatic(Stream stream, ReadOnlySpan<char> fileNameAndExtension = default)
            => stream.Length - stream.Position >= 0x10 && stream.Peek(s => s.Match(Identifier));

        /// <inheritdoc/>
        public void ReadImages(Stream source, ICollection<IImage> images)
        {
            long headerStart = source.Position;
            source.MatchThrow(Identifier);

            int imageCount = source.ReadInt32BigEndian();
            int offsetsTableOffset = source.ReadInt32BigEndian();
            int offsetsTableSize = imageCount * 8;

            source.Seek(headerStart + offsetsTableOffset, SeekOrigin.Begin);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(offsetsTableSize);
            try
            {
                source.ReadExactly(buffer, 0, offsetsTableSize);
                ReadOnlySpan<ImageOffsetEntry> entrys = MemoryMarshal.Cast<byte, ImageOffsetEntry>(buffer.AsSpan(0, offsetsTableSize));
                for (int i = 0; i < imageCount; i++)
                {
                    var entry = entrys[i];

                    GXTexInfo texInfo = new GXTexInfo();
                    uint paletteOffset = 0;
                    if (entry.PaletteHeaderOffset != 0)
                    {
                        source.Seek(headerStart + entry.PaletteHeaderOffset, SeekOrigin.Begin);
                        ReadPaletteHeader(source, texInfo, out paletteOffset);
                    }
                    source.Seek(headerStart + entry.ImageHeaderOffset, SeekOrigin.Begin);
                    ReadImageHeader(source, texInfo, out SamplingInfos samplingInfos, out uint imageOffset);

                    var tex = ReadTexture(source, texInfo, headerStart + imageOffset, headerStart + paletteOffset);
                    tex.Metadata!.SamplingInfos = samplingInfos;
                    images.Add(tex);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        protected virtual void ReadPaletteHeader(Stream source, GXTexInfo texInfo, out uint paletteOffset)
        {
            var header = source.Read<PaletteHeader>();
            texInfo.PaletteCount = header.Count;
            texInfo.PaletteFormat = header.Format;
            paletteOffset = header.Offset;
        }

        protected virtual void ReadImageHeader(Stream source, GXTexInfo texInfo, out SamplingInfos samplingInfos, out uint imageOffset)
        {
            var header = source.Read<ImageHeader>();
            texInfo.Width = header.Width;
            texInfo.Height = header.Height;
            texInfo.Format = header.Format;
            texInfo.MipMapCount = header.MaxLOD;
            imageOffset = header.Offset;

            samplingInfos = new SamplingInfos
            {
                WrapS = header.WrapS,
                WrapT = header.WrapT,
                MinFilter = header.MinFilter,
                MagFilter = header.MagFilter,
                LODBias = header.LODBias,
                MinLOD = header.MinLOD,
                MaxLOD = header.MaxLOD
            };
        }

        protected static IImage ReadTexture(Stream stream, GXTexInfo texInfo, long imageOffset, long paletteOffset)
            => texInfo.ReadGxTexture(stream, imageOffset, paletteOffset);

        /// <inheritdoc/>
        public void WriteImage<TColor>(IReadOnlyImage<TColor> source, Stream destination) where TColor : unmanaged, IColor<TColor>
            => WriteImages(new IReadOnlyImage[] { source }, destination);

        /// <inheritdoc/>
        public void WriteImages(IEnumerable<IReadOnlyImage> source, Stream destination)
        {
            const int offsetsTableOffset = 0x0c;
            int imageCount = source.Count();
            long headerStart = destination.Position;

            destination.Write(Identifier);

            destination.Write(imageCount, Endian.Big);
            destination.Write(offsetsTableOffset, Endian.Big);

            int offsetsTableSize = imageCount * 8;
            int imageHeaderTableSize = imageCount * Unsafe.SizeOf<ImageHeader>();
            byte[] buffer = ArrayPool<byte>.Shared.Rent(offsetsTableSize + imageHeaderTableSize);
            try
            {
                // Placeholder for the offset and image header tables, which are rewritten after all data offsets are known.
                destination.Write(buffer, 0, offsetsTableSize + imageHeaderTableSize);
                destination.WriteAlign(0x20);
                Span<ImageOffsetEntry> offsetTabel = MemoryMarshal.Cast<byte, ImageOffsetEntry>(buffer.AsSpan(0, offsetsTableSize));
                Span<ImageHeader> imageHeaderTabel = MemoryMarshal.Cast<byte, ImageHeader>(buffer.AsSpan(offsetsTableSize, imageHeaderTableSize));

                int i = 0;
                foreach (var image in source)
                {
                    var gxInfo = GXTexInfo.Create(image);

                    offsetTabel[i].ImageHeaderOffset = (uint)(offsetsTableOffset + offsetsTableSize + i * Unsafe.SizeOf<ImageHeader>());
                    offsetTabel[i].PaletteHeaderOffset = 0;
                    uint pos = (uint)(destination.Position - headerStart);
                    imageHeaderTabel[i] = new ImageHeader(gxInfo, image.Metadata?.SamplingInfos, pos);

                    gxInfo.WriteTexture(destination, image, out byte[]? palette);

                    if (gxInfo.IsPaletteFormat)
                    {
                        // Palettes have their own header and are stored separately from the image data.
                        pos = (uint)(destination.Position - headerStart);
                        offsetTabel[i].PaletteHeaderOffset = pos;
                        destination.Write(new PaletteHeader(gxInfo.PaletteCount, gxInfo.PaletteFormat, pos + 0x20));
                        destination.WriteAlign(0x20);
                        destination.Write(palette!, 0, palette!.Length);
                        destination.WriteAlign(0x20);
                    }
                    i++;
                }

                // Rewrite the completed tables.
                destination.At(headerStart + offsetsTableOffset, s => s.Write(buffer, 0, offsetsTableSize + imageHeaderTableSize));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        protected struct ImageOffsetEntry
        {
            private uint _imageHeaderOffset;
            private uint _paletteHeaderOffset;
            public uint ImageHeaderOffset { readonly get => BinaryPrimitives.ReverseEndianness(_imageHeaderOffset); set => _imageHeaderOffset = BinaryPrimitives.ReverseEndianness(value); }
            public uint PaletteHeaderOffset { readonly get => BinaryPrimitives.ReverseEndianness(_paletteHeaderOffset); set => _paletteHeaderOffset = BinaryPrimitives.ReverseEndianness(value); }
        }

        protected struct ImageHeader
        {
            private ushort _height;
            private ushort _width;
            private uint _format;
            private uint _offset;
            private uint _wrapS;
            private uint _wrapT;
            private uint _minFilter;
            private uint _maxFilter;
            private uint _LODBias;
            public byte EdgeLOD;
            public byte MinLOD;
            public byte MaxLOD;
            public byte Unpacked;

            public ImageHeader(GXTexInfo texInfo, SamplingInfos? samplingInfos, uint offset) : this()
            {
                Width = texInfo.Width;
                Height = texInfo.Height;
                Format = texInfo.Format;
                MaxLOD = texInfo.MipMapCount;
                Offset = offset;
                if (samplingInfos != null)
                {
                    WrapS = samplingInfos.WrapS;
                    WrapT = samplingInfos.WrapT;
                    MinFilter = samplingInfos.MinFilter;
                    MagFilter = samplingInfos.MagFilter;
                    LODBias = samplingInfos.LODBias;
                    MinLOD = (byte)samplingInfos.MinLOD;
                }
                if (WrapS > TextureWrapMode.MirroredRepeat)
                    WrapS = TextureWrapMode.ClampToEdge;

                if (WrapT > TextureWrapMode.MirroredRepeat)
                    WrapT = TextureWrapMode.ClampToEdge;

                if (MagFilter > TextureFilter.Linear)
                    MagFilter = TextureFilter.Linear;
            }

            public ushort Width { readonly get => BinaryPrimitives.ReverseEndianness(_width); set => _width = BinaryPrimitives.ReverseEndianness(value); }
            public ushort Height { readonly get => BinaryPrimitives.ReverseEndianness(_height); set => _height = BinaryPrimitives.ReverseEndianness(value); }
            public GXImageFormat Format { readonly get => (GXImageFormat)BinaryPrimitives.ReverseEndianness(_format); set => _format = BinaryPrimitives.ReverseEndianness((uint)value); }
            public uint Offset { readonly get => BinaryPrimitives.ReverseEndianness(_offset); set => _offset = BinaryPrimitives.ReverseEndianness(value); }
            public TextureWrapMode WrapS { readonly get => (TextureWrapMode)BinaryPrimitives.ReverseEndianness(_wrapS); set => _wrapS = BinaryPrimitives.ReverseEndianness((uint)value); }
            public TextureWrapMode WrapT { readonly get => (TextureWrapMode)BinaryPrimitives.ReverseEndianness(_wrapT); set => _wrapT = BinaryPrimitives.ReverseEndianness((uint)value); }
            public TextureFilter MinFilter { readonly get => (TextureFilter)BinaryPrimitives.ReverseEndianness(_minFilter); set => _minFilter = BinaryPrimitives.ReverseEndianness((uint)value); }
            public TextureFilter MagFilter { readonly get => (TextureFilter)BinaryPrimitives.ReverseEndianness(_maxFilter); set => _maxFilter = BinaryPrimitives.ReverseEndianness((uint)value); }
            public float LODBias { readonly get => (float)BinaryPrimitives.ReverseEndianness(_LODBias); set => _LODBias = BinaryPrimitives.ReverseEndianness((uint)value); }
        }

        private struct PaletteHeader
        {
            private ushort _count;
            public byte Unpacked;
            public byte Pad;
            private uint _format;
            private uint _offset;

            public ushort Count { readonly get => BinaryPrimitives.ReverseEndianness(_count); set => _count = BinaryPrimitives.ReverseEndianness(value); }
            public GXPaletteFormat Format { readonly get => (GXPaletteFormat)BinaryPrimitives.ReverseEndianness(_format); set => _format = BinaryPrimitives.ReverseEndianness((uint)value); }
            public uint Offset { readonly get => BinaryPrimitives.ReverseEndianness(_offset); set => _offset = BinaryPrimitives.ReverseEndianness(value); }

            public PaletteHeader(ushort count, GXPaletteFormat format, uint paletteDataAddress) : this()
            {
                Count = count;
                Format = format;
                Offset = paletteDataAddress;
            }
        }
    }
}
