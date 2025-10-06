using System.Collections.Generic;
using BimGisCad.Representation.Geometry;
using BimGisCad.Representation.Geometry.Elementary;

namespace BimGisCad.Representation.Geometry
{
    /// <summary>
    ///  Lokales Koordinatensystem in 2D Kontext 1D Punkte beziehen sich auf Z-Achse 2D Punkte auf XY-Ebene
    /// </summary>
    public class Axis2Placement3D
    {
        #region Fields

        /// <summary>
        ///  Globales Koordinatensystem
        /// </summary>
        public static Axis2Placement3D Standard => new Axis2Placement3D( Vector3.Zero, Direction3.UnitZ,Direction3.UnitX, false);
        private Direction3 refDirection;
        private Direction3 axis;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Konstructor
        /// </summary>
        /// <param name="location"></param>
        /// <param name="axis"></param>
        /// <param name="refDirection"></param>
        /// <param name="reCalcRefDirection"></param>
        protected Axis2Placement3D(Vector3 location, Direction3 axis, Direction3 refDirection, bool reCalcRefDirection = true)
        {
            this.Location = location;
            this.refDirection = reCalcRefDirection ? Direction3.Perp(axis, refDirection) : refDirection;
            this.Axis = axis;
            this.YAxis = Direction3.Create(Direction3.Cross(this.Axis, this.RefDirection));
        }

        //private Axis2Placement3D(Vector3 location, Direction3 xAxis, Direction3 yAxis, Direction3 zAxis)
        //{
        //    this.Location = location;
        //    this.RefDirection = xAxis;
        //    this.YAxis = yAxis;
        //    this.Axis = zAxis;
        //}

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Referenzpunkt
        /// </summary>
        public Vector3 Location { get; set; }

        /// <summary>
        /// Referenzrichtung
        /// </summary>
        public Direction3 RefDirection
        {
            get
            {
                return this.refDirection;
            }
            set
            {
                this.refDirection = Direction3.Perp(this.axis, value);
                Direction3.Create(Direction3.Cross(this.axis, this.refDirection));
            }
        }

        /// <summary>
        /// Y-Achse
        /// </summary>
        public Direction3 YAxis { get; }

        /// <summary>
        /// Z-Achse
        /// </summary>
        public Direction3 Axis
        {
            get
            {
                return this.axis;
            }
            set
            {
                this.axis = value;
                this.refDirection = Direction3.Perp(value, this.refDirection);
                Direction3.Create(Direction3.Cross(this.axis, this.refDirection));
            }
        }

        #endregion Properties

        #region Methods



        /// <summary>
        ///  Erzeugt Standardsystem
        /// </summary>
        /// <returns>  </returns>
        public static Axis2Placement3D Create() => Standard;

        /// <summary>
        ///  Erzeugt Standardsystem mit Translationsvektor
        /// </summary>
        /// <param name="translation"> Translationsvektor </param>
        /// <returns>  </returns>
        public static Axis2Placement3D Create(Vector3 translation) => new Axis2Placement3D(translation, Direction3.UnitZ, Direction3.UnitX, false);

        /// <summary>
        ///  Erzeugt 3D-System aus 2D-System, mit Axis als Z-Achse
        /// </summary>
        /// <param name="placement"> 2D-System </param>
        /// <param name="z"> evtl. Z-Wert Translation</param>
        /// <returns>  </returns>
        public static Axis2Placement3D Create(Axis2Placement2D placement, double z = 0.0)
        {
            var loc = Vector3.Create(placement.Location.X, placement.Location.Y, z);
            var dir = Direction3.Create(placement.RefDirection.X, placement.RefDirection.Y, 0.0);
            return new Axis2Placement3D(loc, Direction3.UnitZ, dir, false);
        }

        /// <summary>
        ///  Erzeugt 3D-System im Ursprung
        /// </summary>
        /// <param name="axis"> Z Achse </param>
        /// <param name="refDirection"> X Achse </param>
        /// <returns>  </returns>
        public static Axis2Placement3D Create(Direction3 axis, Direction3 refDirection) => new Axis2Placement3D(Vector3.Zero, axis, refDirection);

