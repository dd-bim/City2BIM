using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace City2RVT.GUI
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class UI_PlugIn : IExternalApplication
    {
        // Both OnStartup and OnShutdown must be implemented as public method
        // Method "OnStartup" is a Method of IExternalApplication Interface and executes some tasks when Revit starts
        public Result OnStartup(UIControlledApplication application)
        {
            //DB RibbonPanel panel = ribbonPanel(application);
            // Create a push button to trigger a command add it to the ribbon panel.
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            // Add a new Tab
            string tabName = "City2BIM";
            application.CreateRibbonTab(tabName);

            #region Georef panel

            RibbonPanel panel2 = application.CreateRibbonPanel(tabName, "Georeferencing");

            var globePath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var georefRootPath = Path.Combine(globePath, "..", "..");
            var imgGeorefPath = Path.Combine(georefRootPath, "img", "Georef_32px_96dpi.png");
            Uri uriImage = new Uri(imgGeorefPath);
            BitmapImage largeImage = new BitmapImage(uriImage);

            PushButton buttonGeoRef = panel2.AddItem(new PushButtonData("Show Georef information", "Level of Georef",
            thisAssemblyPath, "City2RVT.GUI.Cmd_GeoRefUI")) as PushButton;
            buttonGeoRef.ToolTip = "Show georef information for current project.";
            buttonGeoRef.LargeImage = largeImage;

            #endregion Georef panel

            #region City2BIM panel

            //---------------------City2BIM panel------------------------------------------------------------

            RibbonPanel panel3 = application.CreateRibbonPanel(tabName, "City2BIM");

            var citygmlPath =
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            var citygmlRootPath = Path.Combine(citygmlPath, "..", "..");
            var imgCitygmlPath = Path.Combine(citygmlRootPath, "img", "citygml_32px_96dpi.png");
            Uri uriImageC = new Uri(imgCitygmlPath);
            BitmapImage largeImageC = new BitmapImage(uriImageC);

            PulldownButton btCityGml = panel3.AddItem(new PulldownButtonData("LoadCityGML", "Import City Model...")) as PulldownButton;
            btCityGml.ToolTip = "Import functionality for CityGML data.";
            btCityGml.LargeImage = largeImageC;

            PushButtonData btSolid = new PushButtonData("LoadCityGMLSolids", "... as Solids",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ReadCityGMLSolids")
            {
                ToolTip = "Imports one Solid representation for each Building(part) to Revit category Entourage."
            };

            PushButtonData btSurfaces = new PushButtonData("LoadCityGMLFaces", "... as Surfaces",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ReadCityGMLFaces")
            {
                ToolTip = "Imports every Surface to the matching Revit category Wall, Roof or Slab."
            };

            btCityGml.AddPushButton(btSurfaces);
            btCityGml.AddPushButton(btSolid);

            var citygmlsetPath =
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            var citygmlSetRootPath = Path.Combine(citygmlsetPath, "..", "..");
            var imgCitygmlSetPath = Path.Combine(citygmlSetRootPath, "img", "citygml_set_32px.png");
            Uri uriImageCSet = new Uri(imgCitygmlSetPath);
            BitmapImage largeImageCSet = new BitmapImage(uriImageCSet);

            PushButton btCitySettings = panel3.AddItem(new PushButtonData("OpenCityGMLSettings", "Settings",
            thisAssemblyPath, "City2RVT.GUI.Cmd_OpenSettings")) as PushButton;
            btCitySettings.ToolTip = "Import settings for city model.";
            btCitySettings.LargeImage = largeImageCSet;

            //-------------------------------------------------------------------------------------------------

            #endregion City2BIM panel

            #region ALKIS panel

            RibbonPanel panel4 = application.CreateRibbonPanel(tabName, "ALKIS2BIM");

            var alkisPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var alkisRootPath = Path.Combine(alkisPath, "..", "..");
            var alkisSetPath = Path.Combine(alkisRootPath, "img", "ALKIS_32px_96dpi.png");
            Uri uriImageA = new Uri(alkisSetPath);
            BitmapImage largeImageA = new BitmapImage(uriImageA);

            PushButton buttonAlkis = panel4.AddItem(new PushButtonData("LoadALKIS", "Import ALKIS data",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ReadALKIS")) as PushButton;
            buttonAlkis.ToolTip = "Import functionality ALKIS data from NAS-XML files.)";
            buttonAlkis.LargeImage = largeImageA;

            var alkisxmlPath =
                 System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            var alkisxmlsetRootPath = Path.Combine(alkisxmlPath, "..", "..");
            var alkisxmlSetPath = Path.Combine(alkisxmlsetRootPath, "img", "ALKISset_32px.png");
            Uri uriImageCAlk = new Uri(alkisxmlSetPath);
            BitmapImage largeImageCAlk = new BitmapImage(uriImageCAlk);

            PushButton btAlkisSettings = panel4.AddItem(new PushButtonData("OpenALKISSettings", "Settings",
            thisAssemblyPath, "City2RVT.GUI.Cmd_OpenALKISSettings")) as PushButton;
            btAlkisSettings.ToolTip = "Import settings for ALKIS data.";
            btAlkisSettings.LargeImage = largeImageCAlk;


            #endregion ALKIS panel

            #region Terrain panel

            RibbonPanel panel5 = application.CreateRibbonPanel(tabName, "DTM2BIM");

            var terrainPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var terrainRootPath = Path.Combine(terrainPath, "..", "..");
            var terrainSetPath = Path.Combine(terrainRootPath, "img", "DTM_32px_96dpi.png");
            Uri uriImageT = new Uri(terrainSetPath);
            BitmapImage largeImageT = new BitmapImage(uriImageT);

            PushButton buttonDTM = panel5.AddItem(new PushButtonData("DTM_Importer", "Get Terrain data",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ReadTerrainXYZ")) as PushButton;
            buttonDTM.ToolTip = "Import functionality for Digital Terrain Models out of XYZ data (regular grid)";
            buttonDTM.LargeImage = largeImageT;

            #endregion Terrain panel

            #region IFC Export panel

            RibbonPanel panel6 = application.CreateRibbonPanel(tabName, "IFC Export");

            var ifcExportPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var ifcExportRootPath = Path.Combine(ifcExportPath, "..", "..");
            var ifcExportSetPath = Path.Combine(ifcExportRootPath, "img", "IFC_32px_96dpi.png");
            Uri uriImageIfc = new Uri(ifcExportSetPath);
            BitmapImage largeImageIfc = new BitmapImage(uriImageIfc);

            PushButton buttonIFC = panel6.AddItem(new PushButtonData("IFC_Exporter", "Export data to IFC-file",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ExportIFC")) as PushButton;
            buttonIFC.ToolTip = "Export functionality for writing IFC files.";
            buttonIFC.LargeImage = largeImageIfc;

            #endregion IFC Export panel

            #region XPlanung panel
            RibbonPanel panel7 = application.CreateRibbonPanel(tabName, "XPlanung");

            var xPlanPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var xPlanRootPath = Path.Combine(xPlanPath, "..", "..");
            var xPlanSetPath = Path.Combine(xPlanRootPath, "img", "XPlan_32px.png");
            Uri uriImageXPlan = new Uri(xPlanSetPath);
            BitmapImage largeImageXPLan = new BitmapImage(uriImageXPlan);

            PushButton buttonXPlan = panel7.AddItem(new PushButtonData("XPlanung_Importer", "Import XPlanung-file",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ImportXPlan")) as PushButton;
            buttonXPlan.ToolTip = "Import functionality for XPlanung files.";
            buttonXPlan.LargeImage = largeImageXPLan;

            var xPlan2IFCPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var xPlan2IFCRootPath = Path.Combine(xPlan2IFCPath, "..", "..");
            var xPlan2IFCSetPath = Path.Combine(xPlan2IFCRootPath, "img", "IFC_32px_96dpi.png");
            Uri uriImageXPlan2IFC = new Uri(xPlan2IFCSetPath);
            BitmapImage largeImageXPLan2IFC = new BitmapImage(uriImageXPlan2IFC);

            PushButton buttonXPlan2IFC = panel7.AddItem(new PushButtonData("XPlanung2IFC", "Exports XPlanung to IFC",
            thisAssemblyPath, "City2RVT.GUI.XPlan2BIM.Cmd_ExportXPlan2IFC")) as PushButton;
            buttonXPlan2IFC.ToolTip = "IFC Export functionality for XPlanung files.";
            buttonXPlan2IFC.LargeImage = largeImageXPLan2IFC;


            #endregion XPlanung panel

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // nothing to clean up in this simple case
            return Result.Succeeded;
        }

        //public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        //{
        //    throw new NotImplementedException();
        //}
    }
}