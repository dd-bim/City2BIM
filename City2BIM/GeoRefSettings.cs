using Autodesk.Revit.DB;


namespace City2BIM
{
    public static class GeoRefSettings
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
            WgsCoord[0] = siteLoc.Latitude * Prop.radToDeg;
            WgsCoord[1] = siteLoc.Longitude * Prop.radToDeg;

            //project location
            ProjCoord[1] = pbpLoc.EastWest * Prop.feetToM;
            ProjCoord[0] = pbpLoc.NorthSouth * Prop.feetToM;
            ProjElevation = pbpLoc.Elevation * Prop.feetToM;

            //True North
            ProjAngle = pbpLoc.Angle * Prop.radToDeg;

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