        /// <summary>
        ///  Erzeugt 3D-System
        /// </summary>
        /// <param name="location"> Location</param>
        /// <param name="axis"> Z Achse </param>
        /// <param name="refDirection"> X Achse </param>
        /// <param name="reCalcRefDirection">refDirection rechtwinklig zu axis rechnen (default=true) </param>
        /// <returns>  </returns>
        public static Axis2Placement3D Create(Vector3 location, Direction3 axis, Direction3 refDirection, bool reCalcRefDirection = true) => new Axis2Placement3D(location, axis, refDirection, reCalcRefDirection);

        /// <summary>
        /// Achsen einer Ebene
        /// </summary>
        /// <param name="sys"></param>
        /// <param name="axisPlane"></param>
        /// <param name="axis1"></param>
        /// <param name="axis2"></param>
        public static void GetAxes(Axis2Placement3D sys, AxisPlane axisPlane, out Direction3 axis1, out Direction3 axis2)
        {
            switch(axisPlane)
            {
                case AxisPlane.XY:
                    axis1 = sys.RefDirection;
                    axis2 = sys.YAxis;
                    break;
                case AxisPlane.YZ:
                    axis1 = sys.YAxis;
                    axis2 = sys.Axis;
                    break;
                default:
                    axis1 = sys.Axis;
                    axis2 = sys.RefDirection;
                    break;
            }
        }

