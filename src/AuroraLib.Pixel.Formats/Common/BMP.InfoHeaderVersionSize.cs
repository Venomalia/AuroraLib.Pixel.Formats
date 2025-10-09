namespace AuroraPixel.ImageFormats
{
    public sealed partial class BMP
    {
        public enum InfoHeaderVersionSize : uint
        {
            WinV2 = 12,
            IBMV2Short = 16,
            WinV3 = 40,
            AdobeV3 = 52,
            AdobeV3WithAlpha = 56,
            IBMV2 = 64,
            WinV4 = 108,
            WinV5 = 124
        }
    }
}
