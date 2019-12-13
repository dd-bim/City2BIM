namespace City2BIM.RevitCommands.City2BIM
{
    public static class City2BIM_prop
    {
        //user-defined decision properties        
        private static bool isServerRequest = true;         //default value: true, Server will be called
        private static bool isGeodeticSystem = true;        //default value: true, YXZ coordinate order in geodata preferred
        private static Codelist codelistName = Codelist.none;
        private static bool saveServerResponse = false;

        //user-defined source properties   
        private static string fileUrl = "";
        private static string serverUrl = "https://hosting.virtualcitywfs.de/deutschland_viewer/wfs";
        private static string pathResponse = System.Environment.CurrentDirectory;

        //user-defined extent properties
        private static double extent = 300.0;
        private static double[] serverCoord = new double[2] { GeoRefSettings.WgsCoord[1], GeoRefSettings.WgsCoord[0] } ;

        //user-defined geometry quality values
        private static double equalPtSq = 0.000001; //1mm^2!
        private static double equalPlSq = 0.0025; //5cm^2!
        private static double maxDevPlaneCutSq = 0.0025; //5cm^2!       //will try to fulfill requirements but in some cases failed, see Solid calculation

        public static double Extent { get => extent; set => extent = value; }
        public static bool IsServerRequest { get => isServerRequest; set => isServerRequest = value; }
        public static string ServerUrl { get => serverUrl; set => serverUrl = value; }
        public static string FileUrl { get => fileUrl; set => fileUrl = value; }
        public static bool IsGeodeticSystem { get => isGeodeticSystem; set => isGeodeticSystem = value; }
        public static double[] ServerCoord { get => serverCoord; set => serverCoord = value; }
        public static Codelist CodelistName { get => codelistName; set => codelistName = value; }
        public static bool SaveServerResponse { get => saveServerResponse; set => saveServerResponse = value; }
        public static string PathResponse { get => pathResponse; set => pathResponse = value; }

        public static double MaxDevPlaneCutSq { get => maxDevPlaneCutSq; set => maxDevPlaneCutSq = value; }
        public static double EqualPtSq { get => equalPtSq; set => equalPtSq = value; }
        public static double EqualPlSq { get => equalPlSq; set => equalPlSq = value; }
    }

    public enum Codelist { none, adv, sig3d};
}
