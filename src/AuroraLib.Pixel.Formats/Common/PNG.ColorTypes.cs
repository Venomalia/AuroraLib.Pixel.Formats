namespace AuroraLib.Pixel.Formats.Common
{
    public partial class PNG
    {
        private enum ColorTypes : byte
        {
            /// <summary>
            /// Each pixel is a grayscale sample.
            /// Allowed Bit Depths: 1,2,4,8,16
            /// </summary>
            Grayscale = 0,
            /// <summary>
            /// Each pixel is an R,G,B triple.
            /// Allowed Bit Depths: 8,16
            /// </summary>
            RGB = 2,
            /// <summary>
            /// Each pixel is a palette index;a PLTE chunk must appear.
            /// Allowed Bit Depths: 1,2,4,8
            /// </summary>
            UsePalette = 3,
            /// <summary>
            /// Each pixel is a grayscale sample, followed by an alpha sample.
            /// Allowed Bit Depths: 8,16
            /// </summary>
            GrayscaleAlpha = 4,
            /// <summary>
            /// Each pixel is an R,G,B triple, followed by an alpha sample.
            /// Allowed Bit Depths: 8,16
            /// </summary>
            RGBA = 6,
        }
    }
}
