using System;
using System.Collections.Generic;
using static BimGisCad.Representation.Geometry.Elementary.Common;

namespace BimGisCad.Representation.Geometry.Elementary
{

    /// <summary>
    ///  1-Dimensionaler Vektor (Skalarwert)
    /// </summary>
    public struct Vector1
    {
        #region Fields

        /// <summary>
        ///  Vektor mit Wert 0
        /// </summary>
        public static Vector1 Zero => new Vector1(0.0);

        /// <summary>
        /// Einheitsvektor
        /// </summary>
        public static Vector1 Unit => new Vector1(1.0);

        /// <summary>
        /// Ungültiger Vektor
        /// </summary>
        public static Vector1 NaN => new Vector1(double.NaN);

        /// <summary>
        /// 
        /// </summary>
        public static Vector1 PositiveInfinity => new Vector1(double.PositiveInfinity);

        /// <summary>
        /// 
        /// </summary>
        public static Vector1 NegativeInfinity => new Vector1(double.NegativeInfinity);

        #endregion Fields

        #region Constructors

        /// <summary>
        ///  Konstruktor
        /// </summary>
        /// <param name="z"> Wert </param>
        private Vector1(double z)
        {
            this.Z = z;
        }

        #endregion Constructors

        /// <summary>
        /// Z-Koordinate
        /// </summary>
        public readonly double Z;

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector1 Create(Vector1 vector) => new Vector1(vector.Z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Vector1 Create(double z) => new Vector1(z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="z"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static Vector1 Create(IReadOnlyList<double> z, int startIndex = 0) => new Vector1(z[startIndex]);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static double Norm(Vector1 vector) => Math.Abs(vector.Z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static double Norm2(Vector1 vector) => vector.Z * vector.Z;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static bool IsNaN(Vector1 vector) => double.IsNaN(vector.Z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="norm"></param>
        /// <returns></returns>
        public static bool IsValidNorm(double norm) => !double.IsNaN(norm) && norm > EPS;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="norm"></param>
        /// <returns></returns>
        public static bool TryGetNorm(Vector1 vector, out double norm)
        {
            norm = Norm(vector);
            return IsValidNorm(norm);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static bool Normalized(Vector1 vector, out Vector1 unit) {
            double norm;
            if(TryGetNorm(vector, out norm))
            {
                unit = Vector1.Unit;
                return true;
            }
            else
            {
                unit = default(Vector1);
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
        public static bool Normalized(Vector1 vector, double norm, out Vector1 unit)
        {
            if(IsValidNorm(norm))
            {
                unit = Vector1.Unit;
                return true;
            }
            else
            {
                unit = default(Vector1);
                return false;
            }
        }

        /// <summary>
        ///  Negation
        /// </summary>
        /// <param name="vector"> Zu negierender Vektor </param>
        /// <returns> Negierter Vektor </returns>
        public static Vector1 operator -(Vector1 vector) => new Vector1(-vector.Z);

        /// <summary>
        ///  Vektor Subtraktion
        /// </summary>
        /// <param name="a"> Minuend </param>
        /// <param name="b"> Subtrahend </param>
        /// <returns> Differenz </returns>
        public static Vector1 operator -(Vector1 a, Vector1 b) => new Vector1(a.Z - b.Z);

        /// <summary>
        ///  Vektor Addition
        /// </summary>
        /// <param name="a"> 1. Summand </param>
        /// <param name="b"> 2. Summand </param>
        /// <returns> Summe </returns>
        public static Vector1 operator +(Vector1 a, Vector1 b) => new Vector1(a.Z + b.Z);

        /// <summary>
        ///  Vektor Skalierung (Multiplikation)
        /// </summary>
        /// <param name="a"> Skalar </param>
        /// <param name="b"> Vektor </param>
        /// <returns> Skalierter Vektor </returns>
        public static Vector1 operator *(double a, Vector1 b) => new Vector1(a * b.Z);

        /// <summary>
        ///  Vektor Skalierung (Multiplikation)
        /// </summary>
        /// <param name="a"> Vektor </param>
        /// <param name="b"> Skalar </param>
        /// <returns> Skalierter Vektor </returns>
        public static Vector1 operator *(Vector1 a, double b) => new Vector1(a.Z * b);

        /// <summary>
        ///  Vektor Skalierung (Division)
        /// </summary>
        /// <param name="a"> Vektor </param>
        /// <param name="b"> Skalar (Divisor) </param>
        /// <returns> Skalierter Vektor </returns>
        public static Vector1 operator /(Vector1 a, double b) => new Vector1(a.Z / b);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Vector1 a, Vector1 b) => a.Z * b.Z;

        /// <summary>
        /// Geometrischer Vergleich
        /// </summary>
        /// <param name="other"></param>
        /// <param name="minDist">kleinstmöglicher Abstand</param>
        /// <returns></returns>
        public bool Coincident(Vector1 other, double minDist = MINDIST)
        {
            double dz = other.Z - this.Z;
            return IsNearlyZeroSquared(dz * dz, minDist * minDist);
        }

        #endregion Methods
    }
}