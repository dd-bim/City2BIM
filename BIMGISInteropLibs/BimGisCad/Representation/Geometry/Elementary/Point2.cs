using System.Collections.Generic;
using System.Linq;
using BimGisCad.Representation.Geometry.Linear;
using static BimGisCad.Representation.Geometry.Elementary.Common;
using System;
using System.Globalization;

namespace BimGisCad.Representation.Geometry.Elementary
{

    /// <summary>
    ///  2-Dimensionaler Punkt
    /// </summary>
    public struct Point2 : ILinear2//, IEquatable<Point2>
    {
        #region Fields

        /// <summary>
        ///  Punkt im Ursprung
        /// </summary>
        public static Point2 Zero => new Point2(0.0, 0.0);

        #endregion Fields

        #region Constructors

        /// <summary>
        ///  Konstruktor mit Einzelwerten
        /// </summary>
        /// <param name="x"> X-Koordinate </param>
        /// <param name="y"> Y-Koordinate </param>
        private Point2(double x, double y)
        {
            this.X = x;
            this.Y = y;
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

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public double[][] PointEqu => new []
        {
            new[]{1.0, 0.0, this.X},
            new[]{0.0, 1.0, this.Y},
        };

        #endregion Properties

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Point2 Create(Vector2 vector) => new Point2(vector.X, vector.Y);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Point2 Create(double x, double y) => new Point2(x, y);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public static Point2 Create(double xy) => new Point2(xy, xy);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static Point2 Create(IReadOnlyList<double> xy, int startIndex = 0) => new Point2(xy[startIndex], xy[startIndex + 1]);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="linears"></param>
        /// <param name="point"></param>
        /// <param name="vv"></param>
        /// <returns></returns>
        public static bool Create(IReadOnlyList<ILinear2> linears, out Point2 point, out double vv)
        {
            var slv = new Solve2();
            foreach(var lin in linears)
            {
                slv.AddRows(lin.PointEqu);
            }
            if(slv.Solve())
            {
                vv = slv.VV;
                point = new Point2(slv.X[0], slv.X[1]);
                return true;
            }
            point = default(Point2);
            vv = double.NaN;
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool Create(Line2 a, Line2 b, out Point2 point)
        {
            double det = Direction2.Det(a.Direction, b.Direction);
            var diff = a.Position - b.Position;
            if(det > TRIGTOL || det < -TRIGTOL)
            {
                var pa = a.Position + (Direction2.Det(b.Direction, diff) / det * a.Direction);
                var pb = b.Position + (Direction2.Det(a.Direction, diff) / det * b.Direction);
                point = new Point2((pa.X + pb.X) / 2.0, (pa.Y + pb.Y) / 2.0);
                return true;
            }
            else
            {
                point = default(Point2);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strings"></param>
        /// <param name="point"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static bool Create(IList<string> strings, out Point2 point, int startIndex = 0)
        {
            double[] doubles;
            if(DoubleTryParse(strings, out doubles, startIndex, 2))
            {
                point = new Point2(doubles[0], doubles[1]);
                return true;
            }
            point = default(Point2);
            return false;
        }

        /// <summary>
        ///  Changes the Origin (-)
        /// </summary>
        public static Point2 SetOriginTo(Point2 point, Point2 origin) => Point2.Create(point.X - origin.X, point.Y - origin.Y);

        /// <summary>
        ///  Changes the Origin (+)
        /// </summary>
        public static Point2 SetOriginFrom(Point2 point, Point2 origin) => Point2.Create(point.X + origin.X, point.Y + origin.Y);

        /// <summary>
        ///  Differenzvektor zwischen zwei Punkten
        /// </summary>
        /// <param name="a"> 1. Punkt </param>
        /// <param name="b"> 2. Punkt </param>
        /// <returns> Differenzvektor </returns>
        public static Vector2 operator -(Point2 a, Point2 b) => Vector2.Create(a.X - b.X, a.Y - b.Y);

        /// <summary>
        ///  Translation eines Punktes (Subtraktion)
        /// </summary>
        /// <param name="a"> Punkt </param>
        /// <param name="b"> Translationsvektor </param>
        /// <returns> Translierter Punkt </returns>
        public static Point2 operator -(Point2 a, Vector2 b) => new Point2(a.X - b.X, a.Y - b.Y);

        /// <summary>
        ///  Translation eines Punktes (Addition)
        /// </summary>
        /// <param name="a"> Punkt </param>
        /// <param name="b"> Translationsvektor </param>
        /// <returns> Translierter Punkt </returns>
        public static Point2 operator +(Point2 a, Vector2 b) => new Point2(a.X + b.X, a.Y + b.Y);

        /// <summary>
        ///  Translation eines Punktes (Addition)
        /// </summary>
        /// <param name="a"> Translationsvektor </param>
        /// <param name="b"> Punkt </param>
        /// <returns> Translierter Punkt </returns>
        public static Point2 operator +(Vector2 a, Point2 b) => new Point2(a.X + b.X, a.Y + b.Y);

        /// <summary>
        ///  Skalierung (Multiplikation)
        /// </summary>
        /// <param name="a"> Skalar </param>
        /// <param name="b"> Punkt </param>
        /// <returns> Skalierter Vektor </returns>
        public static Point2 operator *(double a, Point2 b) => new Point2(a * b.X, a * b.Y);

        /// <summary>
        ///  Skalierung (Multiplikation)
        /// </summary>
        /// <param name="a"> Punkt </param>
        /// <param name="b"> Skalar </param>
        /// <returns> Skalierter Vektor </returns>
        public static Point2 operator *(Point2 a, double b) => new Point2(a.X * b, a.Y * b);
 
        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Point2 a, Vector2 b) => (a.X * b.X) + (a.Y * b.Y);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Vector2 a, Point2 b) => (a.X * b.X) + (a.Y * b.Y);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Point2 a, Direction2 b) => (a.X * b.X) + (a.Y * b.Y);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Direction2 a, Point2 b) => (a.X * b.X) + (a.Y * b.Y);

