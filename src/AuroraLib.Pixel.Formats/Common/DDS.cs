using AuroraLib.Core.Format;
using AuroraLib.Core.Format.Identifier;
using AuroraLib.Core.IO;
using AuroraLib.Pixel.BlockProcessor;
using AuroraLib.Pixel.Image;
using AuroraLib.Pixel.Metadata;
using AuroraLib.Pixel.PixelFormats;
using AuroraLib.Pixel.Processing;
using AuroraLib.Pixel.Processing.Analyzer;
using AuroraLib.Pixel.Texture;
using System;
using System.Collections.Generic;
using System.IO;

namespace AuroraLib.Pixel.Formats.Common
{
    /// <summary>
    /// Microsoft DirectDraw Surface texture container.
    /// </summary>
    public sealed partial class DDS : IImageFormat
    {
        private static readonly Identifier32 _Identifier = new Identifier32("DDS ".AsSpan());

        /// <inheritdoc/>
        public IFormatInfo Info => _info;

        private static readonly IFormatInfo _info = new FormatInfo<DDS>("Microsoft DirectDraw Surface", new MediaType(MIMEType.Image, "vnd-ms.dds"), ".dds", _Identifier);

        /// <summary>
        /// Gets or sets whether to force writing a DX10 header.
        /// </summary>
        public bool ForceDXT10Header { get; set; } = false;


        /// <inheritdoc/>
        public bool IsMatch(Stream stream, ReadOnlySpan<char> fileNameAndExtension = default)
            => IsMatchStatic(stream, fileNameAndExtension);

        /// <inheritdoc cref="IsMatch(Stream, ReadOnlySpan{char})"/>
        public static bool IsMatchStatic(Stream stream, ReadOnlySpan<char> fileNameAndExtension = default)
            => stream.Length > 124 && stream.Peek(s => s.Match(_Identifier) && s.ReadUInt32() == 124);

