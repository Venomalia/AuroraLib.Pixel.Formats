using AuroraLib.Pixel.Image;
using AuroraLib.Pixel.Processing.Processor;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace AuroraLib.Pixel.Formats
{
    /// <summary>
    /// Provides extension methods for image format operations.
    /// </summary>
    public static class ImageFormatExtension
    {
        /// <summary>
        /// Reads and decodes an image from the specified file path.
        /// </summary>
        /// <param name="decoder">The image decoder.</param>
        /// <param name="path">The path to the image file.</param>
        /// <returns>The decoded <see cref="IImage"/>.</returns>
        public static IImage ReadImage(this IImageDecoder decoder, string path)
        {
            using FileStream source = new FileStream(path, FileMode.Open, FileAccess.Read);
            return decoder.ReadImage(source);
        }

        /// <summary>
        /// Reads and decodes images from the specified file path.
        /// </summary>
        /// <param name="decoder">The image container decoder.</param>
        /// <param name="path">The path to the image file.</param>
        /// <returns>The decoded images.</returns>
        public static List<IImage> ReadImages(this IImageContainerFormat decoder, string path)
        {
            using FileStream source = new FileStream(path, FileMode.Open, FileAccess.Read);
            return decoder.ReadImages(source);
        }

        /// <summary>
        /// Reads and decodes images from the specified stream.
        /// </summary>
        /// <param name="decoder">The image container decoder.</param>
        /// <param name="source">The stream to read from.</param>
        /// <returns>The decoded images.</returns>
        public static List<IImage> ReadImages(this IImageContainerFormat decoder, Stream source)
        {
            List<IImage> images = new List<IImage>();
            decoder.ReadImages(source, images);
            return images;
        }

        /// <summary>
        /// Encodes the specified image and writes it to a file.
        /// </summary>
        /// <param name="encoder">The image encoder.</param>
        /// <param name="source">The image to encode.</param>
        /// <param name="path">The destination file path.</param>
        public static void WriteImage(this IImageEncoder encoder, IReadOnlyImage source, string path)
        {
            using FileStream destination = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
            WriteImage(encoder, source, destination);
        }

        /// <summary>
        /// Encodes the specified image and writes it to a stream.
        /// </summary>
        /// <param name="encoder">The image encoder.</param>
        /// <param name="source">The image to encode.</param>
        /// <param name="destination">The destination stream.</param>
        public static void WriteImage(this IImageEncoder encoder, IReadOnlyImage source, Stream destination)
            => source.Apply(new WriteImageProcessor(encoder, destination), default);

        /// <summary>
        /// Encodes and writes images to the specified file path.
        /// </summary>
        /// <param name="encoder">The image container encoder.</param>
        /// <param name="source">The images to write.</param>
        /// <param name="path">The path to the image file.</param>
        public static void WriteImages(this IImageContainerFormat encoder, IEnumerable<IReadOnlyImage> source, string path)
        {
            using FileStream destination = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
            encoder.WriteImages(source, destination);
        }

        private sealed class WriteImageProcessor : IReadOnlyPixelProcessor
        {
            private readonly IImageEncoder _encoder;
            private readonly Stream _destination;

            public WriteImageProcessor(IImageEncoder encoder, Stream destination)
            {
                _encoder = encoder;
                _destination = destination;
            }

            public void Apply<TColor>(IReadOnlyImage<TColor> image) where TColor : unmanaged, IColor<TColor>
                => _encoder.WriteImage(image, _destination);

            public void Apply<TColor>(IReadOnlyImage<TColor> image, Rectangle region) where TColor : unmanaged, IColor<TColor>
                => _encoder.WriteImage(image, _destination);
        }
    }
}
