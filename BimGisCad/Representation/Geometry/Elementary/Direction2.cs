using System;
using System.Collections.Generic;
using static BimGisCad.Representation.Geometry.Elementary.Common;

namespace BimGisCad.Representation.Geometry.Elementary
{
    /// <summary>
    ///  2-Dimensionaler Richtungsvektor (Einheitsvektor)
    /// </summary>
    public struct Direction2
    {
        #region Fields

        /// <summary>
        ///  Richtung der X-Achse eines Koordinatensystems
        /// </summary>
        public static Direction2 UnitX => new Direction2(1.0, 0.0);

        /// <summary>
        ///  Richtung der Y-Achse eines Koordinatensystems
        /// </summary>
        public static Direction2 UnitY => new Direction2(0.0, 1.0);

        /// <summary>
        /// 
        /// </summary>
        public static Direction2 NaN => new Direction2(double.NaN, double.NaN);

        #endregion Fields

        #region Constructors

        /// <summary>
        ///  Konstruktor mit Einzelwerten
        /// </summary>
        /// <param name="x"> X-Koordinate </param>
        /// <param name="y"> Y-Koordinate </param>
        private Direction2(double x, double y)
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
        ///  Richtung mit Winkel = 0 (X-Achse)
        /// </summary>
        public static Direction2 Zero => UnitX;

        /// <summary>
        /// 
        /// </summary>
        public double Sin => this.Y;

        /// <summary>
        /// 
        /// </summary>
        public double Cos => this.X;

