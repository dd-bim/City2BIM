using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Serilog;

namespace City2BIM
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class PlugIn : IExternalApplication
    {
        // Both OnStartup and OnShutdown must be implemented as public method
        // Method "OnStartup" is a Method of IExternalApplication Interface and executes some tasks when Revit starts
        public Result OnStartup(UIControlledApplication application)
        {
            // Add a new Tab
            string tabName = "City2BIM";
            application.CreateRibbonTab(tabName);

            // Add a new ribbon panel
            RibbonPanel panel1 = application.CreateRibbonPanel(tabName, "Settings");
            RibbonPanel panel2 = application.CreateRibbonPanel(tabName, "Georeferencing");
            RibbonPanel panel3 = application.CreateRibbonPanel(tabName, "City2BIM");
            RibbonPanel panel4 = application.CreateRibbonPanel(tabName, "BIM2City");
            RibbonPanel panel5 = application.CreateRibbonPanel(tabName, "DTM2BIM");

            //DB RibbonPanel panel = ribbonPanel(application);
            // Create a push button to trigger a command add it to the ribbon panel.
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            PushButton buttonFile = panel1.AddItem(new PushButtonData("Create info table", "Import settings",
            thisAssemblyPath, "City2BIM.ImportSource")) as PushButton;
            buttonFile.ToolTip = "Test window appearing";

            PushButton buttonGeoRef = panel2.AddItem(new PushButtonData("Show Georef information", "Import settings",
            thisAssemblyPath, "City2BIM.RevitCommands.Georeferencing.GeorefUI")) as PushButton;
            buttonFile.ToolTip = "Show georef";

            PushButton btSolid = panel3.AddItem(new PushButtonData("LoadCityGMLSolids", "Get City Model as Solids",
            thisAssemblyPath, "City2BIM.ReadCityGMLSolids")) as PushButton;
            btSolid.ToolTip = "Import functionality for CityGML data as Solid models with category Entourage";

            PushButton button = panel3.AddItem(new PushButtonData("LoadCityGMLFaces", "Get City Model as Faces",
            thisAssemblyPath, "City2BIM.ReadCityGMLFaces")) as PushButton;
            button.ToolTip = "Import functionality for CityGML data as Face models with categories Wall, StructuralSlab and Roof";

            PushButton buttonCode = panel3.AddItem(new PushButtonData("Get CityGML Code Description", "Get Code Decription",
            thisAssemblyPath, "City2BIM.ReadCode")) as PushButton;
            button.ToolTip = "Import functionality for CityGML data";

            PushButton buttonDTM = panel5.AddItem(new PushButtonData("DTM_Importer", "Get DTM from XYZ-file",
            thisAssemblyPath, "City2BIM.ReadTerrainXYZ")) as PushButton;
            button.ToolTip = "Import functionality for Digital Terrain Models out of XYZ data (regular grid)";





            PushButton buttonTeest = panel2.AddItem(new PushButtonData("Create indddfo table", "Create Info table",
            thisAssemblyPath, "City2BIM.CreateTable")) as PushButton;
            buttonTeest.ToolTip = "Test window appearing";


            var globePath =
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName
            (System.Reflection.Assembly.GetExecutingAssembly().Location), "icon_ico.ico");
            Uri uriImage = new Uri(globePath);
            BitmapImage largeImage = new BitmapImage(uriImage);

            button.LargeImage = largeImage;

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