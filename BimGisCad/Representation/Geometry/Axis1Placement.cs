namespace BimGisCad.Representation.Geometry
{
    ///// <summary>
    /////  Lokales Koordinatensystem in 1D Kontext  (nicht Implementiert)
    ///// </summary>
    //public class Axis1Placement
    //{
        //#region Constructors

        //private Axis1Placement(double tz)
        //{
        //    this.Translation = Vector1.Create(tz);
        //}

        //#endregion Constructors

        //#region Properties

        //public Dimensions Dim => Dimensions._1D;

        //public Vector1 Translation { get; }

        //#endregion Properties

        //#region Methods

        //public static void ToLocal(Axis1Placement system, Point1 global, out Point1 local) => local = global - system.Translation;

        //public static Point1 ToGlobal(Axis1Placement system, Point1 local, Point1 global) => local + system.Translation;

        //public static Axis1Placement Create() => new Axis1Placement(0.0);

        //public static Axis1Placement Create(double tz) => new Axis1Placement(tz);

        //public static Axis1Placement Create(Vector1 translation) => new Axis1Placement(translation.Z);

        ///// <summary>
        ///// Kombiniert zwei Systeme zu einem, Reihenfolge vom Kleinen ins Große (Ergebnis des kombinierten Systems ToGlobal, entspricht sys2.ToGlobal(sys1.ToGlobal(x)))
        ///// </summary>
        ///// <param name="sys1"></param>
        ///// <param name="sys2"></param>
        ///// <returns></returns>
        //public static Axis1Placement Combine(Axis1Placement sys1, Axis1Placement sys2) => new Axis1Placement(sys1.Translation.Z + sys2.Translation.Z);

        //#endregion Methods
    //}
}