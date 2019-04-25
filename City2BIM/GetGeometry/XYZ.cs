using System;

namespace City2BIM.GetGeometry
{

    public class XYZ
    {
        public double X, Y, Z;

        public XYZ(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public ReadGeomData ReadData
        {
            get => default(ReadGeomData);
            set
            {
            }
        }

        public Plane Plane
        {
            get => default(Plane);
            set
            {
            }
        }

        public Vertex Vertex
        {
            get => default(Vertex);
            set
            {
            }
        }

        public static XYZ operator +(XYZ a, XYZ b)
        {
            return new XYZ(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static XYZ operator -(XYZ a, XYZ b)
        {
            return new XYZ(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static XYZ operator /(XYZ a, double b)
        {
            return new XYZ(a.X / b, a.Y / b, a.Z / b);
        }

        public static XYZ operator *(XYZ a, double b)
        {
            return new XYZ(a.X * b, a.Y * b, a.Z * b);
        }

        public static double MagnitudeSq(XYZ a)
        {
            return a.X * a.X + a.Y * a.Y + a.Z * a.Z;
        }

        public static XYZ CrossProduct(XYZ a, XYZ b)
        {
            return new XYZ(a.Y * b.Z - a.Z * b.Y,
                           a.Z * b.X - a.X * b.Z,
                           a.X * b.Y - a.Y * b.X);
        }

        public static XYZ Normalized(XYZ a)
        {
            double sum = Math.Sqrt(MagnitudeSq(a));
            return a / sum;
        }

        public static double ScalarProduct(XYZ a, XYZ b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static double DistanceSq(XYZ a, XYZ b)
        {
            return MagnitudeSq(a - b);
        }

    }
}
