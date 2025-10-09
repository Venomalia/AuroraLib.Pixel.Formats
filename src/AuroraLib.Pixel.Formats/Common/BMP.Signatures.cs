namespace AuroraPixel.ImageFormats
{
    public sealed partial class BMP
    {
        /// <summary>
        /// Represents possible file signature identifiers (magic numbers) used in BMP and related formats.
        /// </summary>
        public enum Signatures : ushort
        {
            /// <summary>
            /// Standard Windows Bitmap file ("BM").
            /// </summary>
            Bitmap = 0x4D42,     // "BM"

            /// <summary>
            /// Bitmap array used by OS/2 ("BA").
            /// </summary>
            BitmapArray = 0x4142, // "BA"

            /// <summary>
            /// Color icon resource file used by OS/2 ("CI").
            /// </summary>
            ColorIcon = 0x4349,   // "CI"

            /// <summary>
            /// Icon image resource file used by OS/2 ("IC").
            /// </summary>
            Icon = 0x4943,        // "IC"

            /// <summary>
            /// Color pointer (cursor) resource file used by OS/2 ("CP").
            /// </summary>
            ColorPointer = 0x5043, // "CP"

            /// <summary>
            /// Pointer (cursor) image resource file used by OS/2 ("PT").
            /// </summary>
            Pointer = 0x5450      // "PT"
        }
    }
}
