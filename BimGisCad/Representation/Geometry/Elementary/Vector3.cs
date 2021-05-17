using System;
using System.Collections.Generic;
using System.Globalization;
using static BimGisCad.Representation.Geometry.Elementary.Common;

namespace BimGisCad.Representation.Geometry.Elementary
{
    /// <summary>
    ///  3-Dimensionaler Vektor
    /// </summary>
    public struct Vector3
    {
        #region Fields

        /// <summary>
        ///  Vektor mit Magnitude 0
        /// </summary>
        public static Vector3 Zero => new Vector3(0.0, 0.0, 0.0);

        /// <summary>
        /// 
        /// </summary>
        public static Vector3 UnitX => new Vector3(1.0, 0.0, 0.0);

        /// <summary>
        /// 
        /// </summary>
        public static Vector3 UnitY => new Vector3(0.0, 1.0, 0.0);

        /// <summary>
        /// 
        /// </summary>
        public static Vector3 UnitZ => new Vector3(0.0, 0.0, 1.0);

        /// <summary>
        /// 
        /// </summary>
        public static Vector3 NaN => new Vector3(double.NaN, double.NaN, double.NaN);

        /// <summary>
        /// 
        /// </summary>
        public static Vector3 PositiveInfinity => new Vector3(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);

        /// <summary>
        /// 
        /// </summary>
        public static Vector3 NegativeInfinity => new Vector3(double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity);

        #endregion Fields

        #region Constructors

