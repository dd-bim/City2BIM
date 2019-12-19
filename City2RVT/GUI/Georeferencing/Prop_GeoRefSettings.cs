using Autodesk.Revit.DB;

namespace City2RVT.GUI
{
    public static class Prop_GeoRefSettings
    {
        private static double[] wgsCoord = new double[2];
        private static double[] projCoord = new double[2];
        private static double projAngle;
        private static double projScale = 1.0;
        private static bool isInitialized = false;      //default value: false, Settings must be initialized before importing geodata
        private static bool isGeoreferenced = false;    //default value: false, indicates whether the Site.Location matches the Project.Position (true, if setted in LoGeoRef window)
        private static double projElevation;
        private static string epsg = "";


        public static double[] WgsCoord { get => wgsCoord; set => wgsCoord = value; }
        public static double[] ProjCoord { get => projCoord; set => projCoord = value; }
        public static bool IsInitialized { get => isInitialized; }
        public static double ProjElevation { get => projElevation; set => projElevation = value; }
        public static string Epsg { get => epsg; set => epsg = value; }
        public static bool IsGeoreferenced { get => isGeoreferenced; set => isGeoreferenced = value; }
        public static double ProjAngle { get => projAngle; set => projAngle = value; }
        public static double ProjScale { get => projScale; set => projScale = value; }

        public static void SetInitialSettings(Document doc)
        {
            // equivalent Revit classes
            ProjectPosition pbpLoc = doc.ActiveProjectLocation.GetProjectPosition(XYZ.Zero);
            SiteLocation siteLoc = doc.SiteLocation;
            ProjectInfo geoInfo = doc.ProjectInformation;


            //Site location
            WgsCoord[0] = siteLoc.Latitude * Prop_Revit.radToDeg;
            WgsCoord[1] = siteLoc.Longitude * Prop_Revit.radToDeg;

            //project location
            ProjCoord[1] = pbpLoc.EastWest * Prop_Revit.feetToM;
            ProjCoord[0] = pbpLoc.NorthSouth * Prop_Revit.feetToM;
            ProjElevation = pbpLoc.Elevation * Prop_Revit.feetToM;

            //True North
            ProjAngle = pbpLoc.Angle * Prop_Revit.radToDeg;

            var scale = geoInfo.LookupParameter("Scale");
            if (scale != null)
            {
                ProjScale = scale.AsDouble();
            }

            var epsgRev = geoInfo.LookupParameter("Name");
            if (epsgRev != null)
            {
                Epsg = epsgRev.AsString();
            }

            //EPSG
            if (Epsg == "")
            {
                if (WgsCoord[1] < 12)
                    Epsg = "EPSG:25832";
                else
                    Epsg = "EPSG:25833";
            }
        }
    }
}
