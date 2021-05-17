using BimGisCad.Representation.Geometry.Linear;
using System;

using static BimGisCad.Representation.Geometry.Elementary.Common;

namespace BimGisCad.Representation.Geometry.Elementary
{
    /// <summary>
    ///  3-Dimensionale Gerade (ungerichtet!)
    /// </summary>
    public class Line3 : ILinear3//, IEquatable<Line3>
    {
        #region Constructors

        /// <summary>
        ///  Konstruktor mit Ursprung und Richtung
        /// </summary>
        /// <param name="position">  Ursprung </param>
        /// <param name="direction"> Richtung </param>
        private Line3(Point3 position, Direction3 direction)
        {
            this.Position = position;
            this.Direction = direction;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public Point3 Position { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Direction3 Direction { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double[][] PointEqu
        {
            get
            {
                Direction3 dirY, dirZ;
                Direction3.Perp(this.Direction, out dirY, out dirZ);
                return new[]
                {
                    new[]{ dirY.X, dirY.Y, dirY.Z, Point3.Dot(dirY, this.Position) },
                    new[]{ dirZ.X, dirZ.Y, dirZ.Z, Point3.Dot(dirZ, this.Position) }
                };
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        ///  Erzeugt 3D-Gerade
        /// </summary>
        /// <param name="position"> Punkt auf Gerade </param>
        /// <param name="dir">      Richtung </param>
        public static Line3 Create(Point3 position, Direction3 dir) => new Line3(position, dir);

        /// <summary>
        ///  Erzeugt 3D-Gerade
        /// </summary>
        /// <param name="position"> Punkt auf Gerade </param>
        /// <param name="vec">      Vektor </param>
        /// <param name="line"></param>
        public static bool Create(Point3 position, Vector3 vec, out Line3 line)
        {
            double norm = Vector3.Norm(vec);
            if(IsValidNorm(norm))
            {
                var dir = Direction3.Create(vec.X, vec.Y, vec.Z, norm);
                line = new Line3(position, dir);
                return true;
            }
            else
            {
                line = null;
                return false;
            }
        }

        /// <summary>
        ///  Erzeugt 3D-Gerade
        /// </summary>
        /// <param name="p1"> 1. Punkt auf Gerade </param>
        /// <param name="p2"> 2. Punkt auf Gerade </param>
        /// <param name="line"></param>
        public static bool Create(Point3 p1, Point3 p2, out Line3 line)
        {
            var vec = p2 - p1;
            double norm = Vector3.Norm(vec);
            if(IsValidNorm(norm))
            {
                var dir = Direction3.Create(vec.X, vec.Y, vec.Z, norm);
                line = new Line3(p1, dir);
                return true;
            }
            else
            {
                line = null;
                return false;
            }
            
        }

        /// <summary>
        ///  Schnittgerade zweier Ebenen, falls nicht parallel
        /// </summary>
        /// <param name="a"> 1. Ebene </param>
        /// <param name="b"> 2. Ebene </param>
        /// <param name="line"></param>
        public static bool Create(Plane a, Plane b, out Line3 line)
        {
            line = null;

            var ba = Direction3.Cross(b.Normal, a.Normal);
            double norm = Vector3.Norm(ba);
            if(norm < TRIGTOL)
            { return false; }

            var dir = ba / norm;
            var ad = Direction3.Cross(a.Normal, dir);
            var db = Direction3.Cross(dir, b.Normal);
            double den = Vector3.Dot(dir, ba);
            var pos = ((b.D * ad) + (a.D * db)) / den;
            line = new Line3(Point3.Create(pos), Direction3.Create(dir));

            return true;
        }

        /// <summary>
        ///  Prüft ob Punkt auf der Geraden liegt
        /// </summary>
        /// <param name="line">   </param>
        /// <param name="point">  </param>
        /// <returns>  </returns>
        public static bool Touches(Line3 line, Point3 point)
        {
            double dist = Vector3.Norm(Direction3.Cross(line.Direction, point - line.Position));
            return IsNearlyZero(dist);
        }

        /// <summary>
        ///  Berechnet Punkt aus Strecke auf Gerade
        /// </summary>
        /// <param name="line">     Gerade </param>
        /// <param name="distance"> Strecke auf Gerade </param>
        /// <returns> Punkt </returns>
        public static Point3 PointOnLine(Line3 line, double distance) => line.Position + (distance * line.Direction);

        /// <summary>
        ///  Berechnet Punkt aus Abstandspunkt auf Gerade
        /// </summary>
        /// <param name="line">  Gerade </param>
        /// <param name="point"> Abstandspunkt </param>
        /// <returns> Punkt </returns>
        public static Point3 PointOnLine(Line3 line, Point1 point) => line.Position + (point.Z * line.Direction);

        /// <summary>
        ///  Positiver Abstand eines Punktes zur Geraden
        /// </summary>
        /// <param name="line">  Gerade </param>
        /// <param name="point"> Punkt </param>
        /// <returns> Positiver Abstand zu Gerade </returns>
        public static double DistanceToLine(Line3 line, Point3 point) => Vector3.Norm(Direction3.Cross(line.Direction, point - line.Position));

        /// <summary>
        ///  Abstand des Lotfusspunkts eines Punktes zum Punkt auf der Geraden (negativer Wert
        ///  bedeutet Lotfusspunkt befindet sich vor Position)
        /// </summary>
        /// <param name="line">  Gerade </param>
        /// <param name="point"> Punkt </param>
        /// <returns> Lotfusspunkt-Abstand zu Position </returns>
        public static double PerpendicularDistanceToPosition(Line3 line, Point3 point) => Direction3.Dot(point - line.Position, line.Direction);

        /// <summary>
        ///  Kuerzester Abstand zwischen 2 Geraden (ohne Vorzeichen)
        /// </summary>
        /// <param name="a"> 1. Gerade </param>
        /// <param name="b"> 2. Gerade </param>
        /// <returns> Positiver Abstand </returns>
        public static double LineToLineDistance(Line3 a, Line3 b)
        {
            var n = Direction3.Cross(a.Direction, b.Direction);
            double sin = Vector3.Norm(n);
            var diff = b.Position - a.Position;
            if(sin < TRIGTOL)
            // parallel
            { return Vector3.Norm(Direction3.Cross(a.Direction, diff)); }
            return Math.Abs(Vector3.Dot(n, a.Position - b.Position) / sin);
        }

        /// <summary>
        ///  Geometrischer Vergleich
        /// </summary>
        /// <param name="other">  </param>
        /// <param name="mindist"></param>
        /// <returns>  </returns>
        public bool Coincident(Line3 other, double mindist = MINDIST) => other.Position.Coincident(this.Position, mindist) && Direction3.AreCollinear(other.Direction, this.Direction);

        #endregion Methods
    }
}