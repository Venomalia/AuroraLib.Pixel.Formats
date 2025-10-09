using System.Runtime.InteropServices;

namespace AuroraPixel.ImageFormats
{
    public sealed partial class BMP
    {
        /// <summary>
        /// Represents the file header of a BMP image, which contains general information about the file.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private readonly struct FileHeader
        {
            /// <summary>
            /// The file type signature. Typically "BM" for standard BMP files.
            /// </summary>
            public readonly Signatures Signature;

            /// <summary>
            /// The size of the entire BMP file in bytes.
            /// </summary>
            public readonly uint FileSize;

            /// <summary>
            /// Reserved; typically set to 0.
            /// </summary>
            public readonly ushort Reserved1;

            /// <summary>
            /// Reserved; typically set to 0.
            /// </summary>
            public readonly ushort Reserved2;

            /// <summary>
            /// The offset, in bytes, from the beginning of the file to the start of the bitmap data.
            /// </summary>
            public readonly uint Offset;

            public FileHeader(Signatures signature, uint fileSize, uint offsetData)
            {
                Signature = signature;
                FileSize = fileSize;
                Offset = offsetData;
                Reserved1 = Reserved2 = 0;
            }
        }
    }
}
