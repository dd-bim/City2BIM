namespace City2BIM.RevitCommands.City2BIM
{
    public static class City2BIM_prop
    {
        //user-defined decision properties        
        private static bool isServerRequest = true;         //default value: true, Server will be called
        private static bool isGeodeticSystem = true;        //default value: true, YXZ coordinate order in geodata preferred

        //user-defined source properties   
        private static string fileUrl = "";
        private static string serverUrl = "https://hosting.virtualcitywfs.de/deutschland_viewer/wfs";

        //user-defined extent properties
        private static double extent = 300.0;
        private static double[] serverCoord = new double[2] { GeoRefSettings.WgsCoord[1], GeoRefSettings.WgsCoord[0] } ;

        public static double Extent { get => extent; set => extent = value; }
        public static bool IsServerRequest { get => isServerRequest; set => isServerRequest = value; }
        public static string ServerUrl { get => serverUrl; set => serverUrl = value; }
        public static string FileUrl { get => fileUrl; set => fileUrl = value; }
        public static bool IsGeodeticSystem { get => isGeodeticSystem; set => isGeodeticSystem = value; }
        public static double[] ServerCoord { get => serverCoord; set => serverCoord = value; }
    }
}
