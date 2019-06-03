namespace City2BIM
{
    public static class Prop
    {
        public static readonly double Distolsq = 0.0009; //3cm^2!

       //public static readonly double Distolsq = 0.000001; //1mm^2!

        public static readonly double Determinanttol = 1.0E-8;

        public static GetGeometry.Solid Solid
        {
            get => default(GetGeometry.Solid);
            set
            {
            }
        }
    }
}