        /// <summary>
        ///  Determinante einer 2x2 Matrix aus zwei Vektoren
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Determinante </returns>
        public static double Det(Point2 a, Vector2 b) => (a.X * b.Y) - (a.Y * b.X);
 
        /// <summary>
        ///  Determinante einer 2x2 Matrix aus zwei Vektoren
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Determinante </returns>
        public static double Det(Direction2 a, Point2 b) => (a.X * b.Y) - (a.Y * b.X);

        /// <summary>
        ///  Determinante einer 2x2 Matrix aus zwei Vektoren
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Determinante </returns>
        public static double Det(Point2 a, Direction2 b) => (a.X * b.Y) - (a.Y * b.X);

        /// <summary>
        ///  Determinante einer 2x2 Matrix aus zwei Vektoren
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Determinante </returns>
        public static double Det(Vector2 a, Point2 b) => (a.X * b.Y) - (a.Y * b.X);

        /// <summary>
        ///  Determinante einer 2x2 Matrix aus zwei Punkten
        /// </summary>
        /// <param name="a"> 1. Punkt </param>
        /// <param name="b"> 2. Punkt </param>
        /// <returns> Determinante </returns>
        public static double Det(Point2 a, Point2 b) => (a.X * b.Y) - (a.Y * b.X);

        /// <summary>
        ///  Determinante einer 2x2 Matrix aus zwei Vektoren, b-a und c-a
        /// </summary>
        /// <param name="a"> 1. Punkt </param>
        /// <param name="b"> 2. Punkt </param>
        /// <param name="c"> 3. Punkt </param>
        /// <returns> Determinante </returns>
        public static double Det(Point2 a, Point2 b, Point2 c)
        {
            return (((b.X - a.X) * (c.Y - a.Y))
                    - ((c.X - a.X) * (b.Y - a.Y)));
        }

