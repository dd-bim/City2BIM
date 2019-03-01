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
            RibbonPanel panel = application.CreateRibbonPanel(tabName, "CityGML Importer");

            //DB RibbonPanel panel = ribbonPanel(application);
            // Create a push button to trigger a command add it to the ribbon panel.
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            PushButton buttonTest = panel.AddItem(new PushButtonData("Test window", "test window",
            thisAssemblyPath, "City2BIM.HelloWorld")) as PushButton;
            buttonTest.ToolTip = "Test window appearing";

            PushButton button = panel.AddItem(new PushButtonData("Load CityGML", "Import functionality for CityGML data",
            thisAssemblyPath, "City2BIM.ReadCityGML")) as PushButton;
            button.ToolTip = "Import functionality for CityGML data";

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