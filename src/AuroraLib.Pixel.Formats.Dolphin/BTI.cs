using AuroraLib.Core;
using AuroraLib.Core.Format;
using AuroraLib.Core.IO;
using AuroraLib.Pixel.Formats.Dolphin.GXFormat;
using AuroraLib.Pixel.Image;
using AuroraLib.Pixel.Metadata;
using System;
using System.Buffers.Binary;
using System.IO;

namespace AuroraLib.Pixel.Formats.Dolphin
{
    /// <summary>
    /// Nintendo Binary Texture Image
    /// </summary>
    public sealed class BTI : IImageFormat
    {
        /// <inheritdoc/>
        public IFormatInfo Info => _info;

        private static readonly IFormatInfo _info = new FormatInfo<BTI>("Nintendo Binary Texture Image", new MediaType(MIMEType.Image, "x-nintendo-bti"), ".bti");

        /// <inheritdoc/>
        public bool IsMatch(Stream stream, ReadOnlySpan<char> fileNameAndExtension = default)
            => IsMatchStatic(stream, fileNameAndExtension);

        /// <inheritdoc cref="IsMatch(Stream, ReadOnlySpan{char})"/>
        public static bool IsMatchStatic(Stream stream, ReadOnlySpan<char> fileNameAndExtension = default)
        {
            return stream.Length - stream.Position >= 0x20 && stream.Peek(s =>
            {
                ReadHeader(s, out GXTexInfo texInfo, out SamplingInfos samplingInfos, out uint imageOffset, out uint paletteOffset);
                return texInfo.IsValid &&
                Enum.IsDefined(typeof(TextureWrapMode), samplingInfos.WrapS) && Enum.IsDefined(typeof(TextureWrapMode), samplingInfos.WrapT) &&
                (samplingInfos.MagFilter == TextureFilter.Nearest || samplingInfos.MagFilter == TextureFilter.Linear) &&
                Enum.IsDefined(typeof(TextureFilter), samplingInfos.MinFilter) &&
                samplingInfos.LODBias >= -4.0f && samplingInfos.LODBias <= 3.99f &&
                samplingInfos.MinLOD >= 0 && samplingInfos.MaxLOD >= 0 && samplingInfos.MinLOD <= samplingInfos.MaxLOD && samplingInfos.MaxLOD <= 10 &&
                samplingInfos.MaxAnisotropy < 4 && Enum.IsDefined(typeof(TransparencyMode), samplingInfos.TransparencyMode) && imageOffset != 0;
            });
        }

        /// <inheritdoc/>
        public IImage ReadImage(Stream source)
        {
            ReadHeader(source, out GXTexInfo texInfo, out SamplingInfos samplingInfos, out uint imageOffset, out uint paletteOffset);
            var tex = texInfo.ReadGxTexture(source, imageOffset, paletteOffset);
            tex.Metadata!.SamplingInfos = samplingInfos;
            return tex;
        }

        private static void ReadHeader(Stream source, out GXTexInfo texInfo, out SamplingInfos samplingInfos, out uint imageOffset, out uint paletteOffset)
        {
            // read 32 byte header
            texInfo = new GXTexInfo();
            samplingInfos = new SamplingInfos();
            texInfo.Format = (GXImageFormat)source.ReadByte();
            samplingInfos.TransparencyMode = (TransparencyMode)source.ReadByte();
            texInfo.Width = source.ReadUInt16BigEndian();
            texInfo.Height = source.ReadUInt16BigEndian();
            samplingInfos.WrapS = (TextureWrapMode)source.ReadByte();
            samplingInfos.WrapT = (TextureWrapMode)source.ReadByte();
            byte isPaletteValue = (byte)source.ReadByte();
            texInfo.PaletteFormat = (GXPaletteFormat)source.ReadByte();
            texInfo.PaletteCount = source.ReadUInt16BigEndian();
            paletteOffset = source.ReadUInt32BigEndian();
            texInfo.EnableMips = source.ReadByte() == 1;
            bool useEdgeLod = source.ReadByte() == 1;
            bool useBiasClamp = source.ReadByte() == 1;
            samplingInfos.MaxAnisotropy = source.ReadByte() * 2;
            if (samplingInfos.MaxAnisotropy <= 0) samplingInfos.MaxAnisotropy = 1;
            samplingInfos.MagFilter = (TextureFilter)source.ReadByte();
            samplingInfos.MinFilter = (TextureFilter)source.ReadByte();
            samplingInfos.MinLOD = source.ReadInt8() / 8f;
            samplingInfos.MaxLOD = source.ReadInt8() / 8f;
            texInfo.MipMapCount = (byte)(source.ReadByte() - 1);
            byte unknown = (byte)source.ReadByte();
            samplingInfos.LODBias = source.ReadInt16BigEndian() / 100f;
            imageOffset = source.ReadUInt32BigEndian();
        }

