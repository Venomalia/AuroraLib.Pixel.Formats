namespace AuroraLib.Pixel.Formats.Common
{
    public partial class PNG
    {
        enum ChunkTypes : uint
        {
            /// <summary>
            /// Image Header
            /// </summary>
            Header = 0x52444849, // IHDR
            /// <summary>
            /// The PLTE chunk contains from 1 to 256 palette entries, each a three-byte RGB vaule.
            /// </summary>
            Palette = 0x45544C50, // PLTE
            /// <summary>
            /// The IDAT chunk contains the actual image data. To create this data
            /// </summary>
            Data = 0x54414449, // IDAT
            /// <summary>
            /// It marks the end of the PNG datastream, must appear LAST.
            /// </summary>
            End = 0x444E4549, // IEND
            /// <summary>
            /// The tRNS chunk specifies that the image uses simple transparency: either alpha values associated with palette entries (for indexed-color images) or a single transparent color (for grayscale and truecolor images).
            /// </summary>
            Transparency = 0x534E5274, //tRNS
            /// <summary>
            /// The gAMA chunk specifies the relationship between the image samples and the desired display output intensity.
            /// </summary>
            Gamma = 0x414d4167, // gAMA
            /// <summary>
            /// Applications that need device-independent specification of colors in a PNG file can use the cHRM chunk to specify the 1931 CIE x,y chromaticities of the red, green, and blue primaries used in the image, and the referenced white point.
            /// </summary>
            PrimaryChromaticities = 0x4D524863, // cHRM 
            /// <summary>
            /// If the sRGB chunk is present, the image samples conform to the sRGB color space [sRGB], and should be displayed using the specified rendering intent as defined by the International Color Consortium
            /// </summary>
            StandardRGB = 0x42475273, // sRGB
            /// <summary>
            /// If the iCCP chunk is present, the image samples conform to the color space represented by the embedded ICC profile as defined by the International Color Consortium.
            /// </summary>
            ICC_Profile = 0x50534369, // iCCP
            /// <summary>
            /// If the eXIf chunk is present, the image contains Exif metadata (camera information, orientation, GPS, etc.) as defined by the Exchangeable Image File Format specification.
            /// </summary>
            EXIF_Metadata = 0x66495865,
            /// <summary>
            /// Textual information that the encoder wishes to record with the image can be stored in tEXt chunks.
            /// </summary>
            Text = 0x74584574, //tEXt
            /// <summary>
            /// The zTXt chunk contains textual data, just as tEXt does; however, zTXt takes advantage of compression.
            /// </summary>
            CompressedText = 0x7478547A,// zTXt
            /// <summary>
            /// This chunk is semantically equivalent to the tEXt and zTXt chunks, but the textual data is in the UTF-8 encoding of the Unicode character set instead of Latin-1.
            /// </summary>
            InternationalText = 0x74785469, // iTXt
            /// <summary>
            /// The bKGD chunk specifies a default background color to present the image against.
            /// </summary>
            BackgroundColor = 0x44474B62, //bKGD
            /// <summary>
            /// The pHYs chunk specifies the intended pixel size or aspect ratio for display of the image.
            /// </summary>
            Physical = 0x73594870,//pHYs
            /// <summary>
            /// To simplify decoders, PNG specifies that only certain sample depths can be used, and further specifies that sample values should be scaled to the full range of possible values at the sample depth.
            /// </summary>
            SignificantBits = 0x54594273,// sBIT
            /// <summary>
            /// This chunk can be used to suggest a reduced palette to be used when the display device is not capable of displaying the full range of colors present in the image
            /// </summary>
            SuggestedPalette = 0x544C5073, // sPLT
            /// <summary>
            /// The hIST chunk gives the approximate usage frequency of each color in the color palette. A hIST chunk can appear only when a PLTE chunk appears.
            /// </summary>
            PaletteHistogram = 0x54534968, // hIST 
            /// <summary>
            /// The tIME chunk gives the time of the last image modification (not the time of initial image creation).
            /// </summary>
            LastModification = 0x454D4974, //tIME
        }
    }
}
