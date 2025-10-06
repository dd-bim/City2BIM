using BimGisCad.Representation.Geometry.Elementary;
using System;
using System.Collections.Generic;
using System.Text;
using static BimGisCad.Representation.Geometry.Elementary.Common;

namespace BimGisCad.Representation.Geometry.Composed
{
    /// <summary>
    /// 2D Segment
    /// </summary>
    public struct Segment2
    {
        private double? length;
        #region Constructors

        /// <summary>
        ///  Konstruktor mit 2 Punkten
        /// </summary>
        private Segment2(Point2 start, Point2 end)
        {
            Start = start;
            End = end;
            Direction = end - start;
            length = null;// Vector2.Norm2(Direction);
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Start
        /// </summary>
        public Point2 Start { get; }

        /// <summary>
        /// Richtung
        /// </summary>
        public Vector2 Direction { get; }

        /// <summary>
        /// Ende
        /// </summary>
        public Point2 End { get; }

        /// <summary>
        /// Länge des Segments
        /// </summary>
        public double Length
        {
            get
            {
                if (!length.HasValue) { length = Vector2.Norm(Direction); }
                return length.Value;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        ///  Erzeugt 2D-Segment
        /// </summary>
        public static Segment2 Create(Point2 start, Point2 end)
        {
            return new Segment2(start, end);
        }

        /// <summary>
        /// Lokale Position eines Punktes (x auf Linie (- vor start), y Abstand (- rechts))
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Vector2 LocalPos(Segment2 segment, Point2 point)
        {
            var p = point - segment.Start;
            double x = Vector2.Dot(segment.Direction, p) / segment.Length;
            double y = Vector2.Det(segment.Direction, p) / segment.Length;
            return Vector2.Create(x, y);
        }

        /// <summary>
        /// Berührt Punkt das Segment?
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="point"></param>
        /// <param name="mindist">Mindestabstand != 0</param>
        /// <returns></returns>
        public static bool Touches(Segment2 segment, Point2 point, double mindist = MINDIST)
        {
            var p = point - segment.Start;
            double y = Vector2.Det(segment.Direction, p) / segment.Length;
            if(y > -mindist && y < mindist)
            {
                double x = Vector2.Dot(segment.Direction, p) / segment.Length;
                return x > -mindist && (segment.length - x) > -mindist;
            }
            return false;
        }

        /// <summary>
        /// Querabstand eines Punktes zum Segment(- rechts)
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static double CrossDistance(Segment2 segment, Point2 point)
        {
            return Vector2.Det(segment.Direction, point - segment.Start) / segment.Length;
        }

        /// <summary>
        /// Längsabstand eines Punktes (- davor)
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static double LineDistance(Segment2 segment, Point2 point)
        {
            return Vector2.Dot(segment.Direction, point - segment.Start) / segment.Length;
        }

        /// <summary>
        /// Seite eines Punktes (- rechts)
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static double SideOf(Segment2 segment, Point2 point)
        {
            return Vector2.Det(segment.Direction, point - segment.Start);
        }

        /// <summary>
        /// Prüft ob 2 Segmente sich schneiden, es muss vorher ausgeschlossen werden, dass ein Endpunkt des einen Segments zu nah an das andere Segment kommt
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Intersect(Segment2 a, Segment2 b)
        {
            // Erst prüfen ob sich die Koordinaten überlappen
            double a1 = a.Start.X, 
                a2 = a.End.X, 
                b1 = b.Start.X,
                b2 = b.End.X;
            SortAsc(ref a1, ref a2);
            SortAsc(ref b1, ref b2);
            if(a1 > b2 || a2 < b1) { return false; }
            a1 = a.Start.Y;
            a2 = a.End.Y;
            b1 = b.Start.Y;
            b2 = b.End.Y;
            SortAsc(ref a1, ref a2);
            SortAsc(ref b1, ref b2);
            if (a1 > b2 || a2 < b1) { return false; }
            // Testen ob sich die Punkt jeweils auf der anderen Seite befinden
            if (SideOf(a, b.Start) < 0.0 != SideOf(a, b.End) > 0.0) { return false; }
            if (SideOf(b, a.Start) < 0.0 != SideOf(b, a.End) > 0.0) { return false; }
            // zur Sicherheit
            return true;
        }

        ///<summary>
        /// Schnittpunkt zweier Segmente (null wenn parallel), Achtung keine Prüfung ob Schnittpunkt außerhalb der Segmente!
        ///</summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Point2? Intersection(Segment2 a, Segment2 b)
        {
            double det = Vector2.Det(b.Direction, a.Direction);
            if(det < TRIGTOL && -det < TRIGTOL)
            { // kein Schnitt
                return null;
            }
            double deta = Point2.Det(a.Start, a.End);
            double detb = Point2.Det(b.Start, b.End);
            var vab = Vector2.Create(deta, detb);
            double detx = Vector2.Det(vab, Vector2.Create(a.Direction.X, b.Direction.X));
            double dety = Vector2.Det(vab, Vector2.Create(a.Direction.Y, b.Direction.Y));
            return Point2.Create(detx / det, dety / det);
        }

        ///// <summary>
        ///// Beziehung eines Punktes zum Segment, Neg = rechts, Pos = links des Segments
        ///// </summary>
        ///// <param name="segment"></param>
        ///// <param name="point"></param>
        ///// <param name="axial">Neg = vor Start, Pos = nach End</param>
        ///// <param name="minDist"></param>
        ///// <returns></returns>
        //public static Side Relation(Segment2 segment, Point2 point, out Side axial, double minDist = MINDIST)
        //{
        //    var l = segment.End - segment.Start;
        //    var dl = segment.Length;
        //    var testdist = minDist * dl;
        //    var p = point - segment.Start;
        //    var x = Vector2.Dot(l, p);
        //    var y = Vector2.Det(l, p);
        //    axial = (x - dl * dl) > testdist ? Side.Pos 
        //        : x < -testdist ? Side.Neg : Side.On;
        //    return y > testdist ? Side.Pos 
        //        : y < -testdist ? Side.Neg : Side.On;
        //}

        /// <summary>
        /// Geometrischer Vergleich
        /// </summary>
        /// <param name="other"></param>
        /// <param name="mindist"></param>
        /// <returns></returns>
        public bool Coincident(Segment2 other, double mindist = MINDIST) => other.Start.Coincident(this.Start, mindist) && other.Direction.Coincident(this.Direction, mindist);

        #endregion Methods
    }
}
