
namespace City2BIM
{
    public static class City2BIM_prop
    {
        //user-defined geometry quality values
        private static double equalPtSq = 0.000001; //1mm^2!
        private static double equalPlSq = 0.0025; //5cm^2!
        private static double maxDevPlaneCutSq = 0.0025; //5cm^2!       //will try to fulfill requirements but in some cases failed, see Solid calculation

        public static double MaxDevPlaneCutSq { get => maxDevPlaneCutSq; set => maxDevPlaneCutSq = value; }
        public static double EqualPtSq { get => equalPtSq; set => equalPtSq = value; }
        public static double EqualPlSq { get => equalPlSq; set => equalPlSq = value; }
    }

}