        /// <summary>
        /// Projiziert übergeordnete Richtung in das System (2D)
        /// </summary>
        /// <param name="system"></param>
        /// <param name="reference"></param>
        /// <param name="axisPlane"></param>
        public static Direction2 ToLocal(Axis2Placement3D system, Direction3 reference, AxisPlane axisPlane = AxisPlane.XY)
        {
            Direction3 axis1, axis2;
            GetAxes(system, axisPlane, out axis1, out axis2);
            return Direction2.Create(Direction3.Dot(axis1, reference), Direction3.Dot(axis2, reference), null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="system"></param>
        /// <param name="reference"></param>
        /// <param name="axisPlane"></param>
        /// <returns></returns>
        public static Vector2 ToLocal(Axis2Placement3D system, Vector3 reference, AxisPlane axisPlane = AxisPlane.XY)
        {
            Direction3 axis1, axis2;
            GetAxes(system, axisPlane, out axis1, out axis2);
            return Vector2.Create(Direction3.Dot(axis1, reference), Direction3.Dot(axis2, reference));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="system"></param>
        /// <param name="reference"></param>
        /// <param name="axisPlane"></param>
        /// <returns></returns>
        public static Point2 ToLocal(Axis2Placement3D system, Point3 reference, AxisPlane axisPlane = AxisPlane.XY)
        {
            Direction3 axis1, axis2;
            GetAxes(system, axisPlane, out axis1, out axis2);
            var point = reference - system.Location;
            return Point2.Create(Point3.Dot(axis1, point), Point3.Dot(axis2, point));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="system"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static Direction3 ToLocal(Axis2Placement3D system, Direction3 reference) => Direction3.RotateRow(reference, system.RefDirection, system.YAxis, system.Axis);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="system"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static Vector3 ToLocal(Axis2Placement3D system, Vector3 reference) => Direction3.RotateRow(reference, system.RefDirection, system.YAxis, system.Axis);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="system"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static Point3 ToLocal(Axis2Placement3D system, Point3 reference)
        {
            var point = reference - system.Location;
            return Direction3.RotateRow(point, system.RefDirection, system.YAxis, system.Axis);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="system"></param>
        /// <param name="local"></param>
        /// <param name="axisPlane"></param>
        /// <returns></returns>
        public static Direction3 ToReference(Axis2Placement3D system, Direction2 local, AxisPlane axisPlane = AxisPlane.XY)
        {
            Direction3 axis1, axis2;
            GetAxes(system, axisPlane, out axis1, out axis2);
            return Direction3.Create(
            (local.X * axis1.X) + (local.Y * axis2.X),
            (local.X * axis1.Y) + (local.Y * axis2.Y),
            (local.X * axis1.Z) + (local.Y * axis2.Z));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="system"></param>
        /// <param name="local"></param>
        /// <param name="axisPlane"></param>
        /// <returns></returns>
        public static Vector3 ToReference(Axis2Placement3D system, Vector2 local, AxisPlane axisPlane = AxisPlane.XY)
        {
            Direction3 axis1, axis2;
            GetAxes(system, axisPlane, out axis1, out axis2);
            return Vector3.Create(
            (local.X * axis1.X) + (local.Y * axis2.X),
            (local.X * axis1.Y) + (local.Y * axis2.Y),
            (local.X * axis1.Z) + (local.Y * axis2.Z));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="system"></param>
        /// <param name="local"></param>
        /// <param name="axisPlane"></param>
        /// <returns></returns>
        public static Point3 ToReference(Axis2Placement3D system, Point2 local, AxisPlane axisPlane = AxisPlane.XY)
        {
            Direction3 axis1, axis2;
            GetAxes(system, axisPlane, out axis1, out axis2);
            return Point3.Create(
            system.Location.X + (local.X * axis1.X) + (local.Y * axis2.X),
            system.Location.Y + (local.X * axis1.Y) + (local.Y * axis2.Y),
            system.Location.Z + (local.X * axis1.Z) + (local.Y * axis2.Z));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="system"></param>
        /// <param name="local"></param>
        /// <returns></returns>
        public static Direction3 ToReference(Axis2Placement3D system, Direction3 local) => Direction3.RotateCol(system.RefDirection, system.YAxis, system.Axis, local);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="system"></param>
        /// <param name="local"></param>
        /// <returns></returns>
        public static Vector3 ToReference(Axis2Placement3D system, Vector3 local) => Direction3.RotateCol(system.RefDirection, system.YAxis, system.Axis, local);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="system"></param>
        /// <param name="local"></param>
        /// <returns></returns>
        public static Point3 ToReference(Axis2Placement3D system, Point3 local) => Direction3.RotateCol(system.RefDirection, system.YAxis, system.Axis, local) + system.Location;

        /// <summary>
        /// Kombiniert mindestens zwei Systeme zu einem, Reihenfolge vom Kleinen ins Große (Ergebnis des kombinierten Systems ToGlobal, entspricht sys2.ToGlobal(sys1.ToGlobal(x)))
        /// </summary>
        public static Axis2Placement3D Combine(params Axis2Placement3D[] systems) => Combine(systems);
 
        /// <summary>
        /// Kombiniert mindestens zwei Systeme zu einem, Reihenfolge vom Kleinen ins Große (Ergebnis des kombinierten Systems ToGlobal, entspricht sys2.ToGlobal(sys1.ToGlobal(x)))
        /// </summary>
        public static Axis2Placement3D Combine(IReadOnlyList<Axis2Placement3D> systems)
        {
            // Lösung mit Quaternionen, aufwändiger aber numerisch stabiler
            var q = Quaternion.Create(systems[0].RefDirection, systems[0].YAxis, systems[0].Axis);
            var t = systems[0].Location;

            for (int i = 1; i < systems.Count; i++)
            {
                var qi = Quaternion.Create(systems[i].RefDirection, systems[i].YAxis, systems[i].Axis);
                q = qi * q;
                t = systems[i].Location + (qi * t);
            }


            // Lösung mit Achsen
            //var dirX = Direction3.RotateCol(sys2.XAxis, sys2.YAxis, sys2.ZAxis, sys1.XAxis);
            //var dirY = Direction3.RotateCol(sys2.XAxis, sys2.YAxis, sys2.ZAxis, sys1.YAxis);
            //var t12 = sys2.Translation + Direction3.RotateCol(sys2.XAxis, sys2.YAxis, sys2.ZAxis, sys1.Translation);
            // var dirZ = Direction3.Create(Vector3.Cross(dirX, dirY), true);
            return new Axis2Placement3D(t, Quaternion.ZAxis(q), Quaternion.XAxis(q), false);
        }

        public override string ToString() => string.Format("{0}, {1}, {2}", Location.ToString(), Axis.ToString(), refDirection.ToString());

        #endregion Methods
    }
}