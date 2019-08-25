using System;

namespace City2BIM.GetGeometry
{

    public class C2BPoint
    {
        public double X, Y, Z;

        public C2BPoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        //public ReadGeometry ReadData
        //{
        //    get => default(ReadGeometry);
        //    set
        //    {
        //    }
        //}

        public C2BPlane Plane
        {
            get => default(C2BPlane);
            set
            {
            }
        }

        public C2BVertex Vertex
        {
            get => default(C2BVertex);
            set
            {
            }
        }

        public static C2BPoint operator +(C2BPoint a, C2BPoint b)
        {
            return new C2BPoint(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static C2BPoint operator -(C2BPoint a, C2BPoint b)
        {
            return new C2BPoint(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static C2BPoint operator /(C2BPoint a, double b)
        {
            return new C2BPoint(a.X / b, a.Y / b, a.Z / b);
        }

        public static C2BPoint operator *(C2BPoint a, double b)
        {
            return new C2BPoint(a.X * b, a.Y * b, a.Z * b);
        }

        public static double MagnitudeSq(C2BPoint a)
        {
            return a.X * a.X + a.Y * a.Y + a.Z * a.Z;
        }

        public static C2BPoint CrossProduct(C2BPoint a, C2BPoint b)
        {
            return new C2BPoint(a.Y * b.Z - a.Z * b.Y,
                           a.Z * b.X - a.X * b.Z,
                           a.X * b.Y - a.Y * b.X);
        }

        public static C2BPoint Normalized(C2BPoint a)
        {
            double sum = Math.Sqrt(MagnitudeSq(a));
            return a / sum;
        }

        public static double ScalarProduct(C2BPoint a, C2BPoint b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static double DistanceSq(C2BPoint a, C2BPoint b)
        {
            return MagnitudeSq(a - b);
        }

    }
}
