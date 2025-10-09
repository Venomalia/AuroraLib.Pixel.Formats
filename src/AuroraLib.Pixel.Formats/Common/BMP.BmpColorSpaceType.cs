namespace AuroraPixel.ImageFormats
{
    public sealed partial class BMP
    {
        /// <summary>
        /// Specifies the color space of the bitmap. Used in DIB v4 and v5 headers.
        /// </summary>
        private enum BmpColorSpaceType : uint
        {
            /// <summary>
            /// Calibrated RGB color space. Requires endpoints and gamma values.
            /// </summary>
            LCS_CalibratedRGB = 0x0,

            /// <summary>
            /// Standard RGB color space (sRGB). Most common value.
            /// </summary>
            LCS_sRGB = 0x73524742, // 'sRGB'

            /// <summary>
            /// Microsoft Windows default color space. Obsolete.
            /// </summary>
            LCS_WindowsColorSpace = 0x57696E20, // 'Win '

            /// <summary>
            /// Linked color profile is used. The path to the profile is specified in ProfileData.
            /// </summary>
            ProfileLinked = 0x4C494E4B, // 'LINK'

            /// <summary>
            /// Embedded color profile is used. The profile is embedded in the bitmap.
            /// </summary>
            ProfileEmbedded = 0x4D424544 // 'MBED'
        }
    }
}