        /// <inheritdoc/>
        public void WriteImage<TColor>(IReadOnlyImage<TColor> source, Stream destination) where TColor : unmanaged, IColor<TColor>
        {
            var gxInfo = GXTexInfo.Create(source);
            uint paletteOffset = gxInfo.IsPaletteFormat ? (uint)(0x20 + gxInfo.CalculatedDataSize()) : 0u;

            WriteHeader(destination, gxInfo, source.Metadata?.SamplingInfos, 0x20u, paletteOffset);
           
            gxInfo.WriteTexture(destination, source, out byte[]? palette);
            if (palette != null)
                destination.Write(palette, 0, palette.Length);
        }

        private static void WriteHeader(Stream destination, GXTexInfo texInfo, SamplingInfos? samplingInfos, uint imageOffset, uint paletteOffset)
        {
            samplingInfos ??= new SamplingInfos() { TransparencyMode = texInfo.Format.GetTransparencyMode(texInfo.PaletteFormat), MaxLOD = texInfo.MipMapCount };
            destination.WriteByte((byte)texInfo.Format);
            destination.WriteByte((byte)(samplingInfos.TransparencyMode));
            destination.Write((ushort)BinaryPrimitives.ReverseEndianness(texInfo.Width));
            destination.Write((ushort)BinaryPrimitives.ReverseEndianness(texInfo.Height));
            destination.WriteByte((byte)(samplingInfos.WrapS));
            destination.WriteByte((byte)(samplingInfos.WrapT));

            destination.WriteByte(texInfo.IsPaletteFormat ? (byte)1 : (byte)0);
            destination.WriteByte((byte)texInfo.PaletteFormat);
            destination.Write((ushort)BinaryPrimitives.ReverseEndianness(texInfo.PaletteCount));
            destination.Write((uint)BinaryPrimitives.ReverseEndianness(paletteOffset));

            bool enableMips = texInfo.EnableMips ?? texInfo.MipMapCount != 0;
            destination.WriteByte(enableMips ? (byte)1 : (byte)0);
            destination.WriteByte(samplingInfos.MaxAnisotropy > 1 ? (byte)1 : (byte)0); // EdgeLod.
            destination.WriteByte(0); // BiasClamp virtually never used?
            destination.WriteByte((byte)(samplingInfos.MaxAnisotropy / 2).Clamp(0, 2));

            destination.WriteByte(((byte)samplingInfos.MagFilter).Clamp((byte)0, (byte)1));
            destination.WriteByte((byte)(samplingInfos.MaxAnisotropy > 1 ? TextureFilter.LinearMipmapLinear : samplingInfos.MinFilter));
            destination.WriteByte((byte)(samplingInfos.MinLOD.Clamp(0, 10) * 8f));
            destination.WriteByte((byte)(samplingInfos.MaxLOD.Clamp(0, 10) * 8f));

            destination.WriteByte((byte)(texInfo.MipMapCount + 1));
            destination.WriteByte(0); // Padding

            destination.Write((short)BinaryPrimitives.ReverseEndianness((short)(samplingInfos.LODBias.Clamp(-4, 4) * 100f)));
            destination.Write((uint)BinaryPrimitives.ReverseEndianness(imageOffset));
        }
    }
}
