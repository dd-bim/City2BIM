using BimGisCad.Representation.Geometry.Linear;
using System;
using System.Collections.Generic;
using System.Text;
using static BimGisCad.Representation.Geometry.Elementary.Common;

namespace BimGisCad.Representation.Geometry.Elementary
{
    /// <summary>
    ///  3-Dimensionale Ebene (ungerichtet!)
    /// </summary>
    public class Plane : Axis2Placement3D, ILinear3//, IEquatable<Plane>
    {
        private Point3 position;

        #region Constructors

        private Plane(Point3 position, Direction3 axis, Direction3? refDirection = null) : base((Vector3)position, axis, refDirection ?? Direction3.Perp(axis), false)
        { this.position = position; }


        #endregion Constructors

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public Point3 Position
        {
            get
            {
                return this.position;
            }
            set
            {
                this.position = value;
                this.Location = (Vector3)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Direction3 Normal => this.Axis;

        /// <summary>
        /// 
        /// </summary>
        public double D => -Point3.Dot(this.Position, this.Axis);

        /// <summary>
        /// 
        /// </summary>
        public double[][] PointEqu => new[] { new[] { this.Axis.X, this.Axis.Y, this.Axis.Z, -this.D } };


        #endregion Properties

        #region Methods

        /// <summary>
        /// Prüft ob Punkt auf der Ebene liegt
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="point"></param>
        /// <param name="mindist"></param>
        /// <returns></returns>
        public static bool Touches(Plane plane, Point3 point, double mindist = MINDIST)
        {
            double dist = (plane.Axis.X * (point.X - plane.Position.X)) + (plane.Axis.Y * (point.Y - plane.Position.Y)) + (plane.Axis.Z * (point.Z - plane.Position.Z));
            return IsNearlyZero(dist, mindist);
        }

        /// <summary>
        ///  Abstand eines Punktes zur Ebene
        /// </summary>
        /// <param name="plane"> Plane </param>
        /// <param name="point"> Punkt </param>
        /// <returns> Abstand zu Ebene </returns>
        public static double DistanceToPlane(Plane plane, Point3 point) => Direction3.Dot(point - plane.Position, plane.Axis);

        /// <summary>
        /// Ebene durch durch Punkte, wenn nicht kollinear, 
        /// Normale der Ebene zeigt in Richtung der Halbebene in der der Umring gegen den Uhrzeigersinn verläuft
        /// der Ursprung ist der Schwerpunkt oder Punkt a (je nach originAtA)
        /// Die lokale X-Achse, geht von a nach b (originA = true) oder vom Schwerpunkt nach a
        /// </summary>
        /// <param name="originA">Ursprung bei Punkt a, sonst Schwerpunkt</param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="plane"></param>
         public static bool Create(bool originA, Point3 a, Point3 b, Point3 c, out Plane plane)
        {
            var ab = b - a;
            var vnormal = Vector3.Cross(ab, c - a);
            double norm = Vector3.Norm(vnormal);
            bool valid = IsValidNormSquared(norm); //Quadrat weil Norm der Fläche entspricht
            var ctr = originA ? a: Point3.Centroid(a, b, c);
            plane = valid ? new Plane(ctr, 
                Direction3.Create(vnormal.X, vnormal.Y, vnormal.Z, norm), 
                Direction3.Create(originA ? ab : a - ctr)) : null;
            return valid;
        }

        /// <summary>
        /// Ebene aus Punkt und Normale, es wird eine willkürliche lokale X-Achse erstellt
        /// </summary>
        /// <param name="origin">Ebenenursprung</param>
        /// <param name="normal">Ebenennormale (wird normiert)</param>
        public static Plane Create(Point3 origin, Direction3 normal)
        {
            return new Plane(origin, normal);
        }

        /// <summary>
        /// Ebene durch durch Polygon aus Punkten, wenn nicht kollinear und mindestens 3, 
        /// Normale der Ebene zeigt in Richtung der Halbebene in der der Umring gegen den Uhrzeigersinn verläuft
        /// Der Schwerpunkt ist der Flächenschwerpunkt oder der erste Punkt
        /// Die lokale X-Achse zeigt vom ersten zum zweiten Punkt (originFirst = true) oder vom Schwerpunkt nach Punkt 1
        /// </summary>
        /// <param name="originFirst"></param>
        /// <param name="points">Punkte</param>
        /// <param name="plane"></param>
        /// <param name="vv">Quadratsumme der Verbesserungen</param>
        public static bool Create(bool originFirst, IList<Point3> points, out Plane plane, out double vv)
        {
            vv = 0.0;
            plane = null;

            if(points.Count < 3)
            { return false; }

            if(points.Count == 3)
            { return Create(originFirst, points[0], points[1], points[2], out plane); }

            // Schwerpunkt und reduzierte Punkte
            IList<Vector3> vecs;
            var mp = Point3.Centroid(points, out vecs);

            // Normale aufsummieren
            var vnormal = Vector3.Zero;
            var crosses = new Vector3[points.Count];
            for(int i = 0, j = crosses.Length - 1; i < crosses.Length; j = i, i++)
            {
                crosses[i] = Vector3.Cross(vecs[j], vecs[i]);
                vnormal += crosses[i];
            }
            double norm = Vector3.Norm(vnormal);
            if(!IsValidNormSquared(norm)) //Fläche
            { return false; }
            var normal = Direction3.Create(vnormal.X, vnormal.Y, vnormal.Z, norm);

            // Flächenschwerpunkt und Verbesserungen rechnen
            var sp = Vector3.Zero;
            for(int i = 0, j = crosses.Length - 1; i < crosses.Length; j = i, i++)
            {
                sp += (vecs[j] + vecs[i]) * Direction3.Dot(crosses[i], normal);
                double v = Point3.Dot(points[i], normal);
                vv += v * v;
            }
            mp += sp / (3.0 * norm);
            plane = new Plane(originFirst ? points[0] : mp, normal, Direction3.Create(originFirst ? points[1] - points[0] : points[0] - mp));

            return true;
        }

        /// <summary>
        /// Geometrischer Vergleich, (kein Vergleich der lokalen X Achse!)
        /// </summary>
        /// <param name="other"></param>
        /// <param name="mindist"></param>
        /// <returns></returns>
        public bool Coincident(Plane other, double mindist = MINDIST) => Direction3.AreCollinear(other.Axis, this.Axis) && Touches(other, this.Position, mindist) && Touches(this, other.Position, mindist);


        /// <summary>
        /// Projiziert übergeordnete Richtung in das System der Ebene(2D)
        /// </summary>
        /// <param name="system"></param>
        /// <param name="reference"></param>
       public static Direction2 ToPlaneLocal(Axis2Placement3D system, Direction3 reference)
        {
            return Direction2.Create(Direction3.Dot(system.RefDirection, reference), Direction3.Dot(system.YAxis, reference), null);
        }

        /// <summary>
        /// Projiziert übergeordneten Vektor in das System der Ebene(2D)
        /// </summary>
        /// <param name="system"></param>
        /// <param name="reference"></param>
        public static Vector2 ToPlaneLocal(Axis2Placement3D system, Vector3 reference)
        {
            return Vector2.Create(Direction3.Dot(system.RefDirection, reference), Direction3.Dot(system.YAxis, reference));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="system"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static Point2 ToPlaneLocal(Axis2Placement3D system, Point3 reference)
        {
            var point = reference - system.Location;
            return Point2.Create(Point3.Dot(system.RefDirection, point), Point3.Dot(system.YAxis, point));
        }

        /// <summary>
        /// Schnittpunkt Ebene / Gerade
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public static Point3? Intersection(Plane plane, Line3 line)
        {
            double d = Direction3.Dot(plane.Normal, line.Direction);
 
            // kein Schnitt (parallel)
            if(d < TRIGTOL) {
                return null;
            }

            return Line3.PointOnLine(line, Direction3.Dot(plane.Normal, plane.Position - line.Position) / d);
        }

        #endregion Methods
    }
}