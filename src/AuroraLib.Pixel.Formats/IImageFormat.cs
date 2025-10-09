using AuroraLib.Core.Format;

namespace AuroraLib.Pixel.Formats
{
    /// <summary>
    /// Represents an image format that supports encoding and decoding.
    /// </summary>
    public interface IImageFormat : IImageDecoder, IImageEncoder
    {
    }
}