        /// <summary>
        ///  Konstruktor mit Einzelwerten
        /// </summary>
        /// <param name="x"> X-Koordinate </param>
        /// <param name="y"> Y-Koordinate </param>
        /// <param name="z"> Z-Koordinate </param>
        private Vector3(double x, double y, double z)
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

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector3 Create(Vector3 vector) => new Vector3(vector.X, vector.Y, vector.Z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector3 Create(Point3 vector) => new Vector3(vector.X, vector.Y, vector.Z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector3 Create(Direction3 vector) => new Vector3(vector.X, vector.Y, vector.Z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Vector3 Create(double x, double y, double z) => new Vector3(x, y, z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public static Vector3 Create(double xyz) => new Vector3(xyz, xyz, xyz);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xyz"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static Vector3 Create(IReadOnlyList<double> xyz, int startIndex = 0) => new Vector3(xyz[startIndex], xyz[startIndex + 1], xyz[startIndex + 2]);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static bool IsNaN(Vector3 vector) => double.IsNaN(vector.X) || double.IsNaN(vector.Y) || double.IsNaN(vector.Z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static double Norm(Vector3 vector) => Common.Norm(vector.X, vector.Y, vector.Z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static double Norm2(Vector3 vector) => (vector.X * vector.X) + (vector.Y * vector.Y) + (vector.Z * vector.Z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="norm"></param>
        /// <returns></returns>
        public static bool TryGetNorm(Vector3 vector, out double norm)
        {
            norm = Norm(vector);
            return norm > Common.EPS;
        }

        /// <summary>
        ///  Normalisierung des Vektors (Magnitude = 1)
        /// </summary>
        public static bool Normalized(Vector3 vector, out Vector3 unit)
        {
            double norm;
            if(TryGetNorm(vector, out norm))
            {
                unit = vector / norm;
                return true;
            }
            else
            {
                unit = default(Vector3);
                return false;
            }
        }

        /// <summary>
        ///  Normalisierung des Vektors (Magnitude = 1)
        /// </summary>
        public static bool Normalized(Vector3 vector, double norm, out Vector3 unit)
        {
            if(IsValidNorm(norm))
            {
                unit = vector / norm;
                return true;
            }
            else
            {
                unit = default(Vector3);
                return false;
            }
        }

        /// <summary>
        ///  Negation
        /// </summary>
        /// <param name="vector"> Zu negierender Vektor </param>
        /// <returns> Negierter Vektor </returns>
        public static Vector3 operator -(Vector3 vector) => new Vector3(-vector.X, -vector.Y, -vector.Z);

        /// <summary>
        ///  Vektor Subtraktion
        /// </summary>
        /// <param name="a"> Minuend </param>
        /// <param name="b"> Subtrahend </param>
        /// <returns> Differenz </returns>
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        /// <summary>
        ///  Vektor Addition
        /// </summary>
        /// <param name="a"> 1. Summand </param>
        /// <param name="b"> 2. Summand </param>
        /// <returns> Summe </returns>
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);


        /// <summary>
        ///  Scale X and Y not Z
        /// </summary>
        public static Vector3 ScaleXY(double scale, Vector3 vector) => new Vector3(scale * vector.X, scale * vector.Y, vector.Z);

        /// <summary>
        ///  Unscale X and Y not Z (Division)
        /// </summary>
        public static Vector3 UnScaleXY(double scale, Vector3 vector) => new Vector3(vector.X / scale, vector.Y / scale, vector.Z);

        /// <summary>
        ///  Vektor Skalierung (Multiplikation)
        /// </summary>
        /// <param name="a"> Skalar </param>
        /// <param name="b"> Vektor </param>
        /// <returns> Skalierter Vektor </returns>
        public static Vector3 operator *(double a, Vector3 b) => new Vector3(a * b.X, a * b.Y, a * b.Z);

        /// <summary>
        ///  Vektor Skalierung (Multiplikation)
        /// </summary>
        /// <param name="a"> Vektor </param>
        /// <param name="b"> Skalar </param>
        /// <returns> Skalierter Vektor </returns>
        public static Vector3 operator *(Vector3 a, double b) => new Vector3(a.X * b, a.Y * b, a.Z * b);

        /// <summary>
        ///  Vektor Skalierung (Division)
        /// </summary>
        /// <param name="a"> Vektor </param>
        /// <param name="b"> Skalar (Divisor) </param>
        /// <returns> Skalierter Vektor </returns>
        public static Vector3 operator /(Vector3 a, double b) => new Vector3(a.X / b, a.Y / b, a.Z / b);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Vector3 a, Vector3 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

        /// <summary>
        ///  Kreuzprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Kreuzprodukt </returns>
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

        /// <summary>
        ///  Determinante einer 3x3 Matrix aus drei Vektoren (Spatprodukt)
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <param name="c"> 3. Vektor </param>
        /// <returns> Determinante </returns>
        public static double Det(Vector3 a, Vector3 b, Vector3 c) => Dot(a, Cross(b, c));

        /// <summary>
        /// Reflects a Vector with a Direction
        /// </summary>
        /// <param name="toReflect"></param>
        /// <param name="reflector"></param>
        /// <returns></returns>
        public static Vector3 ReflectWith(Vector3 toReflect, Direction3 reflector)
        {
            var scale = 2.0 * Direction3.Dot(toReflect, reflector);
            return new Vector3((scale * reflector.X) - toReflect.X, (scale * reflector.Y) - toReflect.Y, (scale * reflector.Z) - toReflect.Z);
        }

        /// <summary>
        /// Reflects a Vector with Unit X
        /// </summary>
        /// <param name="toReflect"></param>
        /// <returns></returns>
        public static Vector3 ReflectWithUnitX(Vector3 toReflect) => new Vector3(toReflect.X, -toReflect.Y, -toReflect.Z);

        /// <summary>
        /// Reflects a Vector with Unit Y
        /// </summary>
        /// <param name="toReflect"></param>
        /// <returns></returns>
        public static Vector3 ReflectWithUnitY(Vector3 toReflect) => new Vector3(-toReflect.X, toReflect.Y, -toReflect.Z);

        /// <summary>
        /// Reflects a Vector with Unit Z
        /// </summary>
        /// <param name="toReflect"></param>
        /// <returns></returns>
        public static Vector3 ReflectWithUnitZ(Vector3 toReflect) => new Vector3(-toReflect.X, -toReflect.Y, toReflect.Z);

        /// <summary>
        /// Determinante eines Tetraeders
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static double TetDet(Vector3 a, Vector3 b, Vector3 c, Vector3 d) => Det(a - d, b - d, c - d);


        /// <summary>
        ///  Mittelwert zweier Vectoren
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Mittelpunkt </returns>
        public static Vector3 Mean(Vector3 a, Vector3 b) => new Vector3((a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0, (a.Z + b.Z) / 2.0);

        /// <summary>
        ///  Mittelwert dreier Vectoren
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <param name="c"> 3. Vektor </param>
        /// <returns> Mittelpunkt </returns>
        public static Vector3 Mean(Vector3 a, Vector3 b, Vector3 c) => new Vector3((a.X + b.X + c.X) / 3.0, (a.Y + b.Y + c.Y) / 3.0, (a.Z + b.Z + c.Z) / 3.0);

        /// <summary>
        /// Geometrischer Vergleich
        /// </summary>
        /// <param name="other"></param>
        /// <param name="minDist">kleinstmöglicher Abstand</param>
        /// <returns></returns>
        public bool Coincident(Vector3 other, double minDist = MINDIST)
        {
            double dx = other.X - this.X;
            double dy = other.Y - this.Y;
            double dz = other.Z - this.Z;
            return IsNearlyZeroSquared((dx * dx) + (dy * dy) + (dz * dz), minDist * minDist);
        }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0:F3} {1:F3} {2:F3}", X, Y, Z);

        #endregion Methods
    }
}