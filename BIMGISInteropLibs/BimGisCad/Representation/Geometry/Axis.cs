namespace BimGisCad.Representation.Geometry
{
    /// <summary>
    /// Achsen Aufzählung
    /// </summary>
    public enum Axis
    {
        /// <summary>
        /// X Achse
        /// </summary>
        X = 0,

        /// <summary>
        /// Y-Achse
        /// </summary>
        Y,

        /// <summary>
        /// Z-Achse
        /// </summary>
        Z
    }

    /// <summary>
    /// Aufzählung möglicher Ebenen eines Koordinatensystems
    /// </summary>
    public enum AxisPlane
    {
        /// <summary>
        /// Ebene zwischen X und Y Achse
        /// </summary>
        XY,

        /// <summary>
        /// Ebene zwischen Y und Z Achse
        /// </summary>
        YZ,

        /// <summary>
        /// Ebene zwischen Z und X Achse
        /// </summary>
        ZX
    }
}