        #endregion Properties

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        internal static Direction2 Create(Vector2 vector) => new Direction2(vector.X, vector.Y);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        internal static Direction2 Create(double x, double y) => new Direction2(x, y);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="norm"></param>
        /// <returns></returns>
        public static Direction2 Create(double x, double y, double norm) => new Direction2(x / norm, y / norm);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="norm"></param>
        /// <returns></returns>
        public static Direction2 Create(Vector2 vector, double? norm) => Create(vector.X, vector.Y, norm);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="norm"></param>
        /// <returns></returns>
        public static Direction2 Create(double x, double y, double? norm)
        {
            double nrm = norm ?? Common.Norm(x, y);
            return IsValidNorm(nrm) ? new Direction2(x / nrm, y / nrm) : UnitX;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="startIndex"></param>
        /// <param name="norm"></param>
        /// <returns></returns>
        public static Direction2 Create(IReadOnlyList<double> xy, int startIndex, double? norm) => Create(xy[startIndex], xy[startIndex + 1], norm);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="azimuth"></param>
        /// <returns></returns>
        public static Direction2 Create(double azimuth) => new Direction2(Math.Cos(azimuth), Math.Sin(azimuth));

        /// <summary>
        ///  Richtungswinkel
        /// </summary>
        public static double Azimuth(Direction2 direction) => Math.Atan2(direction.Y, direction.X);

        /// <summary>
        ///  Negation
        /// </summary>
        /// <param name="d"> Zu negierende Richtung </param>
        /// <returns> Negierte Richtung </returns>
        public static Direction2 operator -(Direction2 d) => new Direction2(d.X, -d.Y);

        /// <summary>
        ///  Richtungssubtraktion
        /// </summary>
        /// <param name="a"> Minuend </param>
        /// <param name="b"> Subtrahend </param>
        /// <returns> Differenz </returns>
        public static Direction2 operator -(Direction2 a, Direction2 b) => new Direction2((a.X * b.X) + (a.Y * b.Y), (a.Y * b.X) - (a.X * b.Y));

        /// <summary>
        ///  Richtungssubtraktion
        /// </summary>
        /// <param name="a"> Minuend </param>
        /// <param name="b"> Subtrahend </param>
        /// <returns> Differenz </returns>
        public static Vector2 operator -(Vector2 a, Direction2 b) => Vector2.Create((a.X * b.X) + (a.Y * b.Y), (a.X * b.Y) - (a.Y * b.X));

        /// <summary>
        ///  Richtungssubtraktion (Rotation um Z)
        /// </summary>
        /// <param name="a"> Minuend </param>
        /// <param name="b"> Subtrahend </param>
        /// <returns> Differenz </returns>
        public static Vector3 operator -(Vector3 a, Direction2 b) => Vector3.Create((a.X * b.X) + (a.Y * b.Y), (a.X * b.Y) - (a.Y * b.X), a.Z);

        /// <summary>
        ///  Richtungssubtraktion
        /// </summary>
        /// <param name="a"> Minuend </param>
        /// <param name="b"> Subtrahend </param>
        /// <returns> Differenz </returns>
        public static Point2 operator -(Point2 a, Direction2 b) => Point2.Create((a.X * b.X) + (a.Y * b.Y), (a.X * b.Y) - (a.Y * b.X));

        /// <summary>
        ///  Richtungssubtraktion (Rotation um Z)
        /// </summary>
        /// <param name="a"> Minuend </param>
        /// <param name="b"> Subtrahend </param>
        /// <returns> Differenz </returns>
        public static Point3 operator -(Point3 a, Direction2 b) => Point3.Create((a.X * b.X) + (a.Y * b.Y), (a.X * b.Y) - (a.Y * b.X), a.Z);

        /// <summary>
        ///  Richtungsaddition
        /// </summary>
        /// <param name="a"> 1. Summand </param>
        /// <param name="b"> 2. Summand </param>
        /// <returns> Summe </returns>
        public static Direction2 operator +(Direction2 a, Direction2 b) => new Direction2((a.X * b.X) - (a.Y * b.Y), (a.Y * b.X) + (a.X * b.Y));

        /// <summary>
        ///  Richtungsaddition (Rotation)
        /// </summary>
        /// <param name="a"> 1. Summand </param>
        /// <param name="b"> 2. Summand </param>
        /// <returns> Summe </returns>
        public static Vector2 operator +(Vector2 a, Direction2 b) => Vector2.Create((a.X * b.X) - (a.Y * b.Y), (a.Y * b.X) + (a.X * b.Y));

        /// <summary>
        ///  Richtungsaddition (Rotation)
        /// </summary>
        /// <param name="a"> 1. Summand </param>
        /// <param name="b"> 2. Summand </param>
        /// <returns> Summe </returns>
        public static Vector3 operator +(Vector3 a, Direction2 b) => Vector3.Create((a.X * b.X) - (a.Y * b.Y), (a.Y * b.X) + (a.X * b.Y), a.Z);

        /// <summary>
        ///  Richtungsaddition (Rotation um Ursprung)
        /// </summary>
        /// <param name="a"> 1. Summand </param>
        /// <param name="b"> 2. Summand </param>
        /// <returns> Summe </returns>
        public static Point2 operator +(Point2 a, Direction2 b) => Point2.Create((a.X * b.X) - (a.Y * b.Y), (a.Y * b.X) + (a.X * b.Y));

        /// <summary>
        ///  Richtungsaddition (Rotation um Ursprung)
        /// </summary>
        /// <param name="a"> 1. Summand </param>
        /// <param name="b"> 2. Summand </param>
        /// <returns> Summe </returns>
        public static Point3 operator +(Point3 a, Direction2 b) => Point3.Create((a.X * b.X) - (a.Y * b.Y), (a.Y * b.X) + (a.X * b.Y), a.Z);


        /// <summary>
        ///  Vektor Skalierung (Multiplikation)
        /// </summary>
        /// <param name="a"> Skalar </param>
        /// <param name="b"> Vektor </param>
        /// <returns> Skalierter Vektor </returns>
        public static Vector2 operator *(double a, Direction2 b) => Vector2.Create(a * b.X, a * b.Y);

        /// <summary>
        ///  Vektor Skalierung (Multiplikation)
        /// </summary>
        /// <param name="a"> Vektor </param>
        /// <param name="b"> Skalar </param>
        /// <returns> Skalierter Vektor </returns>
        public static Vector2 operator *(Direction2 a, double b) => Vector2.Create(a.X * b, a.Y * b);

        /// <summary>
        ///  Umkehrung der Richtung
        /// </summary>
        /// <param name="d"> Umzukehrende Richtung </param>
        /// <returns> Umgekehrte Richtung </returns>
        public static Direction2 Reverse(Direction2 d) => new Direction2(-d.X, -d.Y);

        /// <summary>
        ///  Mittlere Richtung zweier Richtungen
        /// </summary>
        /// <param name="a"> 1. Richtung </param>
        /// <param name="b"> 2. Richtung </param>
        /// <returns> Mittlere Richtung </returns>
        public static Direction2 Bisector(Direction2 a, Direction2 b)
        {
            double x = a.X + b.X;
            double y = a.Y + b.Y;
            double r = Common.Norm(x, y);
            return r > TRIGTOL
                ? new Direction2(x / r, y / r)
                : Perp(a); // Richtungen entgegengesetzt kollinear
        }

        /// <summary>
        ///  Sind 2 Richtungen kollinear
        /// </summary>
        /// <param name="a"> Richtung 1 </param>
        /// <param name="b"> Richtung 2 </param>
        /// <returns> Senkrecht? </returns>
        public static bool AreCollinear(Direction2 a, Direction2 b)
        {
            double det = (a.X * b.Y) - (a.Y * b.X);
            return -TRIGTOL < det && det < TRIGTOL;
        }

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Vector2 a, Direction2 b) => (a.X * b.X) + (a.Y * b.Y);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Direction2 a, Vector2 b) => (a.X * b.X) + (a.Y * b.Y);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Direction2 a, Direction2 b) => (a.X * b.X) + (a.Y * b.Y);

        /// <summary>
        ///  Determinante einer 2x2 Matrix aus zwei Vektoren
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Determinante </returns>
        public static double Det(Vector2 a, Direction2 b) => (a.X * b.Y) - (a.Y * b.X);

        /// <summary>
        ///  Determinante einer 2x2 Matrix aus zwei Vektoren
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Determinante </returns>
        public static double Det(Direction2 a, Vector2 b) => (a.X * b.Y) - (a.Y * b.X);

        /// <summary>
        ///  Determinante einer 2x2 Matrix aus zwei Vektoren
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Determinante </returns>
        public static double Det(Direction2 a, Direction2 b) => (a.X * b.Y) - (a.Y * b.X);

        /// <summary>
        ///  Projiziert Vektor auf Richtung
        /// </summary>
        /// <param name="direction"> Richtung </param>
        /// <param name="vector">    Vektor </param>
        /// <returns> Projizierter Vektor </returns>
        public static Vector2 Projection(Direction2 direction, Vector2 vector) => Dot(vector, direction) * direction;

        /// <summary>
        ///  Sind 2 Richtungen Senkrecht zueinander
        /// </summary>
        /// <param name="a"> Richtung 1 </param>
        /// <param name="b"> Richtung 2 </param>
        /// <returns> Senkrecht? </returns>
        public static bool ArePerp(Direction2 a, Direction2 b)
        {
            double dot = (a.X * b.X) + (a.Y * b.Y);
            return -TRIGTOL < dot && dot < TRIGTOL;
        }

        /// <summary>
        ///  Rotate by 90° (Perpendicular) counterclockwise or if needed clockwise
        /// </summary>
        /// <param name="d">         Richtung </param>
        /// <param name="clockwise"> im Uhrzeigersinn (default false) </param>
        /// <returns> Senkrechte </returns>
        public static Direction2 Perp(Direction2 d, bool clockwise = false) => clockwise ? new Direction2(d.Y, -d.X) : new Direction2(-d.Y, d.X);

        /// <summary>
        /// Reflects a Direction with another Direction
        /// </summary>
        /// <param name="toReflect"></param>
        /// <param name="reflector"></param>
        /// <returns></returns>
        public static Direction2 ReflectWith(Direction2 toReflect, Direction2 reflector)
        {
            var scale = 2.0 * Dot(toReflect, reflector);
            return new Direction2((scale * reflector.X) - toReflect.X, (scale * reflector.Y) - toReflect.Y);
        }

        /// <summary>
        /// Reflects a Direction with Unit X
        /// </summary>
        /// <param name="toReflect"></param>
        /// <returns></returns>
        public static Direction2 ReflectWithUnitX(Direction2 toReflect) => -toReflect;

        /// <summary>
        /// Reflects a Direction with Unit Y
        /// </summary>
        /// <param name="toReflect"></param>
        /// <returns></returns>
        public static Direction2 ReflectWithUnitY(Direction2 toReflect) => new Direction2(-toReflect.X, toReflect.Y);

        /// <summary>
        /// Swaps X and Y
        /// </summary>
        public static Direction2 Swap(Direction2 direction) => new Direction2(direction.Y, direction.X);

        /// <summary>
        /// Geometrischer Vergleich
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Coincident(Direction2 other)
        {
            return AreCollinear(this, other);
        }


        #endregion Methods
    }
}