namespace AuroraLib.Pixel.Formats.Common
{
    public partial class PNG
    {
        private enum FilterTypes : byte
        {
            /// <summary>
            /// With the None filter, the scanline is transmitted unmodified; it is necessary only to insert a filter-type byte before the data. 
            /// </summary>
            None = 0,
            /// <summary>
            /// The Sub filter transmits the difference between each byte and the value of the corresponding byte of the prior pixel. 
            /// </summary>
            Sub = 1,
            /// <summary>
            /// The Up filter is just like the Sub filter except that the pixel immediately above the current pixel, rather than just to its left, is used as the predictor. 
            /// </summary>
            Up = 2,
            /// <summary>
            /// The Average filter uses the average of the two neighboring pixels (left and above) to predict the value of a pixel. 
            /// </summary>
            Average = 3,
            /// <summary>
            /// The Paeth filter computes a simple linear function of the three neighboring pixels (left, above, upper left), then chooses as predictor the neighboring pixel closest to the computed value.
            /// </summary>
            Paeth = 4,
        }
    }
}
