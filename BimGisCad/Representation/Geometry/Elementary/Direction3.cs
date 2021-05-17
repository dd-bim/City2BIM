using System;
using System.Collections.Generic;
using System.Globalization;
using static BimGisCad.Representation.Geometry.Elementary.Common;

namespace BimGisCad.Representation.Geometry.Elementary
{
    /// <summary>
    ///  3-Dimensionaler Richtungsvektor (Einheitsvektor)
    /// </summary>
    public struct Direction3
    {
        #region Fields

        /// <summary>
        ///  Richtung der X-Achse eines Standardkoordinatensystems
        /// </summary>
        public static Direction3 UnitX => new Direction3(1.0, 0.0, 0.0);

        /// <summary>
        ///  Richtung der Y-Achse eines Standardkoordinatensystems
        /// </summary>
        public static Direction3 UnitY => new Direction3(0.0, 1.0, 0.0);

        /// <summary>
        ///  Richtung der Z-Achse eines Standardkoordinatensystems
        /// </summary>
        public static Direction3 UnitZ => new Direction3(0.0, 0.0, 1.0);

        /// <summary>
        ///  Richtung der X-Achse eines Standardkoordinatensystems
        /// </summary>
        public static Direction3 NegUnitX => new Direction3(-1.0, 0.0, 0.0);

        /// <summary>
        ///  Richtung der Y-Achse eines Standardkoordinatensystems
        /// </summary>
        public static Direction3 NegUnitY => new Direction3(0.0, -1.0, 0.0);

        /// <summary>
        ///  Richtung der Z-Achse eines Standardkoordinatensystems
        /// </summary>
        public static Direction3 NegUnitZ => new Direction3(0.0, 0.0, -1.0);

        /// <summary>
        /// 
        /// </summary>
        public static Direction3 NaN => new Direction3(double.NaN, double.NaN, double.NaN);

        #endregion Fields

        #region Constructors

        /// <summary>
        ///  Konstruktor mit Einzelwerten
        /// </summary>
        /// <param name="x"> X-Koordinate </param>
        /// <param name="y"> Y-Koordinate </param>
        /// <param name="z"> Z-Koordinate </param>
        private Direction3(double x, double y, double z)
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
        ///  Richtung mit Winkel = 0 (X-Achse)
        /// </summary>
        public static Direction3 Zero => UnitX;

        #endregion Properties

        #region Methods

        internal static Direction3 Create(Vector3 vector) => new Direction3(vector.X, vector.Y, vector.Z);

