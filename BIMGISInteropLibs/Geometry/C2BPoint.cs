using System;

namespace BIMGISInteropLibs.Geometry
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

        public static bool CCW(C2BPoint p1, C2BPoint p2, C2BPoint p3, C2BPoint vecNormal)
        {
            var normal = new C2BPoint(0, 0, 0);
            normal += C2BPoint.CrossProduct(p1, p2);
            normal += C2BPoint.CrossProduct(p2, p3);
            normal += C2BPoint.CrossProduct(p3, p1);

            C2BPoint vecTri = C2BPoint.Normalized(normal);

            C2BPoint diffVec = vecTri - vecNormal;

            if (diffVec.X < 0.2 && diffVec.Y < 0.2 && diffVec.Z < 0.2)
            {
                return true;
            }
            return false;
        }

        public static double DistanceSq(C2BPoint a, C2BPoint b)
        {
            return MagnitudeSq(a - b);
        }
    }
}