        #region ReadImage (Decode)
        /// <inheritdoc/>
        public IImage ReadImage(Stream source)
        {
            source.MatchThrow(_Identifier);
            Header header = source.Read<Header>();
            return header.Format.FourCC switch
            {
                FourCCType.None => ReadImageRGB(source, header),
                FourCCType.DXT1 => ReadTexture(source, header, new BC1Block<RGBA<byte>>()),
                FourCCType.DXT2 => ReadTexture(source, header, new BC2Block<RGBA<byte>>()),
                FourCCType.DXT3 => ReadTexture(source, header, new BC2Block<RGBA<byte>>()),
                FourCCType.DXT4 => ReadTexture(source, header, new BC3Block<RGBA<byte>>()),
                FourCCType.DXT5 => ReadTexture(source, header, new BC3Block<RGBA<byte>>()),
                FourCCType.DX10 => ReadImageDXT10(source, header),
                FourCCType.BC4U => ReadTexture(source, header, new BC4UBlock<I<byte>>()),
                FourCCType.BC4S => ReadTexture(source, header, new BC4SBlock<I<sbyte>>()),
                FourCCType.BC5U => ReadTexture(source, header, new BC5UBlock()),
                FourCCType.BC5S => ReadTexture(source, header, new BC5SBlock()),
                FourCCType.ATI1 => ReadTexture(source, header, new BC4UBlock<I<byte>>()),
                FourCCType.ATI2 => ReadTexture(source, header, new BC5UBlock()),
                FourCCType.RGBG => ReadTexture<RGBA<byte>>(source, header),//!!!
                FourCCType.GRGB => ReadTexture<ARGB<byte>>(source, header),//!!!
                FourCCType.UYVY => ReadTexture(source, header, new UYVY<YUV3>()),
                FourCCType.YUY2 => ReadTexture(source, header, new YUY2<YUV3>()),
                FourCCType.R16G16B16A16_UNORM => ReadTexture<RGBA<ushort>>(source, header),
                FourCCType.R16G16B16A16_SNORM => ReadTexture<RGBA<short>>(source, header),
                FourCCType.R16G16B16A16_FLOAT => ReadTexture<RGBA<Half>>(source, header),
                FourCCType.R16_FLOAT => ReadTexture<I<Half>>(source, header),
                FourCCType.R16G16_FLOAT => ReadTexture<IA<Half>>(source, header),
                FourCCType.R32_FLOAT => ReadTexture<I<float>>(source, header),
                FourCCType.R32G32_FLOAT => ReadTexture<IA<float>>(source, header),
                FourCCType.R32G32B32A32_FLOAT => ReadTexture<RGBA<float>>(source, header),
                //FourCCType.MET1 => throw new NotImplementedException(),
                //FourCCType.CxV8U8 => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
        }

        private IImage ReadImageRGB(Stream source, Header header)
        {
            PixelFormatInfo formatInfo = header.Format.FormatInfo;

            switch (header.Format.RGBBitCount)
            {
                case 8:
                    if (formatInfo.Equals(A<byte>.FormatInfo)) return ReadTexture<A<byte>>(source, header);
                    if (formatInfo.Equals(I<byte>.FormatInfo)) return ReadTexture<I<byte>>(source, header);
                    if (formatInfo.Equals(IA8.FormatInfo)) return ReadTexture<IA8>(source, header);
                    break;
                case 16:
                    if (formatInfo.Equals(BGR555.FormatInfo)) return ReadTexture<BGR555>(source, header);
                    if (formatInfo.Equals(RGB555.FormatInfo)) return ReadTexture<RGB555>(source, header);
                    if (formatInfo.Equals(RGB565.FormatInfo)) return ReadTexture<RGB565>(source, header);
                    if (formatInfo.Equals(ARGB1555.FormatInfo)) return ReadTexture<ARGB1555>(source, header);
                    if (formatInfo.Equals(ARGB16.FormatInfo)) return ReadTexture<ARGB16>(source, header);
                    if (formatInfo.Equals(RGBA16.FormatInfo)) return ReadTexture<RGBA16>(source, header);
                    if (formatInfo.Equals(IA<byte>.FormatInfo)) return ReadTexture<IA<byte>>(source, header);
                    if (formatInfo.Equals(A<ushort>.FormatInfo)) return ReadTexture<A<ushort>>(source, header);
                    if (formatInfo.Equals(I<ushort>.FormatInfo)) return ReadTexture<I<ushort>>(source, header);
                    break;
                case 24:
                    if (formatInfo.Equals(RGB<byte>.FormatInfo)) return ReadTexture<RGB<byte>>(source, header);
                    if (formatInfo.Equals(BGR<byte>.FormatInfo)) return ReadTexture<BGR<byte>>(source, header);
                    if (formatInfo.Equals(YUV3.FormatInfo)) return ReadTexture<YUV3>(source, header);
                    break;
                case 32:
                    if (formatInfo.Equals(RGBA<byte>.FormatInfo)) return ReadTexture<RGBA<byte>>(source, header);
                    if (formatInfo.Equals(ARGB<byte>.FormatInfo)) return ReadTexture<ARGB<byte>>(source, header);
                    if (formatInfo.Equals(BGRA<byte>.FormatInfo)) return ReadTexture<BGRA<byte>>(source, header);
                    if (formatInfo.Equals(ABGR<byte>.FormatInfo)) return ReadTexture<ABGR<byte>>(source, header);
                    if (formatInfo.Equals(BGRA1010102.FormatInfo)) return ReadTexture<BGRA1010102>(source, header);
                    if (formatInfo.Equals(RGBA1010102.FormatInfo)) return ReadTexture<RGBA1010102>(source, header);
                    if (formatInfo.Equals(AYUV.FormatInfo)) return ReadTexture<AYUV>(source, header);
                    if (formatInfo.Equals(Y410.FormatInfo)) return ReadTexture<Y410>(source, header);
                    if (formatInfo.Equals(IA<ushort>.FormatInfo)) return ReadTexture<IA<ushort>>(source, header);
                    if (formatInfo.Equals(I<uint>.FormatInfo)) return ReadTexture<I<uint>>(source, header);
                    if (formatInfo.Equals(new PixelFormatInfo(32, 8, 0, 8, 8, 8, 16, 0, 0))) return ReadTexture<RGBA<byte>>(source, header);//RGBX
                    break;
                default:
                    break;
            }

            throw new NotImplementedException();
        }
        private IImage ReadImageDXT10(Stream source, Header header)
        {
            DXT10Header dXT10Header = source.Read<DXT10Header>();
            return dXT10Header.Format switch
            {
                DXGIFormats.None => ReadImageRGB(source, header),
                DXGIFormats.R32G32B32A32_TYPELESS => ReadTexture<RGBA<float>>(source, header),
                DXGIFormats.R32G32B32A32_FLOAT => ReadTexture<RGBA<float>>(source, header),
                DXGIFormats.R32G32B32A32_UINT => ReadTexture<RGBA<uint>>(source, header),
                DXGIFormats.R32G32B32A32_SINT => ReadTexture<RGBA<int>>(source, header),
                DXGIFormats.R32G32B32_TYPELESS => ReadTexture<RGB<uint>>(source, header),
                DXGIFormats.R32G32B32_FLOAT => ReadTexture<RGB<float>>(source, header),
                DXGIFormats.R32G32B32_UINT => ReadTexture<RGB<uint>>(source, header),
                DXGIFormats.R32G32B32_SINT => ReadTexture<RGB<int>>(source, header),
                DXGIFormats.R16G16B16A16_TYPELESS => ReadTexture<RGBA<ushort>>(source, header),
                DXGIFormats.R16G16B16A16_FLOAT => ReadTexture<RGBA<Half>>(source, header),
                DXGIFormats.R16G16B16A16_UNORM => ReadTexture<RGBA<ushort>>(source, header),
                DXGIFormats.R16G16B16A16_UINT => ReadTexture<RGBA<ushort>>(source, header),
                DXGIFormats.R16G16B16A16_SNORM => ReadTexture<RGBA<short>>(source, header),
                DXGIFormats.R16G16B16A16_SINT => ReadTexture<RGBA<short>>(source, header),
                DXGIFormats.R32G32_TYPELESS => ReadTexture<IA<uint>>(source, header),
                DXGIFormats.R32G32_FLOAT => ReadTexture<IA<float>>(source, header),
                DXGIFormats.R32G32_UINT => ReadTexture<IA<uint>>(source, header),
                DXGIFormats.R32G32_SINT => ReadTexture<IA<int>>(source, header),
                DXGIFormats.R32G8X24_TYPELESS => throw new NotImplementedException(),
                DXGIFormats.D32_FLOAT_S8X24_UINT => throw new NotImplementedException(),
                DXGIFormats.R32_FLOAT_X8X24_TYPELESS => throw new NotImplementedException(),
                DXGIFormats.X32_TYPELESS_G8X24_UINT => throw new NotImplementedException(),
                DXGIFormats.R10G10B10A2_TYPELESS => ReadTexture<RGBA1010102>(source, header),
                DXGIFormats.R10G10B10A2_UNORM => ReadTexture<RGBA1010102>(source, header),
                DXGIFormats.R10G10B10A2_UINT => ReadTexture<RGBA1010102>(source, header),
                DXGIFormats.R11G11B10_FLOAT => throw new NotImplementedException(),
                DXGIFormats.R8G8B8A8_TYPELESS => ReadTexture<RGBA<byte>>(source, header),
                DXGIFormats.R8G8B8A8_UNORM => ReadTexture<RGBA<byte>>(source, header),
                DXGIFormats.R8G8B8A8_UNORM_SRGB => ReadTexture<RGBA<byte>>(source, header),
                DXGIFormats.R8G8B8A8_UINT => ReadTexture<RGBA<byte>>(source, header),
                DXGIFormats.R8G8B8A8_SNORM => ReadTexture<RGBA<sbyte>>(source, header),
                DXGIFormats.R8G8B8A8_SINT => ReadTexture<RGBA<sbyte>>(source, header),
                DXGIFormats.R16G16_TYPELESS => ReadTexture<IA<ushort>>(source, header),
                DXGIFormats.R16G16_FLOAT => ReadTexture<IA<Half>>(source, header),
                DXGIFormats.R16G16_UNORM => ReadTexture<IA<ushort>>(source, header),
                DXGIFormats.R16G16_UINT => ReadTexture<IA<ushort>>(source, header),
                DXGIFormats.R16G16_SNORM => ReadTexture<I<short>>(source, header),
                DXGIFormats.R16G16_SINT => ReadTexture<IA<short>>(source, header),
                DXGIFormats.R32_TYPELESS => ReadTexture<I<uint>>(source, header),
                DXGIFormats.D32_FLOAT => ReadTexture<I<float>>(source, header),
                DXGIFormats.R32_FLOAT => ReadTexture<I<float>>(source, header),
                DXGIFormats.R32_UINT => ReadTexture<I<uint>>(source, header),
                DXGIFormats.R32_SINT => ReadTexture<I<int>>(source, header),
                DXGIFormats.R24G8_TYPELESS => throw new NotImplementedException(),
                DXGIFormats.D24_UNORM_S8_UINT => throw new NotImplementedException(),
                DXGIFormats.R24_UNORM_X8_TYPELESS => throw new NotImplementedException(),
                DXGIFormats.X24_TYPELESS_G8_UINT => throw new NotImplementedException(),
                DXGIFormats.R8G8_TYPELESS => ReadTexture<IA<byte>>(source, header),
                DXGIFormats.R8G8_UNORM => ReadTexture<IA<byte>>(source, header),
                DXGIFormats.R8G8_UINT => ReadTexture<IA<byte>>(source, header),
                DXGIFormats.R8G8_SNORM => ReadTexture<IA<sbyte>>(source, header),
                DXGIFormats.R8G8_SINT => ReadTexture<IA<sbyte>>(source, header),
                DXGIFormats.R16_TYPELESS => ReadTexture<I<ushort>>(source, header),
                DXGIFormats.R16_FLOAT => ReadTexture<I<Half>>(source, header),
                DXGIFormats.D16_UNORM => ReadTexture<I<ushort>>(source, header),
                DXGIFormats.R16_UNORM => ReadTexture<I<ushort>>(source, header),
                DXGIFormats.R16_UINT => ReadTexture<I<ushort>>(source, header),
                DXGIFormats.R16_SNORM => ReadTexture<I<short>>(source, header),
                DXGIFormats.R16_SINT => ReadTexture<I<short>>(source, header),
                DXGIFormats.R8_TYPELESS => ReadTexture<I<byte>>(source, header),
                DXGIFormats.R8_UNORM => ReadTexture<I<byte>>(source, header),
                DXGIFormats.R8_UINT => ReadTexture<I<byte>>(source, header),
                DXGIFormats.R8_SNORM => ReadTexture<I<sbyte>>(source, header),
                DXGIFormats.R8_SINT => ReadTexture<I<sbyte>>(source, header),
                DXGIFormats.A8_UNORM => ReadTexture<A<byte>>(source, header),
                DXGIFormats.R1_UNORM => throw new NotImplementedException(),
                DXGIFormats.R9G9B9E5_SHAREDEXP => throw new NotImplementedException(),
                DXGIFormats.R8G8_B8G8_UNORM => ReadTexture<RGBA<byte>>(source, header), //!!!
                DXGIFormats.G8R8_G8B8_UNORM => ReadTexture<ARGB<byte>>(source, header), //!!!
                DXGIFormats.BC1_TYPELESS => ReadTexture(source, header, new BC1Block<RGBA<byte>>()),
                DXGIFormats.BC1_UNORM => ReadTexture(source, header, new BC1Block<RGBA<byte>>()),
                DXGIFormats.BC1_UNORM_SRGB => ReadTexture(source, header, new BC1Block<RGBA<byte>>()),
                DXGIFormats.BC2_TYPELESS => ReadTexture(source, header, new BC2Block<RGBA<byte>>()),
                DXGIFormats.BC2_UNORM => ReadTexture(source, header, new BC2Block<RGBA<byte>>()),
                DXGIFormats.BC2_UNORM_SRGB => ReadTexture(source, header, new BC2Block<RGBA<byte>>()),
                DXGIFormats.BC3_TYPELESS => ReadTexture(source, header, new BC3Block<RGBA<byte>>()),
                DXGIFormats.BC3_UNORM => ReadTexture(source, header, new BC3Block<RGBA<byte>>()),
                DXGIFormats.BC3_UNORM_SRGB => ReadTexture(source, header, new BC3Block<RGBA<byte>>()),
                DXGIFormats.BC4_TYPELESS => ReadTexture(source, header, new BC4UBlock<I<byte>>()),
                DXGIFormats.BC4_UNORM => ReadTexture(source, header, new BC4UBlock<I<byte>>()),
                DXGIFormats.BC4_SNORM => ReadTexture(source, header, new BC4SBlock<I<sbyte>>()),
                DXGIFormats.BC5_TYPELESS => ReadTexture(source, header, new BC5UBlock()),
                DXGIFormats.BC5_UNORM => ReadTexture(source, header, new BC5UBlock()),
                DXGIFormats.BC5_SNORM => ReadTexture(source, header, new BC5SBlock()),
                DXGIFormats.B5G6R5_UNORM => ReadTexture<RGB565>(source, header),
                DXGIFormats.B5G5R5A1_UNORM => ReadTexture<ARGB1555>(source, header),
                DXGIFormats.B8G8R8A8_UNORM => ReadTexture<BGRA<byte>>(source, header),
                DXGIFormats.B8G8R8X8_UNORM => ReadTexture<BGRA<byte>>(source, header),
                DXGIFormats.R10G10B10_XR_BIAS_A2_UNORM => throw new NotImplementedException(),
                DXGIFormats.B8G8R8A8_TYPELESS => ReadTexture<BGRA<byte>>(source, header),
                DXGIFormats.B8G8R8A8_UNORM_SRGB => ReadTexture<BGRA<byte>>(source, header),
                DXGIFormats.B8G8R8X8_TYPELESS => ReadTexture<BGRA<byte>>(source, header),
                DXGIFormats.B8G8R8X8_UNORM_SRGB => ReadTexture<BGRA<byte>>(source, header),
                DXGIFormats.BC6H_TYPELESS => throw new NotImplementedException(),
                DXGIFormats.BC6H_UF16 => throw new NotImplementedException(),
                DXGIFormats.BC6H_SF16 => throw new NotImplementedException(),
                DXGIFormats.BC7_TYPELESS => throw new NotImplementedException(), //!!!
                DXGIFormats.BC7_UNORM => throw new NotImplementedException(),
                DXGIFormats.BC7_UNORM_SRGB => throw new NotImplementedException(),
                DXGIFormats.AYUV => ReadTexture<AYUV>(source, header),
                DXGIFormats.Y410 => ReadTexture<Y410>(source, header),
                DXGIFormats.Y416 => throw new NotImplementedException(),
                DXGIFormats.NV12 => throw new NotImplementedException(),
                DXGIFormats.P010 => throw new NotImplementedException(),
                DXGIFormats.P016 => throw new NotImplementedException(),
                DXGIFormats._420_OPAQUE => throw new NotImplementedException(),
                DXGIFormats.YUY2 => ReadTexture(source, header, new YUY2<YUV3>()),
                DXGIFormats.Y210 => ReadTexture(source, header, new Y210()),
                DXGIFormats.Y216 => throw new NotImplementedException(),
                DXGIFormats.NV11 => throw new NotImplementedException(),
                DXGIFormats.AI44 => throw new NotImplementedException(),
                DXGIFormats.IA44 => throw new NotImplementedException(),
                DXGIFormats.P8 => throw new NotImplementedException(),
                DXGIFormats.A8P8 => throw new NotImplementedException(),
                DXGIFormats.B4G4R4A4_UNORM => ReadTexture<RGBA16>(source, header),
                DXGIFormats.P208 => throw new NotImplementedException(),
                DXGIFormats.V208 => throw new NotImplementedException(),
                DXGIFormats.V408 => throw new NotImplementedException(),
                DXGIFormats.SAMPLER_FEEDBACK_MIN_MIP_OPAQUE => throw new NotImplementedException(),
                DXGIFormats.SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE => throw new NotImplementedException(),
                DXGIFormats.FORCE_UINT => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
        }

        private Texture<TColor> ReadTexture<TColor>(Stream source, Header header, IBlockProcessor<TColor>? block = null) where TColor : unmanaged, IColor<TColor>
        {
            int level = header.HasMipmaps() ? (int)header.MipMapCount : 1;
            if (header.Caps2.HasFlag(Header.Caps2Flag.Cubemap))
            {
                FlatTexture<TColor>? positiveX, negativeX, positiveY, negativeY, positiveZ, negativeZ;

                positiveX = header.Caps2.HasFlag(Header.Caps2Flag.CubemapPositiveX)
                    ? ImageStreamHelper.ReadTexture(source, (int)header.Width, (int)header.Height, level - 1, block)
                    : null;

                negativeX = header.Caps2.HasFlag(Header.Caps2Flag.CubemapNegativeX)
                    ? ImageStreamHelper.ReadTexture(source, (int)header.Width, (int)header.Height, level - 1, block)
                    : null;

                positiveY = header.Caps2.HasFlag(Header.Caps2Flag.CubemapPositiveY)
                    ? ImageStreamHelper.ReadTexture(source, (int)header.Width, (int)header.Height, level - 1, block)
                    : null;

                negativeY = header.Caps2.HasFlag(Header.Caps2Flag.CubemapNegativeY)
                    ? ImageStreamHelper.ReadTexture(source, (int)header.Width, (int)header.Height, level - 1, block)
                    : null;

                positiveZ = header.Caps2.HasFlag(Header.Caps2Flag.CubemapPositiveZ)
                    ? ImageStreamHelper.ReadTexture(source, (int)header.Width, (int)header.Height, level - 1, block)
                    : null;

                negativeZ = header.Caps2.HasFlag(Header.Caps2Flag.CubemapNegativeZ)
                    ? ImageStreamHelper.ReadTexture(source, (int)header.Width, (int)header.Height, level - 1, block)
                    : null;

                return new Cubemap<TColor>(positiveX, negativeX, positiveY, negativeY, positiveZ, negativeZ);
            }
            else if (header.Caps2.HasFlag(Header.Caps2Flag.Volume))
            {
                return new VolumeTexture<TColor>(ReadVolumeTexture(source, header, block));
            }

            return ImageStreamHelper.ReadTexture(source, (int)header.Width, (int)header.Height, level - 1, block);
        }

        private IEnumerable<FlatTexture<TColor>> ReadVolumeTexture<TColor>(Stream source, Header header, IBlockProcessor<TColor>? block) where TColor : unmanaged, IColor<TColor>
        {
            int width = (int)header.Width;
            int height = (int)header.Height;
            uint level = header.HasMipmaps() ? header.MipMapCount : 1;

            int depths = (int)header.Depth;
            var layer = new FlatTexture<TColor>[depths];
            for (int i = 0; i < level; i++)
            {
                for (int d = 0; d < depths; d++)
                {
                    IImage<TColor> image = ImageStreamHelper.ReadImage(source, width, height, block);
                    if (i == 0)
                        layer[d] = new FlatTexture<TColor>(image);
                    else
                        layer[d].Add(image);
                }
                width >>= 1;
                height >>= 1;
            }
            return layer;
        }
        #endregion

        #region WriteImage (Encode)
        /// <inheritdoc/>
        public void WriteImage<TColor>(IReadOnlyImage<TColor> source, Stream destination) where TColor : unmanaged, IColor<TColor>
        {
            FourCCType fourCCType = FourCCType.None;
            DXGIFormats dXGIFormats = DXGIFormats.None;
            PixelFormatInfo pixelInfo = default(TColor).FormatInfo;
            uint linearSize = 0;
            uint mipMapCount = 0;
            uint depth = 0;
            var texType = Header.Caps2Flag.None;

            IReadOnlyImage<TColor> baseImage = source;
            if (source is Texture<TColor> tex)
            {
                if (source is Cubemap<TColor> cube)
                {
                    texType = Header.Caps2Flag.Cubemap | Header.Caps2Flag.CubemapPositiveX | Header.Caps2Flag.CubemapNegativeX | Header.Caps2Flag.CubemapPositiveY | Header.Caps2Flag.CubemapNegativeY | Header.Caps2Flag.CubemapPositiveZ | Header.Caps2Flag.CubemapNegativeZ;
                }
                else if (source is VolumeTexture<TColor> vol)
                {
                    texType = Header.Caps2Flag.Volume;
                    depth = (uint)vol.Depths.Count;
                }
                mipMapCount = (uint)tex.MipMapCount;
                baseImage = tex.GetLevel(0);
            }

            if (baseImage is BlockImage<TColor> blockImage)
            {
                linearSize = (uint)(Math.Max(1, ((source.Width + 3) / 4)) * blockImage.BlockFormat.BytesPerBlock);
                if (blockImage.BlockFormat is BC1Block<RGBA<byte>> || blockImage.BlockFormat is BC1Block<BGRA<byte>> || blockImage.BlockFormat is BC1Block<ARGB<byte>> || blockImage.BlockFormat is BC1Block<ABGR<byte>>)
                {
                    fourCCType = FourCCType.DXT1;
                    dXGIFormats = DXGIFormats.BC1_UNORM;
                }
                else if (blockImage.BlockFormat is BC2Block<RGBA<byte>> || blockImage.BlockFormat is BC2Block<BGRA<byte>> || blockImage.BlockFormat is BC2Block<ARGB<byte>> || blockImage.BlockFormat is BC2Block<ABGR<byte>>)
                {
                    fourCCType = FourCCType.DXT3;
                    dXGIFormats = DXGIFormats.BC2_UNORM;
                }
                else if (blockImage.BlockFormat is BC3Block<RGBA<byte>> || blockImage.BlockFormat is BC3Block<BGRA<byte>> || blockImage.BlockFormat is BC3Block<ARGB<byte>> || blockImage.BlockFormat is BC3Block<ABGR<byte>>)
                {
                    fourCCType = FourCCType.DXT5;
                    dXGIFormats = DXGIFormats.BC3_UNORM;
                }
                else if (blockImage.BlockFormat is BC4SBlock<I<sbyte>>)
                {
                    fourCCType = FourCCType.BC4S;
                    dXGIFormats = DXGIFormats.BC4_SNORM;
                }
                else if (blockImage.BlockFormat is BC4UBlock<I<byte>>)
                {
                    fourCCType = FourCCType.BC4U;
                    dXGIFormats = DXGIFormats.BC4_UNORM;
                }
                else if (blockImage.BlockFormat is BC5UBlock)
                {
                    fourCCType = FourCCType.BC5U;
                    dXGIFormats = DXGIFormats.BC5_UNORM;
                }
                else if (blockImage.BlockFormat is BC5SBlock)
                {
                    fourCCType = FourCCType.BC5S;
                    dXGIFormats = DXGIFormats.BC5_SNORM;
                }
                else if (blockImage.BlockFormat is UYVY<YUV3>)
                {
                    fourCCType = FourCCType.UYVY;
                }
                else if (blockImage.BlockFormat is YUY2<YUV3>)
                {
                    fourCCType = FourCCType.YUY2;
                    dXGIFormats = DXGIFormats.YUY2;
                }
                else if (blockImage.BlockFormat is Y210)
                {
                    fourCCType = FourCCType.DX10;
                    dXGIFormats = DXGIFormats.Y210;
                }
            }

            if (fourCCType == FourCCType.None)
            {
                linearSize = (uint)((source.Width * pixelInfo.BitsPerPixel + 7) / 8);
                if (pixelInfo.Equals(RGBA<ushort>.FormatInfo))
                {
                    fourCCType = FourCCType.R16G16B16A16_UNORM;
                    dXGIFormats = DXGIFormats.R16G16B16A16_UNORM;
                }
                else if (pixelInfo.Equals(RGBA<short>.FormatInfo))
                {
                    fourCCType = FourCCType.R16G16B16A16_SNORM;
                    dXGIFormats = DXGIFormats.R16G16B16A16_SNORM;
                }
                else if (pixelInfo.Equals(RGBA<float>.FormatInfo))
                {
                    fourCCType = FourCCType.R32G32B32A32_FLOAT;
                    dXGIFormats = DXGIFormats.R32G32B32A32_FLOAT;
                }
                else if (pixelInfo.Equals(I<Half>.FormatInfo))
                {
                    fourCCType = FourCCType.R16_FLOAT;
                    dXGIFormats = DXGIFormats.R16_FLOAT;
                }
                else if (pixelInfo.Equals(IA<Half>.FormatInfo))
                {
                    fourCCType = FourCCType.R16G16_FLOAT;
                    dXGIFormats = DXGIFormats.R16G16_FLOAT;
                }
                else if (pixelInfo.Equals(I<float>.FormatInfo))
                {
                    fourCCType = FourCCType.R32_FLOAT;
                    dXGIFormats = DXGIFormats.R32_FLOAT;
                }
                else if (pixelInfo.Equals(IA<float>.FormatInfo))
                {
                    fourCCType = FourCCType.R32G32_FLOAT;
                    dXGIFormats = DXGIFormats.R32G32_FLOAT;
                }
                else if (pixelInfo.Equals(RGBA<Half>.FormatInfo))
                {
                    fourCCType = FourCCType.R16G16B16A16_FLOAT;
                    dXGIFormats = DXGIFormats.R16G16B16A16_FLOAT;
                }
                else if (pixelInfo.Type != PixelFormatInfo.ChannelType.Unsigned || pixelInfo.BitsPerPixel > 32 || pixelInfo.ColorSpace == PixelFormatInfo.ColorSpaceType.CMYK)
                {
                    using var clone = source.CloneAs<RGBA<byte>>();
                    WriteImage(clone, destination);
                    return;
                }
            }

            PixelFormat pixelFormat = fourCCType == FourCCType.None ? new PixelFormat(pixelInfo) : new PixelFormat(fourCCType);

            if (ForceDXT10Header)
                pixelFormat.FourCC = fourCCType = FourCCType.DX10;

            Header header = new Header((uint)source.Width, (uint)source.Height, pixelFormat, linearSize, mipMapCount, texType, depth);
            destination.Write(_Identifier);
            destination.Write(header);
            if (fourCCType == FourCCType.DX10)
            {
                TransparencyMode mode = source.Metadata?.SamplingInfos?.TransparencyMode ?? source.Apply(new TransparencyAnalyzer());

                var alpha = mode switch
                {
                    TransparencyMode.Opaque => DXT10Header.Misc2Flags.OPAQUE,
                    TransparencyMode.Straight => DXT10Header.Misc2Flags.STRAIGHT,
                    TransparencyMode.Cutout => DXT10Header.Misc2Flags.STRAIGHT,
                    TransparencyMode.Premultiplied => DXT10Header.Misc2Flags.PREMULTIPLIED,
                    _ => throw new NotImplementedException()
                };
                DXT10Header dXT10 = new DXT10Header(header, dXGIFormats, alpha);
                destination.Write(dXT10);
            }

 
            if (source is Cubemap<TColor> cubemap)
            {
                ImageStreamHelper.WriteTexture(destination, cubemap.PositiveX);
                ImageStreamHelper.WriteTexture(destination, cubemap.NegativeX);
                ImageStreamHelper.WriteTexture(destination, cubemap.PositiveY);
                ImageStreamHelper.WriteTexture(destination, cubemap.NegativeY);
                ImageStreamHelper.WriteTexture(destination, cubemap.PositiveZ);
                ImageStreamHelper.WriteTexture(destination, cubemap.NegativeZ);
            }
            else if (source is VolumeTexture<TColor> volume)
            {
                for (int i = 0; i < mipMapCount+1; i++)
                {
                    for (int d = 0; d < depth; d++)
                    {
                        var level = volume.Depths[d].Levels[i];
                        ImageStreamHelper.WriteTexture(destination, level);
                    }
                }
            }
            else
            {
                ImageStreamHelper.WriteTexture(destination, source);
            }
        }
        #endregion
    }
}
