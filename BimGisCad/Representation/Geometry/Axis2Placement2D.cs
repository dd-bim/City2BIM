using BimGisCad.Representation.Geometry.Elementary;

namespace BimGisCad.Representation.Geometry
{
    /// <summary>
    ///  Lokales Koordinatensystem in 2D Kontext
    /// </summary>
    public class Axis2Placement2D
    {
        #region Constructors

        /// <summary>
        /// Konstruktor aus Referenzpunkt und Referenzrichtung
        /// </summary>
        /// <param name="location"></param>
        /// <param name="refDirection"></param>
        protected Axis2Placement2D(Vector2? location = null, Direction2? refDirection = null)
        {
            this.Location = location ?? Vector2.Zero;
            this.RefDirection = refDirection ?? Direction2.UnitX;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Referenzpunkt
        /// </summary>
        public Vector2 Location { get; set; }

        /// <summary>
        /// Referenzrichtung
        /// </summary>
        public Direction2 RefDirection { get; set; }

        /// <summary>
        /// Y-Achse
        /// </summary>
        public Direction2 YAxis => Direction2.Perp(this.RefDirection);

        #endregion Properties

        #region Methods

        /// <summary>
        /// Rotiert Übergeordneten Richtung in das System
        /// </summary>
        /// <param name="system"></param>
        /// <param name="reference"></param>
        public static Direction2 ToLocal(Axis2Placement2D system, Direction2 reference) => reference - system.RefDirection;

        /// <summary>
        /// Rotiert Übergeordneten Vektor in das System
        /// </summary>
        /// <param name="system"></param>
        /// <param name="reference"></param>
        public static Vector2 ToLocal(Axis2Placement2D system, Vector2 reference) => reference - system.RefDirection;

        /// <summary>
        /// Transformiert Übergeordneten Punkt in das System
        /// </summary>
        /// <param name="system"></param>
        /// <param name="reference"></param>
        public static Point2 ToLocal(Axis2Placement2D system, Point2 reference) => (reference - system.Location) - system.RefDirection;


        /// <summary>
        /// Rotiert Lokale Richtung in das Übergeordnete System
        /// </summary>
        /// <param name="system"></param>
        /// <param name="local"></param>
        public static Direction2 ToReference( Axis2Placement2D system,  Direction2 local) => local + system.RefDirection;

        /// <summary>
        /// Rotiert Lokalen Vektor in das Übergeordnete System
        /// </summary>
        /// <param name="system"></param>
        /// <param name="local"></param>
        public static Vector2 ToReference( Axis2Placement2D system,  Vector2 local) => local + system.RefDirection;

        /// <summary>
        /// Transformiert Lokalen Punkt in das Übergeordnete System
        /// </summary>
        /// <param name="system"></param>
        /// <param name="local"></param>
        public static Point2 ToReference( Axis2Placement2D system,  Point2 local) => system.Location + (local + system.RefDirection);

        /// <summary>
        /// Builder mit Referenzpunkt 0,0
        /// </summary>
        /// <param name="refDirection"></param>
        /// <returns></returns>
        public static Axis2Placement2D Create(Direction2? refDirection = null) => new Axis2Placement2D(null, refDirection);

        /// <summary>
        /// Builder
        /// </summary>
        /// <param name="location"></param>
        /// <param name="refDirection"></param>
        /// <returns></returns>
        public static Axis2Placement2D Create(Vector2 location, Direction2? refDirection = null) => new Axis2Placement2D(location, refDirection);

        /// <summary>
        /// Kombiniert mindestens zwei Systeme zu einem, Reihenfolge vom Kleinen ins Große (Ergebnis des kombinierten Systems ToGlobal, entspricht sys2.ToGlobal(sys1.ToGlobal(x)))
        /// </summary>
        public static Axis2Placement2D Combine(params Axis2Placement2D[] systems)
        {
            var d = systems[0].RefDirection;
            var t = systems[0].Location;

            for(int i = 1; i < systems.Length; i++)
            {
                d += systems[i].RefDirection;
                t = systems[i].Location + (t + systems[i].RefDirection);
            }

            return new Axis2Placement2D(t, d);

            //var dir = sys1.XAxis + sys2.XAxis;
            //var tra = sys2.Translation + Direction2.RotateCol(sys2.XAxis, sys1.Translation);
            //return new System2(tra, dir);
        }

        #endregion Methods
    }
}