        /// <summary>
        ///  Mittelpunkt zweier Punkte
        /// </summary>
        /// <param name="a"> 1. Punkt </param>
        /// <param name="b"> 2. Punkt </param>
        /// <returns> Mittelpunkt </returns>
        public static Point2 Centroid(Point2 a, Point2 b) => new Point2((a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0);

        /// <summary>
        ///  Berechnet den Schwerpunkt einer Punktliste, bei Bedarf die Vektoren vom Schwerpunkt zu
        ///  den Punkten
        /// </summary>
        /// <param name="points">      Punktliste </param>
        /// <returns> Schwerpunkt </returns>
        public static Point2 Centroid(IList<Point2> points)
        {
            double x = 0.0, y = 0.0;
            foreach(var point in points)
            {
                x += point.X;
                y += point.Y;
            }
            x /= points.Count;
            y /= points.Count;
            return new Point2(x, y);
        }

        /// <summary>
        ///  Berechnet den Schwerpunkt einer Punktliste, bei Bedarf die Vektoren vom Schwerpunkt zu
        ///  den Punkten
        /// </summary>
        /// <param name="points">      Punktliste </param>
        /// <param name="vectors">     Vektoren </param>
        /// <returns> Schwerpunkt </returns>
        public static Point2 Centroid(IList<Point2> points, out IList<Vector2> vectors)
        {
            double x = 0.0, y = 0.0;
            foreach(var point in points)
            {
                x += point.X;
                y += point.Y;
            }
            x /= points.Count;
            y /= points.Count;
            vectors = points.Select(p => Vector2.Create(p.X - x, p.Y - y)).ToList();
            return new Point2(x, y);
        }

        /// <summary>
        /// Abstand eines Punktes (- rechts)
        /// </summary>
        /// <param name="lineStart"></param>
        /// <param name="lineEnd"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static double SignedDistance(Point2 lineStart, Point2 lineEnd, Point2 point)
        {
            var l = lineEnd - lineStart;
            return Vector2.Det(l, point - lineStart) / Vector2.Norm2(l);
        }


        ///// <summary>
        ///// Berechnet auf welcher Seite eines verankerten Vektors ein Punkt p liegt (Pos = links, Neg = rechts des Vektors)
        ///// </summary>
        ///// <param name="p0">Vektoranker</param>
        ///// <param name="v">Vektor</param>
        ///// <param name="p">Testpunkt</param>
        ///// <returns></returns>
        //public static Side SideOf(Point2 p0, Vector2 v, Point2 p)
        //{
        //    double det = (v.X * (p.Y - p0.Y)) - ((p.X - p0.X) * v.Y);
        //    return det < -EPS ? Side.Neg : det > EPS ? Side.Pos : Side.On;
        //}

        ///// <summary>
        ///// Berechnet auf welcher Seite einer Linie (vin l1 nach l2) ein Punkt p liegt (Pos = links, Neg = rechts der Linie)
        ///// </summary>
        ///// <param name="lp1">Linienpunkt 1</param>
        ///// <param name="lp2">Linienpunkt 2</param>
        ///// <param name="p">Testpunkt</param>
        ///// <returns></returns>
        //public static Side SideOf(Point2 lp1, Point2 lp2, Point2 p)
        //{
        //    double det = ((lp2.X - lp1.X) * (p.Y - lp1.Y)) - ((p.X - lp1.X) * (lp2.Y - lp1.Y));
        //    return det < -EPS ? Side.Neg : det > EPS ? Side.Pos : Side.On;
        //}

        /// <summary>
        /// Geometrischer Vergleich
        /// </summary>
        /// <param name="other"></param>
        /// <param name="minDist">kleinstmöglicher Abstand</param>
        /// <returns></returns>
        public bool Coincident(Point2 other, double minDist = MINDIST)
        {
            double dx = other.X - this.X;
            double dy = other.Y - this.Y;
            return IsNearlyZeroSquared((dx * dx) + (dy * dy), minDist*minDist);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static string ToCSVString(Point2 point) => string.Format(CultureInfo.InvariantCulture, "{0},{1}", point.X, point.Y);

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0:F3} {1:F3}", X, Y);

        #endregion Methods
    }
}