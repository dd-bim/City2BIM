using System;
using System.Collections.Generic;
using static BimGisCad.Representation.Geometry.Elementary.Common;

namespace BimGisCad.Representation.Geometry.Elementary
{
    /// <summary>
    ///  2-Dimensionaler Vektor
    /// </summary>
    public struct Vector2
    {
        #region Fields

        /// <summary>
        ///  Vektor mit Norm 0
        /// </summary>
        public static Vector2 Zero => new Vector2(0.0, 0.0);

        /// <summary>
        /// Richtung der X-Achse
        /// </summary>
        public static Vector2 UnitX => new Vector2(1.0, 0.0);

        /// <summary>
        /// Richtung der Y-Achse
        /// </summary>
        public static Vector2 UnitY => new Vector2(0.0, 1.0);

        /// <summary>
        /// Ungültiger Vektor
        /// </summary>
        public static Vector2 NaN => new Vector2(double.NaN, double.NaN);

        /// <summary>
        /// 
        /// </summary>
        public static Vector2 PositiveInfinity => new Vector2(double.PositiveInfinity, double.PositiveInfinity);

        /// <summary>
        /// 
        /// </summary>
        public static Vector2 NegativeInfinity => new Vector2(double.NegativeInfinity, double.PositiveInfinity);

        #endregion Fields

        #region Constructors

        /// <summary>
        ///  Konstruktor mit Einzelwerten
        /// </summary>
        /// <param name="x"> X-Koordinate </param>
        /// <param name="y"> Y-Koordinate </param>
        private Vector2(double x, double y)
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

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector2 Create(Vector2 vector) => new Vector2(vector.X, vector.Y);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector2 Create(Vector3 vector) => new Vector2(vector.X, vector.Y);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector2 Create(Direction2 vector) => new Vector2(vector.X, vector.Y);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Vector2 Create(double x, double y) => new Vector2(x, y);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xy"></param>
        /// <returns></returns>
        public static Vector2 Create(double xy) => new Vector2(xy, xy);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static Vector2 Create(IReadOnlyList<double> xy, int startIndex = 0) => new Vector2(xy[startIndex], xy[startIndex + 1]);

        /// <summary>
        /// Swaps X and Y
        /// </summary>
        public static Vector2 Swap(Vector2 vector) => new Vector2(vector.Y, vector.X);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static double Norm(Vector2 vector) => Common.Norm(vector.X, vector.Y);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static double Norm2(Vector2 vector) => (vector.X * vector.X) + (vector.Y * vector.Y);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static bool IsNaN(Vector2 vector) => double.IsNaN(vector.X) || double.IsNaN(vector.Y);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="norm"></param>
        /// <returns></returns>
        public static bool TryGetNorm(Vector2 vector, out double norm)
        {
            norm = Norm(vector);
            return norm > EPS;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static bool Normalized(Vector2 vector, out Vector2 unit)
        {
            double norm;
            if(TryGetNorm(vector, out norm))
            {
                unit = vector / norm;
                return true;
            }
            else
            {
                unit = default(Vector2);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="norm"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static bool Normalized(Vector2 vector, double norm, out Vector2 unit)
        {
            if(IsValidNorm(norm))
            {
                unit = vector / norm;
                return true;
            }
            else
            {
                unit = default(Vector2);
                return false;
            }
        }

        /// <summary>
        ///  Negation
        /// </summary>
        /// <param name="vector"> Zu negierender Vektor </param>
        /// <returns> Negierter Vektor </returns>
        public static Vector2 operator -(Vector2 vector) => new Vector2(-vector.X, -vector.Y);

        /// <summary>
        ///  Vektor Subtraktion
        /// </summary>
        /// <param name="a"> Minuend </param>
        /// <param name="b"> Subtrahend </param>
        /// <returns> Differenz </returns>
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);


        /// <summary>
        ///  Vektor Addition
        /// </summary>
        /// <param name="a"> 1. Summand </param>
        /// <param name="b"> 2. Summand </param>
        /// <returns> Summe </returns>
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);

        /// <summary>
        ///  Vektor Skalierung (Multiplikation)
        /// </summary>
        /// <param name="a"> Skalar </param>
        /// <param name="b"> Vektor </param>
        /// <returns> Skalierter Vektor </returns>
        public static Vector2 operator *(double a, Vector2 b) => new Vector2(a * b.X, a * b.Y);

        /// <summary>
        ///  Vektor Skalierung (Multiplikation)
        /// </summary>
        /// <param name="a"> Vektor </param>
        /// <param name="b"> Skalar </param>
        /// <returns> Skalierter Vektor </returns>
        public static Vector2 operator *(Vector2 a, double b) => new Vector2(a.X * b, a.Y * b);

        /// <summary>
        ///  Vektor Skalierung (Division)
        /// </summary>
        /// <param name="a"> Vektor </param>
        /// <param name="b"> Skalar (Divisor) </param>
        /// <returns> Skalierter Vektor </returns>
        public static Vector2 operator /(Vector2 a, double b) => new Vector2(a.X / b, a.Y / b);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Vector2 a, Vector2 b) => (a.X * b.X) + (a.Y * b.Y);

        /// <summary>
        ///  Determinante einer 2x2 Matrix aus zwei Vektoren
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Determinante </returns>
        public static double Det(Vector2 a, Vector2 b) => (a.X * b.Y) - (a.Y * b.X);
        //       public static double Det(Vector2 a, Vector2 b) => (a.X + b.X) * (b.Y - a.Y); // effizienter

        /// <summary>
        /// Reflects a Vector with a Direction
        /// </summary>
        /// <param name="toReflect"></param>
        /// <param name="reflector"></param>
        /// <returns></returns>
        public static Vector2 ReflectWith(Vector2 toReflect, Direction2 reflector)
        {
            var scale = 2.0 * Direction2.Dot(toReflect, reflector);
            return new Vector2((scale * reflector.X) - toReflect.X, (scale * reflector.Y) - toReflect.Y);
        }

        /// <summary>
        /// Reflects a Vector with Unit X
        /// </summary>
        /// <param name="toReflect"></param>
        /// <returns></returns>
        public static Vector2 ReflectWithUnitX(Vector2 toReflect) => new Vector2(toReflect.X, -toReflect.Y);

        /// <summary>
        /// Rotate by 90° (Perpendicular) counterclockwise or if needed clockwise
        /// </summary>
        /// <param name="v"></param>
        /// <param name="clockWise"></param>
        /// <returns></returns>
        public static Vector2 Perp(Vector2 v, bool clockWise = false) =>
            clockWise ? new Vector2(v.Y, -v.X) : new Vector2(-v.Y, v.X);

        /// <summary>
        /// Reflects a Vector with Unit Y
        /// </summary>
        /// <param name="toReflect"></param>
        /// <returns></returns>
        public static Vector2 ReflectWithUnitY(Vector2 toReflect) => new Vector2(-toReflect.X, toReflect.Y);

        /// <summary>
        /// Geometrischer Vergleich
        /// </summary>
        /// <param name="other"></param>
        /// <param name="minDist">kleinstmöglicher Abstand</param>
        /// <returns></returns>
        public bool Coincident(Vector2 other, double minDist = MINDIST)
        {
            double dx = other.X - this.X;
            double dy = other.Y - this.Y;
            return IsNearlyZeroSquared((dx * dx) + (dy * dy), minDist * minDist);
        }

        #endregion Methods
    }
}