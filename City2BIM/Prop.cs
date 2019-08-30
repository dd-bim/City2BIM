namespace City2BIM
{
    public static class Prop
    {
        //public static readonly double Distolsq = 0.01; //10cm^2!

        //public static readonly double Distolsq = 0.0009; //3cm^2!

        //public static readonly double Distolsq = 0.0001; //1cm^2!

        /// <summary>
        /// Assumption tolerance for same points
        /// </summary>
        public static readonly double Distolsq = 0.000001; //1mm^2!

        /// <summary>
        /// Tolerance for valid determinant
        /// </summary>
        public static readonly double Determinanttol = 1.0E-4;
        //public static readonly double Determinanttol = 1.0E-3;

        public static GetGeometry.C2BSolid Solid
        {
            get => default(GetGeometry.C2BSolid);
            set
            {
            }
        }
    }
}
