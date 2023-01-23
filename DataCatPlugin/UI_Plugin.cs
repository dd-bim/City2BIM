using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

using Autodesk.Revit.UI;
using Serilog;

namespace DataCatPlugin
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class UI_Plugin : IExternalApplication
    {

        public Result OnStartup(UIControlledApplication application)
        {
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            string pluginTabName = "DataCat";
            application.CreateRibbonTab(pluginTabName);

            string logPath = Path.Combine(Path.GetDirectoryName(thisAssemblyPath), "DataCatPluginLog.log");

            Log.Logger = new LoggerConfiguration().WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, rollOnFileSizeLimit: true).CreateLogger();

            #region DataCat panel

            RibbonPanel panelDataCat = application.CreateRibbonPanel(pluginTabName, "DataCat");
            PushButton loginDataCat = panelDataCat.AddItem(new PushButtonData("LoginBtn", "Login", thisAssemblyPath, "DataCatPlugin.GUI.Cmd_DataCatLogin")) as PushButton;
            loginDataCat.ToolTip = "Login to server";
            loginDataCat.LargeImage = getBitmapFromResx(ResourceImages.loginIcon32);

            PushButton querySubjects = panelDataCat.AddItem(new PushButtonData("QuerySubjBtn", "Query Catalog", thisAssemblyPath, "DataCatPlugin.GUI.Cmd_DataCatSubjQuery")) as PushButton;
            querySubjects.ToolTip = "Query server with key word";
            querySubjects.LargeImage = getBitmapFromResx(ResourceImages.queryDataCat);

            PushButton overviewEditor = panelDataCat.AddItem(new PushButtonData("OverviewBtn", "Overview", thisAssemblyPath, "DataCatPlugin.GUI.Cmd_DataCatOverview")) as PushButton;
            overviewEditor.ToolTip = "Show overview of all data objects";
            overviewEditor.LargeImage = getBitmapFromResx(ResourceImages.overViewIcon);

            #endregion DataCat panel


            #region IFC Export panel

            RibbonPanel panelIFC = application.CreateRibbonPanel(pluginTabName, "IFC Export");

            PushButton buttonIFC = panelIFC.AddItem(new PushButtonData("IFC_Exporter", "Export data to IFC-file",
            thisAssemblyPath, "DataCatPlugin.GUI.Cmd_ExportIFC")) as PushButton;
            buttonIFC.ToolTip = "Export functionality for writing IFC files.";
            buttonIFC.LargeImage = getBitmapFromResx(ResourceImages.IFC_32px_96dpi);

            #endregion IFC Export panel


            Log.Information("DataCatPlugin successfully started");

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application) {
            Log.CloseAndFlush();
            return Result.Succeeded;
        }

        private BitmapImage getBitmapFromResx(System.Drawing.Bitmap bmp)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage img = new BitmapImage();
            ms.Position = 0;
            img.BeginInit();
            img.StreamSource = ms;
            img.EndInit();

            return img;
        }
    }
}