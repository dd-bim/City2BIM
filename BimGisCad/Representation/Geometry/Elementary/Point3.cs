using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BimGisCad.Representation.Geometry.Linear;
using static BimGisCad.Representation.Geometry.Elementary.Common;


namespace BimGisCad.Representation.Geometry.Elementary
{
    /// <summary>
    ///  3-Dimensionaler Punkt
    /// </summary>
    public struct Point3 : ILinear3//, IEquatable<Point3>
    {
        #region Fields

        /// <summary>
        ///  Punkt im Ursprung
        /// </summary>
        public static Point3 Zero => new Point3(0.0, 0.0, 0.0);

        #endregion Fields

        #region Constructors

        /// <summary>
        ///  Konstruktor mit Einzelwerten
        /// </summary>
        /// <param name="x"> X-Koordinate </param>
        /// <param name="y"> Y-Koordinate </param>
        /// <param name="z"> Z-Koordinate </param>
        private Point3(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        #endregion Constructors

        /// <summary>
        ///  X-Koordinate
        /// </summary>
        public readonly double X;

        /// <summary>
        ///  Y-Koordinate
        /// </summary>
        public readonly double Y;

        /// <summary>
        ///  Z-Koordinate
        /// </summary>
        public readonly double Z;

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public double[][] PointEqu => new double[][]
        {
                    new[]{1.0, 0.0, 0.0, this.X},
                    new[]{0.0, 1.0, 0.0, this.Y},
                    new[]{0.0, 0.0, 1.0, this.Z},
        };

        #endregion Properties

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Point3 Create(Vector3 vector) => new Point3(vector.X, vector.Y, vector.Z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Point3 Create(double x, double y, double z) => new Point3(x, y, z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public static Point3 Create(double xyz) => new Point3(xyz, xyz, xyz);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xyz"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static Point3 Create(IReadOnlyList<double> xyz, int startIndex = 0) => new Point3(xyz[startIndex], xyz[startIndex + 1], xyz[startIndex + 2]);

        /// <summary>
        /// Punkt als Schnittpunkt, wenn möglich
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="line"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool Create(Plane plane, Line3 line, out Point3 point)
        {
            double cos = Direction3.Dot(plane.Normal, line.Direction);
            if(Math.Abs(cos) < TRIGTOL)
            {
                point = default(Point3);
                return false;
            }
            double dist = Plane.DistanceToPlane(plane, line.Position) / cos;
            point = new Point3(
                line.Position.X - (dist * line.Direction.X),
                line.Position.Y - (dist * line.Direction.Y),
                line.Position.Z - (dist * line.Direction.Z));
            return true;
        }

        /// <summary>
        /// Punkt als Schnittpunkt, wenn möglich
        /// </summary>
        /// <param name="a">Ebene 1</param>
        /// <param name="b">Ebene 2</param>
        /// <param name="c">Ebene 3</param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool Create(Plane a, Plane b, Plane c, out Point3 point)
        {
            var ab = Direction3.Cross(a.Normal, b.Normal);
            if(Vector3.Norm2(ab) > TRIGTOL_SQUARED)
            {
                var bc = Direction3.Cross(b.Normal, c.Normal);
                if(Vector3.Norm2(bc) > TRIGTOL_SQUARED)
                {
                    var ca = Direction3.Cross(c.Normal, a.Normal);
                    if(Vector3.Norm2(ca) > TRIGTOL_SQUARED)
                    {
                        var v = (a.D * bc) + (b.D * ca) + (c.D * ab);
                        double rdet = -3.0 / (Direction3.Dot(ab, c.Normal) + Direction3.Dot(bc, a.Normal) + Direction3.Dot(ca, b.Normal));
                        point = new Point3(rdet * v.X, rdet * v.Y, rdet * v.Z);
                        return true;
                    }
                }
            }
            point = default(Point3);
            return false;
        }

        /// <summary>
        /// Punkt als Schnittpunkt, wenn möglich
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool Create(Line3 a, Line3 b, out Point3 point)
        {
            var n = Direction3.Cross(a.Direction, b.Direction);
            double sin2 = Vector3.Norm2(n);
            if(sin2 < TRIGTOL_SQUARED)
            // parallel
            {
                point = default(Point3);
                return false;
            }
            var diff = b.Position - a.Position;
            double dot = Vector3.Dot(diff, n);
            if(!IsNearlyZeroSquared(dot * dot / sin2))
            // windschief
            {
                point = default(Point3);
                return false;
            }
            var pa = a.Position + (Direction3.Det(diff, b.Direction, n) * a.Direction);
            var pb = b.Position + (Direction3.Det(diff, a.Direction, n) * b.Direction);
            point = new Point3((pa.X + pb.X) / 2.0, (pa.Y + pb.Y) / 2.0, (pa.Z + pb.Z) / 2.0);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="linears"></param>
        /// <param name="point"></param>
        /// <param name="vv"></param>
        /// <returns></returns>
        public static bool Create(IReadOnlyList<ILinear3> linears, out Point3 point, out double vv)
        {
            var slv = new Solve3();
            foreach(var lin in linears)
            {
                slv.AddRows(lin.PointEqu);
            }
            if(slv.Solve())
            {
                vv = slv.VV;
                point = new Point3(slv.X[0], slv.X[1], slv.X[2]);
                return true;
            }
            point = default(Point3);
            vv = double.NaN;
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strings"></param>
        /// <param name="point"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static bool Create(IList<string> strings, out Point3 point, int startIndex = 0)
        {
            double[] doubles;
            if(DoubleTryParse(strings, out doubles, startIndex, 3))
            {
                point = new Point3(doubles[0], doubles[1], doubles[2]);
                return true;
            }
            point = default(Point3);
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        public static explicit operator Vector3(Point3 point) => Vector3.Create(point.X, point.Y, point.Z);


        /// <summary>
        ///  Changes the Origin (-)
        /// </summary>
        public static Point3 SetOriginTo(Point3 point, Point3 origin) => Point3.Create(point.X - origin.X, point.Y - origin.Y, point.Z - origin.Z);

        /// <summary>
        ///  Changes the Origin (+)
        /// </summary>
        public static Point3 SetOriginFrom(Point3 point, Point3 origin) => Point3.Create(point.X + origin.X, point.Y + origin.Y, point.Z - origin.Z);


        /// <summary>
        ///  Differenzvektor zwischen zwei Punkten
        /// </summary>
        /// <param name="a"> 1. Punkt </param>
        /// <param name="b"> 2. Punkt </param>
        /// <returns> Differenzvektor </returns>
        public static Vector3 operator -(Point3 a, Point3 b) => Vector3.Create(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        /// <summary>
        ///  Translation eines Punktes (Subtraktion)
        /// </summary>
        /// <param name="a"> Punkt </param>
        /// <param name="b"> Translationsvektor </param>
        /// <returns> Translierter Punkt </returns>
        public static Point3 operator -(Point3 a, Vector3 b) => new Point3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        /// <summary>
        ///  Translation eines Punktes (Addition)
        /// </summary>
        /// <param name="a"> Punkt </param>
        /// <param name="b"> Translationsvektor </param>
        /// <returns> Translierter Punkt </returns>
        public static Point3 operator +(Point3 a, Vector3 b) => new Point3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        /// <summary>
        ///  Scale X and Y not Z
        /// </summary>
        public static Point3 ScaleXY(double scale, Point3 point) => new Point3(scale * point.X, scale * point.Y, point.Z);

        /// <summary>
        ///  Unscale X and Y not Z (Division)
        /// </summary>
        public static Point3 UnScaleXY(double scale, Point3 point) => new Point3(point.X / scale, point.Y / scale, point.Z);


        /// <summary>
        ///  Skalierung (Multiplikation)
        /// </summary>
        /// <param name="a"> Skalar </param>
        /// <param name="b"> Punkt </param>
        /// <returns> Skalierter Vektor </returns>
        public static Point3 operator *(double a, Point3 b) => new Point3(a * b.X, a * b.Y, a * b.Z);

        /// <summary>
        ///  Skalierung (Multiplikation)
        /// </summary>
        /// <param name="a"> Punkt </param>
        /// <param name="b"> Skalar </param>
        /// <returns> Skalierter Vektor </returns>
        public static Point3 operator *(Point3 a, double b) => new Point3(a.X * b, a.Y * b, a.Z * b);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Vector3 a, Point3 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Point3 a, Vector3 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Direction3 a, Point3 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Point3 a, Direction3 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Point3 a, Point3 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

        /// <summary>
        ///  Mittelpunkt zweier Punkte
        /// </summary>
        /// <param name="a"> 1. Punkt </param>
        /// <param name="b"> 2. Punkt </param>
        /// <returns> Mittelpunkt </returns>
        public static Point3 Centroid(Point3 a, Point3 b) => new Point3((a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0, (a.Z + b.Z) / 2.0);

        /// <summary>
        ///  Berechnet den Schwerpunkt
        /// </summary>
        /// <param name="a"> 1. Punkt </param>
        /// <param name="b"> 2. Punkt </param>
        /// <param name="c"> 3. Punkt </param>
        /// <returns> Schwerpunkt </returns>
        public static Point3 Centroid(Point3 a, Point3 b, Point3 c) => new Point3((a.X + b.X + c.X) / 3.0, (a.Y + b.Y) + c.Y / 3.0, (a.Z + b.Z + c.Z) / 3.0);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double DistanceSq(Point3 a, Point3 b) => Vector3.Norm2(b - a);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Distance(Point3 a, Point3 b) => Vector3.Norm(b - a);
        
        /// <summary>
        ///  Berechnet den Schwerpunkt einer Punktliste, bei Bedarf die Vektoren vom Schwerpunkt zu
        ///  den Punkten
        /// </summary>
        /// <param name="points">      Punktliste </param>
        /// <returns> Schwerpunkt </returns>
        public static Point3 Centroid(IList<Point3> points)
        {
            double x = 0.0, y = 0.0, z = 0.0;
            foreach(var point in points)
            {
                x += point.X;
                y += point.Y;
                z += point.Z;
            }
            x /= points.Count;
            y /= points.Count;
            z /= points.Count;
            return new Point3(x, y, z);
        }

        /// <summary>
        ///  Berechnet den Schwerpunkt einer Punktliste, bei Bedarf die Vektoren vom Schwerpunkt zu
        ///  den Punkten
        /// </summary>
        /// <param name="points">      Punktliste </param>
        /// <param name="vectors">     Vektoren </param>
        /// <returns> Schwerpunkt </returns>
        public static Point3 Centroid(IList<Point3> points,out IList<Vector3> vectors)
        {
            double x = 0.0, y = 0.0, z = 0.0;
            foreach(var point in points)
            {
                x += point.X;
                y += point.Y;
                z += point.Z;
            }
            x /= points.Count;
            y /= points.Count;
            z /= points.Count;
            vectors = points.Select(p => Vector3.Create(p.X - x, p.Y - y, p.Z - z)).ToList();
            return new Point3(x, y, z);
        }

        /// <summary>
        /// Geometrischer Vergleich
        /// </summary>
        /// <param name="other"></param>
        /// <param name="minDist">kleinstmöglicher Abstand</param>
        /// <returns></returns>
        public bool Coincident(Point3 other, double minDist = MINDIST)
        {
            double dx = other.X - this.X;
            double dy = other.Y - this.Y;
            double dz = other.Z - this.Z;
            return IsNearlyZeroSquared((dx * dx) + (dy * dy) + (dz * dz), minDist * minDist);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static string ToCSVString(Point3 point) => string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", point.X, point.Y, point.Z);

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0:F3} {1:F3} {2:F3}", X, Y, Z);


        #endregion Methods
    }
}