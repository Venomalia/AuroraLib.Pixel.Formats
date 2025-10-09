using AuroraLib.Pixel.Metadata;
using System.Numerics;
using System.Runtime.InteropServices;

namespace AuroraLib.Pixel.Formats.Common.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct CIEXYZ
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public CIEXYZ(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public CIEXYZ(Vector2 xy)
        {
            float Y = 1f;
            float X = (xy.X / xy.Y) * Y;
            float Z = ((1f - xy.X - xy.Y) / xy.Y) * Y;

            this.X = (int)(X * 65536f);
            this.Y = (int)(Y * 65536f);
            this.Z = (int)(Z * 65536f);
        }

        public Vector2 ToXY()
        {
            const float FIXED = 1f / 65536f;

            float Xf = X * FIXED;
            float Yf = Y * FIXED;
            float Zf = Z * FIXED;

            float sum = Xf + Yf + Zf;
            if (sum == 0)
                return Vector2.Zero;

            return new Vector2(Xf / sum, Yf / sum);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct CIEXYZTRIPLE
    {
        public readonly CIEXYZ Red;
        public readonly CIEXYZ Green;
        public readonly CIEXYZ Blue;

        public CIEXYZTRIPLE(ColorSpace colorSpace)
        {
            Red = new CIEXYZ(colorSpace.Red);
            Green = new CIEXYZ(colorSpace.Green);
            Blue = new CIEXYZ(colorSpace.Blue);
        }
        public ColorSpace ToColorSpace()
        {
            Vector2 red = Red.ToXY();
            Vector2 green = Green.ToXY();
            Vector2 blue = Blue.ToXY();
            Vector2 white = ComputeWhitePoint().ToXY();

            return new ColorSpace(white, red, green, blue);
        }

        public CIEXYZ ComputeWhitePoint()
            => new CIEXYZ(
                Red.X + Green.X + Blue.X,
                Red.Y + Green.Y + Blue.Y,
                Red.Z + Green.Z + Blue.Z
            );
    }
}
