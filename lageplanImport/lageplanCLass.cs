using System;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Windows.Media.Imaging;

namespace lageplanImport
{
    [TransactionAttribute(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public class lageplanClass : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "Aussenbereich";
            application.CreateRibbonTab(tabName);
            RibbonPanel dxfPanelLageplan = application.CreateRibbonPanel(tabName, "Import surveyorsplan");
            RibbonPanel dxfPanelAttribute = application.CreateRibbonPanel(tabName, "Define attributes");
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            PushButton importButton = dxfPanelLageplan.AddItem(new PushButtonData("Import Lageplan (dxf)", "Import surveyorsplan", thisAssemblyPath, "lageplanImport.importLageplan")) as PushButton;
            importButton.ToolTip = "Imports geometry and attributes from an dxf-file.";
            var globePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Vermesser32.png");
            Uri uriImage = new Uri(globePath);
            BitmapImage largeImage = new BitmapImage(uriImage);
            importButton.LargeImage = largeImage;
            PushButton attributButton = dxfPanelAttribute.AddItem(new PushButtonData("Define Attributes", "Define Attributes", thisAssemblyPath, "lageplanImport.Attributes")) as PushButton;
            attributButton.ToolTip = "Sets attributes for families. Only available in family editor.";
            var globePath2 = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Attribute32.png");
            Uri uriImage2 = new Uri(globePath2);
            BitmapImage largeImage2 = new BitmapImage(uriImage2);
            attributButton.LargeImage = largeImage2;


            //Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.File(@"D:\log.txt", rollingInterval: RollingInterval.Day).CreateLogger();
            //Log.Information("Ribbon created.");
            //Log.CloseAndFlush();
            //Console.Read();

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