        internal static Direction3 Create(double x, double y, double z) => new Direction3(x, y, z );

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="norm"></param>
        /// <returns></returns>
        public static Direction3 Create(double x, double y, double z, double norm) => new Direction3(x / norm, y / norm, z / norm);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="norm"></param>
        /// <returns></returns>
        public static Direction3 Create(Vector3 vector, double? norm) => Create(vector.X, vector.Y, vector.Z, norm);
 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="norm"></param>
        /// <returns></returns>
        public static Direction3 Create(double x, double y, double z, double? norm)
        {
            double nrm = norm ?? Common.Norm(x, y, z);
            return IsValidNorm(nrm) ? new Direction3(x / nrm, y / nrm, z / nrm) : UnitZ;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xyz"></param>
        /// <param name="startIndex"></param>
        /// <param name="norm"></param>
        /// <returns></returns>
        public static Direction3 Create(IReadOnlyList<double> xyz, int startIndex, double? norm) => Create(xyz[startIndex], xyz[startIndex + 1], xyz[startIndex + 2], norm);

        /// <summary>
        ///  Erzeugt Richtungsvektor aus Richtungs- und Zenitwinkel
        /// </summary>
        /// <param name="azimuth">     Richtungswinkel </param>
        /// <param name="inclination"> Zenitwinkel </param>
        /// <returns> Richtungsvektor </returns>
        public static Direction3 Create(double azimuth, double inclination)
        {
            double sin = Math.Sin(inclination);
            return new Direction3(sin * Math.Cos(azimuth), sin * Math.Sin(azimuth), Math.Cos(inclination));
        }

        /// <summary>
        ///  Erzeugt Richtungsvektor aus Richtungs- und Zenitwinkel
        /// </summary>
        /// <param name="azimuth">     Richtungswinkel </param>
        /// <param name="inclination"> Zenitwinkel </param>
        /// <returns> Richtungsvektor </returns>
        public static Direction3 Create(Direction2 azimuth, Direction2 inclination) => new Direction3(inclination.Sin * azimuth.Cos, inclination.Sin * azimuth.Sin, inclination.Cos);

        /// <summary>
        ///  Vektor Skalierung (Multiplikation)
        /// </summary>
        /// <param name="a"> Skalar </param>
        /// <param name="b"> Vektor </param>
        /// <returns> Skalierter Vektor </returns>
        public static Vector3 operator *(double a, Direction3 b) => Vector3.Create(a * b.X, a * b.Y, a * b.Z);

        /// <summary>
        ///  Vektor Skalierung (Multiplikation)
        /// </summary>
        /// <param name="a"> Vektor </param>
        /// <param name="b"> Skalar </param>
        /// <returns> Skalierter Vektor </returns>
        public static Vector3 operator *(Direction3 a, double b) => Vector3.Create(a.X * b, a.Y * b, a.Z * b);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Direction3 a, Vector3 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Vector3 a, Direction3 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

        /// <summary>
        ///  Skalarprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Skalarprodukt </returns>
        public static double Dot(Direction3 a, Direction3 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

        /// <summary>
        ///  Kreuzprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Kreuzprodukt </returns>
        public static Vector3 Cross(Direction3 a, Vector3 b) => Vector3.Create((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

        /// <summary>
        ///  Kreuzprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Kreuzprodukt </returns>
        public static Vector3 Cross(Vector3 a, Direction3 b) => Vector3.Create((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

        /// <summary>
        ///  Kreuzprodukt
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <returns> Kreuzprodukt </returns>
        public static Vector3 Cross(Direction3 a, Direction3 b) => Vector3.Create((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

        /// <summary>
        ///  Determinante einer 3x3 Matrix aus drei Vektoren (Spatprodukt)
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <param name="c"> 3. Vektor </param>
        /// <returns> Determinante </returns>
        public static double Det(Direction3 a, Vector3 b, Vector3 c) => Dot(a, Vector3.Cross(b, c));

        /// <summary>
        ///  Determinante einer 3x3 Matrix aus drei Vektoren (Spatprodukt)
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <param name="c"> 3. Vektor </param>
        /// <returns> Determinante </returns>
        public static double Det(Vector3 a, Direction3 b, Vector3 c) => Vector3.Dot(a, Cross(b, c));

        /// <summary>
        ///  Determinante einer 3x3 Matrix aus drei Vektoren (Spatprodukt)
        /// </summary>
        /// <param name="a"> 1. Vektor </param>
        /// <param name="b"> 2. Vektor </param>
        /// <param name="c"> 3. Vektor </param>
        /// <returns> Determinante </returns>
        public static double Det(Vector3 a, Vector3 b, Direction3 c) => Vector3.Dot(a, Cross(b, c));

        /// <summary>
        /// Reflects a Direction with another Direction
        /// </summary>
        /// <param name="toReflect"></param>
        /// <param name="reflector"></param>
        /// <returns></returns>
        public static Direction3 ReflectWith(Direction3 toReflect, Direction3 reflector)
        {
            var scale = 2.0 * Dot(toReflect, reflector);
            return new Direction3((scale * reflector.X) - toReflect.X, (scale * reflector.Y) - toReflect.Y, (scale * reflector.Z) - toReflect.Z);
        }

        /// <summary>
        /// Reflects a Direction with Unit X
        /// </summary>
        /// <param name="toReflect"></param>
        /// <returns></returns>
        public static Direction3 ReflectWithUnitX(Direction3 toReflect) => new Direction3(toReflect.X, -toReflect.Y, -toReflect.Z);

        /// <summary>
        /// Reflects a Direction with Unit Y
        /// </summary>
        /// <param name="toReflect"></param>
        /// <returns></returns>
        public static Direction3 ReflectWithUnitY(Direction3 toReflect) => new Direction3(-toReflect.X, toReflect.Y, -toReflect.Z);

        /// <summary>
        /// Reflects a Direction with Unit Z
        /// </summary>
        /// <param name="toReflect"></param>
        /// <returns></returns>
        public static Direction3 ReflectWithUnitZ(Direction3 toReflect) => new Direction3(-toReflect.X, -toReflect.Y, toReflect.Z);

        /// <summary>
        /// Multipliziert Zeilenvektor mit Matrix[rx,ry,rz] (rx ry und rz müssen orthonormal sein!)
        /// </summary>
        /// <param name="row"></param>
        /// <param name="rx"></param>
        /// <param name="ry"></param>
        /// <param name="rz"></param>
        /// <returns></returns>
        public static Direction3 RotateRow(Direction3 row, Direction3 rx, Direction3 ry, Direction3 rz) => new Direction3(
             (row.X * rx.X) + (row.Y * rx.Y) + (row.Z * rx.Z),
             (row.X * ry.X) + (row.Y * ry.Y) + (row.Z * ry.Z),
             (row.X * rz.X) + (row.Y * rz.Y) + (row.Z * rz.Z));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="rx"></param>
        /// <param name="ry"></param>
        /// <param name="rz"></param>
        /// <returns></returns>
        public static Vector3 RotateRow(Vector3 row, Direction3 rx, Direction3 ry, Direction3 rz) => Vector3.Create(
             (row.X * rx.X) + (row.Y * rx.Y) + (row.Z * rx.Z),
             (row.X * ry.X) + (row.Y * ry.Y) + (row.Z * ry.Z),
             (row.X * rz.X) + (row.Y * rz.Y) + (row.Z * rz.Z));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="rx"></param>
        /// <param name="ry"></param>
        /// <param name="rz"></param>
        /// <returns></returns>
        public static Point3 RotateRow(Point3 row, Direction3 rx, Direction3 ry, Direction3 rz) => Point3.Create(
             (row.X * rx.X) + (row.Y * rx.Y) + (row.Z * rx.Z),
             (row.X * ry.X) + (row.Y * ry.Y) + (row.Z * ry.Z),
             (row.X * rz.X) + (row.Y * rz.Y) + (row.Z * rz.Z));

        /// <summary>
        /// Multipliziert Matrix[rx,ry,rz] mit Spaltenvektor (rx ry und rz müssen orthonormal sein!)
        /// </summary>
        /// <param name="rx"></param>
        /// <param name="ry"></param>
        /// <param name="rz"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public static Direction3 RotateCol(Direction3 rx, Direction3 ry, Direction3 rz, Direction3 col) => new Direction3(
             (col.X * rx.X) + (col.Y * ry.X) + (col.Z * rz.X),
             (col.X * rx.Y) + (col.Y * ry.Y) + (col.Z * rz.Y),
             (col.X * rx.Z) + (col.Y * ry.Z) + (col.Z * rz.Z));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rx"></param>
        /// <param name="ry"></param>
        /// <param name="rz"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public static Vector3 RotateCol(Direction3 rx, Direction3 ry, Direction3 rz, Vector3 col) => Vector3.Create(
             (col.X * rx.X) + (col.Y * ry.X) + (col.Z * rz.X),
             (col.X * rx.Y) + (col.Y * ry.Y) + (col.Z * rz.Y),
             (col.X * rx.Z) + (col.Y * ry.Z) + (col.Z * rz.Z));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rx"></param>
        /// <param name="ry"></param>
        /// <param name="rz"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public static Point3 RotateCol(Direction3 rx, Direction3 ry, Direction3 rz, Point3 col) => Point3.Create(
              (col.X * rx.X) + (col.Y * ry.X) + (col.Z * rz.X),
              (col.X * rx.Y) + (col.Y * ry.Y) + (col.Z * rz.Y),
              (col.X * rx.Z) + (col.Y * ry.Z) + (col.Z * rz.Z));

         /// <summary>
        ///  Richtungswinkel
        /// </summary>
        public static double Azimuth(Direction3 direction) => Math.Atan2(direction.Y, direction.X);

        /// <summary>
        ///  Zenitwinkel
        /// </summary>
        public static double Inclination(Direction3 direction) => Math.Acos(direction.Z);

        /// <summary>
        ///  Umkehrung der Richtung
        /// </summary>
        /// <param name="d"> Umzukehrende Richtung </param>
        /// <returns> Umgekehrte Richtung </returns>
        public static Direction3 Reverse(Direction3 d) => new Direction3(-d.X, -d.Y, -d.Z);

        /// <summary>
        ///  Mittlere Richtung zweier Richtungen
        /// </summary>
        /// <param name="a"> 1. Richtung </param>
        /// <param name="b"> 2. Richtung </param>
        /// <returns> Mittlere Richtung </returns>
        public static Direction3 Bisector(Direction3 a, Direction3 b)
        {
            double x = a.X + b.X;
            double y = a.Y + b.Y;
            double z = a.Z + b.Z;
            double r = Common.Norm(x, y, z);
            return r > TRIGTOL ? new Direction3(x / r, y / r, z / r)
                : Perp(a); // Richtungen entgegengesetzt kollinear, keine Mitte definiert
        }

        /// <summary>
        ///  Projiziert Vektor auf Richtung
        /// </summary>
        /// <param name="direction"> Richtung </param>
        /// <param name="vector">    Vektor </param>
        /// <returns> Projizierter Vektor </returns>
        public static Vector3 Projection(Direction3 direction, Vector3 vector) => Dot(vector, direction) * direction;


        /// <summary>
        ///  Sind 2 Richtungen kollinear
        /// </summary>
        /// <param name="a"> Richtung 1 </param>
        /// <param name="b"> Richtung 2 </param>
        /// <returns> Senkrecht? </returns>
        public static bool AreCollinear(Direction3 a, Direction3 b)
        {
            //Math.Abs((a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z)) - 1.0 <= TRIGTOL;
            double det = (a.Y * b.Z) - (a.Z * b.Y);
            if(-TRIGTOL <= det && det <= TRIGTOL)
            {
                det = (a.Z * b.X) - (a.X * b.Z);
                if(-TRIGTOL <= det && det <= TRIGTOL)
                {
                    det = (a.X * b.Y) - (a.Y * b.X);
                    return -TRIGTOL < det && det < TRIGTOL;
                }
            }
            return false;
        }

        /// <summary>
        ///  Sind 2 Richtungen Senkrecht zueinander
        /// </summary>
        /// <param name="a"> Richtung 1 </param>
        /// <param name="b"> Richtung 2 </param>
        /// <returns> Senkrecht? </returns>
        public static bool ArePerp(Direction3 a, Direction3 b)
        {
            double dot = (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
            return -TRIGTOL < dot && dot < TRIGTOL;
        }

        /// <summary>
        ///  Beliebige Senkrechte zur Richtung
        /// </summary>
        /// <param name="dir"> Richtung </param>
        /// <returns> Senkrechte </returns>
        public static Direction3 Perp(Direction3 dir)
        {
            double ax = Math.Abs(dir.X), ay = Math.Abs(dir.Y), az = Math.Abs(dir.Z), norm;
            if(ax < ay)
            {
                if(ax < az)
                {
                    norm = Math.Sqrt((ay * ay) + (az * az));
                    return new Direction3(0.0, dir.Z / norm, -dir.Y / norm);
                }
                else
                {
                    norm = Math.Sqrt((ax * ax) + (ay * ay));
                    return new Direction3(dir.Y / norm, -dir.X / norm, 0.0);
                }
            }
            else
            {
                if(ay < az)
                {
                    norm = Math.Sqrt((ax * ax) + (az * az));
                    return new Direction3(-dir.Z / norm, 0.0, dir.X / norm);
                }
                else
                {
                    norm = Math.Sqrt((ax * ax) + (ay * ay));
                    return new Direction3(dir.Y / norm, -dir.X / norm, 0.0);
                }
            }
        }

        /// <summary>
        ///  Beliebige Senkrechten zur Richtung
        /// </summary>
        /// <param name="dirX"> Richtung </param>
        /// <param name="dirY"> Richtung </param>
        /// <param name="dirZ"> Richtung </param>
        public static void Perp(Direction3 dirX, out Direction3 dirY, out Direction3 dirZ)
        {
            double ax = Math.Abs(dirX.X), ay = Math.Abs(dirX.Y), az = Math.Abs(dirX.Z), norm;
            if(ax < ay)
            {
                if(ax < az)
                {
                    norm = Math.Sqrt((ay * ay) + (az * az));
                    dirY = new Direction3(0.0, dirX.Z / norm, -dirX.Y / norm);
                }
                else
                {
                    norm = Math.Sqrt((ax * ax) + (ay * ay));
                    dirY = new Direction3(dirX.Y / norm, -dirX.X / norm, 0.0);
                }
            }
            else
            {
                if(ay < az)
                {
                    norm = Math.Sqrt((ax * ax) + (az * az));
                    dirY = new Direction3(-dirX.Z / norm, 0.0, dirX.X / norm);
                }
                else
                {
                    norm = Math.Sqrt((ax * ax) + (ay * ay));
                    dirY = new Direction3(dirX.Y / norm, -dirX.X / norm, 0.0);
                }
            }
            dirZ = new Direction3((dirX.Y * dirY.Z) - (dirX.Z * dirY.Y), (dirX.Z * dirY.X) - (dirX.X * dirY.Z), (dirX.X * dirY.Y) - (dirX.Y * dirY.X));
        }

        /// <summary>
        ///  Senkrechte zur 1. Richtung in Ebene der 2 Richtungen
        /// </summary>
        /// <param name="fixedDir"> Feste Richtung </param>
        /// <param name="planeDir"> Richtung in Ebene </param>
        /// <returns>  </returns>
        public static Direction3 Perp(Direction3 fixedDir, Direction3 planeDir)
        {
            double cos = Dot(fixedDir, planeDir);
            double sin2 = 1.0 - (cos * cos);
            return sin2 < TRIGTOL_SQUARED
                ? Perp(fixedDir) // Kollinear
                : Create(planeDir.X - (cos * fixedDir.X), planeDir.Y - (cos * fixedDir.Y), planeDir.Z - (cos * fixedDir.Z), Math.Sqrt(sin2));
        }

        /// <summary>
        /// Geometrischer Vergleich
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Coincident(Direction3 other)
        {
            return AreCollinear(this, other);
        }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0:F3} {1:F3} {2:F3}", X, Y, Z);

        #endregion Methods
    }
}