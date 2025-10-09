using AuroraLib.Core.Format;
using AuroraLib.Pixel.Image;
using System.IO;

namespace AuroraLib.Pixel.Formats
{
    /// <summary>
    /// Defines a decoder capable of reading an <see cref="IImage"/> from a stream.
    /// </summary>
    public interface IImageDecoder : IFormatInfoProvider
    {
        /// <summary>
        /// Reads and decodes an image from the specified stream.
        /// </summary>
        /// <param name="source">The stream containing the encoded image.</param>
        /// <returns>The decoded <see cref="IImage"/>.</returns>
        IImage ReadImage(Stream source);
    }
}
