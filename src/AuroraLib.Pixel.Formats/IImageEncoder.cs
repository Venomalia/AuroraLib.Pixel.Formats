using AuroraLib.Pixel.Image;
using System.IO;

namespace AuroraLib.Pixel.Formats
{
    /// <summary>
    /// Provides functionality to encode image to a stream.
    /// </summary>
    public interface IImageEncoder
    {
        /// <summary>
        /// Encodes the specified <see cref="IReadOnlyImage{TColor}"/> and writes it to the stream.
        /// </summary>
        /// <typeparam name="TColor">The pixel color type.</typeparam>
        /// <param name="source">The image to encode.</param>
        /// <param name="destination">The stream to write the encoded image to.</param>
        void WriteImage<TColor>(IReadOnlyImage<TColor> source, Stream destination) where TColor : unmanaged, IColor<TColor>;
    }
}
