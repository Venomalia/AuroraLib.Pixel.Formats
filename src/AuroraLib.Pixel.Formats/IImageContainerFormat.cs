using AuroraLib.Core.Format;
using AuroraLib.Pixel.Image;
using System.Collections.Generic;
using System.IO;

namespace AuroraLib.Pixel.Formats
{
    /// <summary>
    /// Represents an image container format.
    /// </summary>
    public interface IImageContainerFormat : IImageEncoder, IFormatInfoProvider
    {
        /// <summary>
        /// Reads images from the specified stream and adds them to the collection.
        /// </summary>
        /// <param name="source">The stream to read from.</param>
        /// <param name="images">The collection to add the images to.</param>
        void ReadImages(Stream source, ICollection<IImage> images);

        /// <summary>
        /// Writes the specified images to the destination stream.
        /// </summary>
        /// <param name="source">The images to write.</param>
        /// <param name="destination">The stream to write to.</param>
        void WriteImages(IEnumerable<IReadOnlyImage> source, Stream destination);
    }
}
