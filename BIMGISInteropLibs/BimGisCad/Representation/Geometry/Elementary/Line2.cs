using BimGisCad.Representation.Geometry.Linear;
using System;
using static BimGisCad.Representation.Geometry.Elementary.Common;

namespace BimGisCad.Representation.Geometry.Elementary
{
    /// <summary>
    ///  2-Dimensionale Gerade (ungerichtet!)
    /// </summary>
    public class Line2 : ILinear2//, IEquatable<Line2>
    {
        #region Constructors

        /// <summary>
        ///  Konstruktor mit Ursprung und Richtung
        /// </summary>
        /// <param name="position">  Ursprung </param>
        /// <param name="direction"> Richtung </param>
        private Line2(Point2 position, Direction2 direction)
        {
            this.Position = position;
            this.Direction = direction;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public Point2 Position { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Direction2 Direction { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double[][] PointEqu => new[] { new[] { -this.Direction.Y, this.Direction.X, Point2.Det(this.Direction, this.Position) } };

        #endregion Properties

        #region Methods

        /// <summary>
        ///  Erzeugt 2D-Gerade
        /// </summary>
        /// <param name="position"> Punkt auf Gerade </param>
        /// <param name="vec"> Vektor </param>
        /// <param name="line"></param>
        public static bool Create(Point2 position, Vector2 vec, out Line2 line)
        {
            line = null;
            var dir = Direction2.NaN;
            double norm = Vector2.Norm(vec);
                if(IsValidNorm(norm))
                { dir = Direction2.Create(vec.X, vec.Y, norm); }
                else
                { return false; }
            line = new Line2(position, dir);
            return true;
        }

        /// <summary>
        ///  Erzeugt 2D-Gerade
        /// </summary>
        /// <param name="p1"> 1. Punkt auf Gerade </param>
        /// <param name="p2"> 2. Punkt auf Gerade </param>
        internal static Line2 Create(Point2 p1, Point2 p2)
        {
            var vec = p2 - p1;
            double norm = Vector2.Norm(vec);
            var dir = Direction2.Create(vec, norm);
            return new Line2(p1, dir);
        }

        /// <summary>
        ///  Erzeugt 2D-Gerade
        /// </summary>
        /// <param name="position"> Punkt auf Gerade </param>
        /// <param name="dir">Richtung </param>
        public static Line2 Create(Point2 position, Direction2 dir) => new Line2(position, dir);

        /// <summary>
        /// Prüft ob Punkt auf der Geraden liegt
        /// </summary>
        /// <param name="line"></param>
        /// <param name="point"></param>
        /// <param name="minDist"></param>
        /// <returns></returns>
        public static bool Touches(Line2 line, Point2 point, double minDist = MINDIST)
        {
            double dist = Direction2.Det(point - line.Position, line.Direction);
            return IsNearlyZero(dist, minDist);
        }

        /// <summary>
        ///  Berechnet Punkt aus Strecke auf Gerade
        /// </summary>
        /// <param name="line">     Gerade </param>
        /// <param name="distance"> Strecke auf Gerade </param>
        /// <returns> Punkt </returns>
        public static Point2 PointOnLine(Line2 line, double distance) => line.Position + (distance * line.Direction);

        /// <summary>
        ///  Berechnet Punkt aus Abstandspunkt auf Gerade
        /// </summary>
        /// <param name="line">  Gerade </param>
        /// <param name="point"> Abstandspunkt </param>
        /// <returns> Punkt </returns>
        public static Point2 PointOnLine(Line2 line, Point1 point) => line.Position + (point.Z * line.Direction);

        /// <summary>
        ///  Positiver Abstand eines Punktes zur Geraden
        /// </summary>
        /// <param name="line">  Gerade </param>
        /// <param name="point"> Punkt </param>
        /// <returns> Positiver Abstand zu Gerade </returns>
        public static double DistanceToLine(Line2 line, Point2 point) => Math.Abs(Direction2.Det(point - line.Position, line.Direction));


        /// <summary> 
        ///  Lotfusspunkts eines Punktes auf der Geraden 
        /// </summary>
        /// <param name="line"> Gerade </param>
        /// <param name="point"> Punkt </param> 
        /// <returns> Lotfusspunkt </returns>
        public static Point2 PerpendicularFoot(Line2 line, Point2 point) => line.Position + Direction2.Projection(line.Direction, point - line.Position);

        /// <summary>
        /// Geometrischer Vergleich
        /// </summary>
        /// <param name="other"></param>
        /// <param name="mindist"></param>
        /// <returns></returns>
        public bool Coincident(Line2 other, double mindist = MINDIST) => other.Position.Coincident(this.Position, mindist) && Direction2.AreCollinear(other.Direction, this.Direction);

        #endregion Methods
    }
}