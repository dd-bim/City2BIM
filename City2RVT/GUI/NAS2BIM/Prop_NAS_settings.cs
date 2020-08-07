using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace City2RVT.GUI
{
    public static class Prop_NAS_settings
    {
        //copy from City2BIM_prop --> contains therefore additionally functionality for server request and codelist transformation
        //commented variables may be used for later implementations
        //added bool for mapping of subregions to existent TopoSurface 

        //user-defined decision properties        
        //private static bool isServerRequest = true;         //default value: true, Server will be called
        private static bool isGeodeticSystem = true;        //default value: true, YXZ coordinate order in geodata preferred
        //private static Codelist codelistName = Codelist.none;
        //private static bool saveServerResponse = false;

        //user-defined source properties   
        private static string fileUrl = "";
        //private static string serverUrl = "https://hosting.virtualcitywfs.de/deutschland_viewer/wfs"; //only CityGml, if enhanced --> change URL
        //private static string pathResponse = System.Environment.CurrentDirectory;

        private static string dtmFile = "";

        private static System.Collections.IList selectedLayer = default(System.Collections.IList);
        private static List<string> paramList = default(List<string>);
        private static System.Collections.IList selectedParams = default(System.Collections.IList);
        private static System.Collections.IList selectedParamsPlusLayer = default(System.Collections.IList);
        private static string selectedElement = default(string);
        private static string selectedPset = default(string);
        private static int chosenElement = default(int);
        private static string selectedId = default(string);

        //user-defined extent properties
        //private static double extent = 300.0;
        //private static double[] serverCoord = new double[2] { GeoRefSettings.WgsCoord[1], GeoRefSettings.WgsCoord[0] };

        //mapping on TopoSurface?
        private static bool drapeBldgsOnTopo = false;
        private static bool drapeUsageOnTopo = false;
        private static bool drapeParcelsOnTopo = false;
        private static bool zOffsetIsChecked = false;

        //public static double Extent { get => extent; set => extent = value; }
        //public static bool IsServerRequest { get => isServerRequest; set => isServerRequest = value; }
        //public static string ServerUrl { get => serverUrl; set => serverUrl = value; }
        public static string FileUrl { get => fileUrl; set => fileUrl = value; }
        public static string DtmFile { get => dtmFile; set => dtmFile = value; }
        //public static string SelectedLayer { get => SelectedLayer; set => SelectedLayer = value; }
        public static System.Collections.IList SelectedLayer { get => selectedLayer; set => selectedLayer = value; }
        public static bool ZOffsetIsChecked { get => zOffsetIsChecked; set => zOffsetIsChecked = value; }
        public static List<string> ParamList { get => paramList; set => paramList = value; }
        public static int ChosenElement { get => chosenElement; set => chosenElement = value; }
        public static System.Collections.IList SelectedParams { get => selectedParams; set => selectedParams = value; }
        public static System.Collections.IList SelectedParamsPlusLayer { get => selectedParamsPlusLayer; set => selectedParamsPlusLayer = value; }
        public static string SelectedElement { get => selectedElement; set => selectedElement = value; }
        public static string SelectedPset { get => selectedPset; set => selectedPset = value; }

        public static string SelectedId { get => selectedId; set => selectedId = value; }


        public static bool IsGeodeticSystem { get => isGeodeticSystem; set => isGeodeticSystem = value; }
        public static bool DrapeBldgsOnTopo { get => drapeBldgsOnTopo; set => drapeBldgsOnTopo = value; }
        public static bool DrapeUsageOnTopo { get => drapeUsageOnTopo; set => drapeUsageOnTopo = value; }
        public static bool DrapeParcelsOnTopo { get => drapeParcelsOnTopo; set => drapeParcelsOnTopo = value; }
        
        //public static double[] ServerCoord { get => serverCoord; set => serverCoord = value; }
        //public static Codelist CodelistName { get => codelistName; set => codelistName = value; }
        //public static bool SaveServerResponse { get => saveServerResponse; set => saveServerResponse = value; }
        //public static string PathResponse { get => pathResponse; set => pathResponse = value; }
    }

    //public enum Codelist { none, adv, sig3d }; //check for codes in ALKIS data and may implement
}



