using City2BIM.GmlRep;
using System;
using System.IO;

namespace City2RVT.GUI
{
    public class Prop_CityGML_settings
    {
        //user-defined decision properties        
        private static bool isServerRequest = true;         //default value: true, Server will be called
        private static bool isGeodeticSystem = true;        //default value: true, YXZ coordinate order in geodata preferred
        private static CityGml_Codelist.Codelist codelistName = CityGml_Codelist.Codelist.none;
        private static bool saveServerResponse = false;

        //user-defined source properties   
        private static string fileUrl = "";
        private static string serverUrl = "https://hosting.virtualcitywfs.de/deutschland_viewer/wfs";
        private static string pathResponse = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "City2BIM");

        //user-defined extent properties
        private static double extent = 300.0;
        private static double[] serverCoord = new double[2] { Prop_GeoRefSettings.WgsCoord[1], Prop_GeoRefSettings.WgsCoord[0] } ;

        public static double Extent { get => extent; set => extent = value; }
        public static bool IsServerRequest { get => isServerRequest; set => isServerRequest = value; }
        public static string ServerUrl { get => serverUrl; set => serverUrl = value; }
        public static string FileUrl { get => fileUrl; set => fileUrl = value; }
        public static bool IsGeodeticSystem { get => isGeodeticSystem; set => isGeodeticSystem = value; }
        public static double[] ServerCoord { get => serverCoord; set => serverCoord = value; }
        public static CityGml_Codelist.Codelist CodelistName { get => codelistName; set => codelistName = value; }
        public static bool SaveServerResponse { get => saveServerResponse; set => saveServerResponse = value; }
        public static string PathResponse { get => pathResponse; set => pathResponse = value; }
    }

}
