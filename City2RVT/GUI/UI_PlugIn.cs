using System;
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
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Georef_32px_96dpi.png");
            Uri uriImage = new Uri(globePath);
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
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "citygml_32px_96dpi.png");
            Uri uriImageC = new Uri(citygmlPath);
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
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "citygml_set_32px.png");
            Uri uriImageCSet = new Uri(citygmlsetPath);
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
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ALKIS_32px_96dpi.png");
            Uri uriImageA = new Uri(alkisPath);
            BitmapImage largeImageA = new BitmapImage(uriImageA);

            PushButton buttonAlkis = panel4.AddItem(new PushButtonData("LoadALKIS", "Import ALKIS data",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ReadALKIS")) as PushButton;
            buttonAlkis.ToolTip = "Import functionality ALKIS data from NAS-XML files.)";
            buttonAlkis.LargeImage = largeImageA;

            var alkisxmlsetPath =
                 System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ALKISset_32px.png");
            Uri uriImageCAlk = new Uri(alkisxmlsetPath);
            BitmapImage largeImageCAlk = new BitmapImage(uriImageCAlk);

            PushButton btAlkisSettings = panel4.AddItem(new PushButtonData("OpenALKISSettings", "Settings",
            thisAssemblyPath, "City2RVT.GUI.Cmd_OpenALKISSettings")) as PushButton;
            btAlkisSettings.ToolTip = "Import settings for ALKIS data.";
            btAlkisSettings.LargeImage = largeImageCAlk;


            #endregion ALKIS panel

            #region Terrain panel

            RibbonPanel panel5 = application.CreateRibbonPanel(tabName, "DTM2BIM");

            var terrainPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DTM_32px_96dpi.png");
            Uri uriImageT = new Uri(terrainPath);
            BitmapImage largeImageT = new BitmapImage(uriImageT);

            PushButton buttonDTM = panel5.AddItem(new PushButtonData("DTM_Importer", "Get Terrain data",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ReadTerrainXYZ")) as PushButton;
            buttonDTM.ToolTip = "Import functionality for Digital Terrain Models out of XYZ data (regular grid)";
            buttonDTM.LargeImage = largeImageT;

            #endregion Terrain panel

            #region IFC Export panel

            RibbonPanel panel6 = application.CreateRibbonPanel(tabName, "IFC Export");

            var ifcExportPath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "IFC_32px_96dpi.png");
            Uri uriImageIfc = new Uri(ifcExportPath);
            BitmapImage largeImageIfc = new BitmapImage(uriImageIfc);

            PushButton buttonIFC = panel6.AddItem(new PushButtonData("IFC_Exporter", "Export data to IFC-file",
            thisAssemblyPath, "City2RVT.GUI.Cmd_ExportIFC")) as PushButton;
            buttonIFC.ToolTip = "Export functionality for writing IFC files.";
            buttonIFC.LargeImage = largeImageIfc;

            #endregion IFC Export panel

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