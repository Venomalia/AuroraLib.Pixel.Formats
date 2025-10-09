namespace AuroraPixel.ImageFormats
{
    public sealed partial class BMP
    {
        private enum BmpCompression : int
        {
            /// <summary>
            /// No compression
            /// </summary>
            BI_RGB = default,

            /// <summary>
            /// Run-Length Encoding (RLE) compression for 8-bit images
            /// </summary>
            BI_RLE8 = 1,

            /// <summary>
            /// Run-Length Encoding (RLE) compression for 4-bit images
            /// </summary>
            BI_RLE4 = 2,

            /// <summary>
            /// Bit-fields compression
            /// </summary>
            BI_BITFIELDS = 3,

            /// <summary>
            /// JPEG compression (introduced in BMP v4, less commonly used)
            /// </summary>
            BI_JPEG = 4,

            /// <summary>
            /// PNG compression (introduced in BMP v4, less commonly used)
            /// </summary>
            BI_PNG = 5,

            /// <summary>
            /// only Windows CE 5.0 with .NET 4.0 or later 
            /// </summary>
            BI_ALPHABITFIELDS = 6,

            /// <summary>
            /// only Windows Metafile CMYK
            /// </summary>
            BI_CMYK = 11,

            /// <summary>
            /// RLE-8 only Windows Metafile CMYK
            /// </summary>
            BI_CMYKRLE8 = 12,

            /// <summary>
            /// RLE-4 only Windows Metafile CMYK
            /// </summary>
            BI_CMYKRLE4 = 13,

            /// <summary>
            /// Only OS/2.
            /// </summary>
            RLE24 = 100,

        }
    }
}
