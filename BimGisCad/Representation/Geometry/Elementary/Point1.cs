using System;
using System.Collections.Generic;
using static BimGisCad.Representation.Geometry.Elementary.Common;
using System.Linq;

namespace BimGisCad.Representation.Geometry.Elementary
{

    /// <summary>
    ///  1-Dimensionaler Punkt
    /// </summary>
    public struct Point1
    {
        #region Fields

        /// <summary>
        ///  Punkt im Ursprung
        /// </summary>
        public static Point1 Zero => new Point1(0.0);

        #endregion Fields

        #region Constructors

        /// <summary>
        ///  Konstruktor mit Wert
        /// </summary>
        /// <param name="z"> Koordinate </param>
        private Point1(double z) { this.Z = z; }

        #endregion Constructors


        /// <summary>
        /// Z-Koordinate
        /// </summary>
        public readonly double Z;

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Point1 Create(double z) => new Point1(z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Point1 Create(Vector1 vector) => new Point1(vector.Z);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="z"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static Point1 Create(IReadOnlyList<double> z, int startIndex = 0) => new Point1(z[startIndex]);

        /// <summary>
        ///  Differenzvektor zwischen zwei Punkten
        /// </summary>
        /// <param name="a"> 1. Punkt </param>
        /// <param name="b"> 2. Punkt </param>
        /// <returns> Differenzvektor </returns>
        public static Vector1 operator -(Point1 a, Point1 b) => Vector1.Create(a.Z - b.Z);

        /// <summary>
        ///  Translation eines Punktes (Subtraktion)
        /// </summary>
        /// <param name="a"> Punkt </param>
        /// <param name="b"> Translationsvektor </param>
        /// <returns> Translierter Punkt </returns>
        public static Point1 operator -(Point1 a, Vector1 b) => new Point1(a.Z - b.Z);

        /// <summary>
        ///  Translation eines Punktes (Addition)
        /// </summary>
        /// <param name="a"> Punkt </param>
        /// <param name="b"> Translationsvektor </param>
        /// <returns> Translierter Punkt </returns>
        public static Point1 operator +(Point1 a, Vector1 b) => new Point1(a.Z + b.Z);

        /// <summary>
        ///  Mittelpunkt zweier Punkte
        /// </summary>
        /// <param name="a"> 1. Punkt </param>
        /// <param name="b"> 2. Punkt </param>
        /// <returns> Mittelpunkt </returns>
        public static Point1 Centroid(Point1 a, Point1 b) => new Point1((a.Z + b.Z) / 2.0);

        /// <summary>
        ///  Berechnet den Schwerpunkt einer Punktliste, bei Bedarf die Vektoren vom Schwerpunkt zu
        ///  den Punkten
        /// </summary>
        /// <param name="points">      Punktliste </param>
        /// <param name="fillVectors"> Vektoren berechnen </param>
        /// <param name="vectors">     Vektoren </param>
        /// <returns> Schwerpunkt </returns>
        public static Point1 Centroid(IList<Point1> points, bool fillVectors, out IList<Vector1> vectors)
        {
            double z = 0.0;
            foreach(var point in points)
            {
                z += point.Z;
            }
            z /= points.Count;
            vectors = fillVectors ? points.Select(p => Vector1.Create(p.Z - z)).ToList() : null;
            return new Point1(z);
        }

        /// <summary>
        /// Geometrischer Vergleich
        /// </summary>
        /// <param name="other"></param>
        /// <param name="minDist">kleinstmöglicher Abstand</param>
        /// <returns></returns>
        public bool Coincident(Point1 other, double minDist = MINDIST)
        {
            double dz = other.Z - this.Z;
            return IsNearlyZeroSquared(dz * dz, minDist * minDist);
        }

        #endregion Methods
    }
}