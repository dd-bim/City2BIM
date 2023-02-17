using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

using Autodesk.Revit.UI;
using Serilog;

namespace CityBIM.GUI
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
            string tabName = "CityBIM";
            application.CreateRibbonTab(tabName);

            var programmDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var versionNumber = application.ControlledApplication.VersionNumber;
            var filePath = "Autodesk\\Revit\\Addins\\" + versionNumber + "\\HTWDDLog.log";
            var logPath = Path.Combine(programmDataPath, filePath);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, rollOnFileSizeLimit: true, shared: true).MinimumLevel.Debug()
                .CreateLogger();


            Log.Information("CityBIM-Application started!");
            //Log.Information("assumed log path is: " + assumedLogPath);

            #region Georef panel

            RibbonPanel panelGeoref = application.CreateRibbonPanel(tabName, "Georeferencing");
            PushButton buttonGeoRef = panelGeoref.AddItem(new PushButtonData("Show Georef information", "Level of Georef", thisAssemblyPath, "CityBIM.GUI.Cmd_GeoRefUI")) as PushButton;
            buttonGeoRef.ToolTip = "Show georef information for current project.";
            buttonGeoRef.LargeImage = getBitmapFromResx(ResourcePictures.Georef_32px_96dpi);

            #endregion Georef panel

            #region Terrain panel
            RibbonPanel panelTerrain = application.CreateRibbonPanel(tabName, "DTM2BIM");
            PushButton buttonDTM = panelTerrain.AddItem(new PushButtonData("DTM_Importer", "Import Terrian data", thisAssemblyPath, "CityBIM.GUI.Cmd_ReadTerrain")) as PushButton;
            buttonDTM.ToolTip = "Import functionality for Digital Terrain Models from different file formats.";
            buttonDTM.LargeImage = getBitmapFromResx(ResourcePictures.DTM_32px_96dpi);
            #endregion Terrain panel

            #region City2BIM panel

            //---------------------City2BIM panel------------------------------------------------------------
            RibbonPanel panelCityGML = application.CreateRibbonPanel(tabName, "City2BIM");
            PushButton btCityGML = panelCityGML.AddItem(new PushButtonData("ImportCityGML", "Import CityGML data", thisAssemblyPath, "CityBIM.GUI.City2BIM.Cmd_ImportCityGML")) as PushButton;
            btCityGML.ToolTip = "Import CityGML data";
            btCityGML.LargeImage = getBitmapFromResx(ResourcePictures.citygml_32px_96dpi);

            //-------------------------------------------------------------------------------------------------

            #endregion City2BIM panel

            #region ALKIS panel

            RibbonPanel panelAlkis = application.CreateRibbonPanel(tabName, "ALKIS2BIM");
            PushButton buttonAlkis = panelAlkis.AddItem(new PushButtonData("LoadALKIS", "Import ALKIS data",
            thisAssemblyPath, "CityBIM.GUI.Cmd_ReadALKIS")) as PushButton;
            buttonAlkis.ToolTip = "Import functionality ALKIS data from NAS-XML files.";
            buttonAlkis.LargeImage = getBitmapFromResx(ResourcePictures.ALKIS_32px_96dpi);

            #endregion ALKIS panel

            #region XPlanung panel

            RibbonPanel panelXplanung = application.CreateRibbonPanel(tabName, "XPlanung");
            PushButton buttonXPlan = panelXplanung.AddItem(new PushButtonData("XPlanung_Importer", "Import XPlanung data",
            thisAssemblyPath, "CityBIM.GUI.Cmd_ImportXPlan")) as PushButton;
            buttonXPlan.ToolTip = "Import functionality for XPlanung files.";
            buttonXPlan.LargeImage = getBitmapFromResx(ResourcePictures.XPlan_32px);



            #endregion XPlanung panel


            #region modify panel

            RibbonPanel panelModify = application.CreateRibbonPanel(tabName, "Modify");
            PushButton buttonHideLayer = panelModify.AddItem(new PushButtonData("Hide surfaces.", "Hides surfaces", thisAssemblyPath, "CityBIM.GUI.Modify.Cmd_HideLayers")) as PushButton;
            buttonHideLayer.ToolTip = "Hide surfaces by its theme.";
            buttonHideLayer.LargeImage = getBitmapFromResx(ResourcePictures.HideLayerIcon_32px_96dpi);
            
            #endregion modify panel

            #region property panel

            RibbonPanel panelProperty = application.CreateRibbonPanel(tabName, "Properties");

            PushButton buttonProperty = panelProperty.AddItem(new PushButtonData("Property", "Manage Properties",
            thisAssemblyPath, "CityBIM.GUI.Properties.Cmd_properties")) as PushButton;
            buttonProperty.ToolTip = "Select a surface to show and edit its properties.";
            buttonProperty.LargeImage = getBitmapFromResx(ResourcePictures.Attribute32);

            #endregion property panel

            #region IFC Export panel

            RibbonPanel panelIFC = application.CreateRibbonPanel(tabName, "IFC Export");

            PushButton buttonIFC = panelIFC.AddItem(new PushButtonData("IFC_Exporter", "Export data to IFC-file",
            thisAssemblyPath, "CityBIM.GUI.Cmd_ExportIFC")) as PushButton;
            buttonIFC.ToolTip = "Export functionality for writing IFC files.";
            buttonIFC.LargeImage = getBitmapFromResx(ResourcePictures.IFC_32px_96dpi);

            #endregion IFC Export panel


            #region Logging
            RibbonPanel panelLogger = application.CreateRibbonPanel(tabName, "Logging");
            PushButton buttonLog = panelLogger.AddItem(new PushButtonData("OpenLog", "Open Log File", thisAssemblyPath, "CityBIM.GUI.Cmd_LogFile")) as PushButton;
            buttonLog.ToolTip = "Opens the current Log file";
            buttonLog.LargeImage = getBitmapFromResx(ResourcePictures.LogIcon);
            #endregion Logging

            #region survPlan panel
            /*
            RibbonPanel panelSurveyorsPlan = application.CreateRibbonPanel(tabName, "Surveyorsplan2BIM");
            PushButton buttonsurvPlan = panelSurveyorsPlan.AddItem(new PushButtonData("SurveyorsplanImporter", "Import Surveyorsplan", thisAssemblyPath, "CityBIM.GUI.Cmd_Surveyorsplan")) as PushButton;
            buttonsurvPlan.ToolTip = "Import functionality for Surveyorsplan data.";
            buttonsurvPlan.LargeImage = getBitmapFromResx(ResourcePictures.Vermesser32);

            
            PushButton buttonattribute = panelSurveyorsPlan.AddItem(new PushButtonData("Attribute", "Attribute", thisAssemblyPath, "CityBIM.Surveyorsplan2Revit.Attributes")) as PushButton;
            buttonattribute.ToolTip = "Show and edit properties.";
            buttonattribute.LargeImage = getBitmapFromResx(ResourcePictures.Attribute32);
            */
            #endregion survPlan panel


            #region Documentation
            RibbonPanel panelDocu = application.CreateRibbonPanel(tabName, "Documentation");
            PushButton buttonDoc = panelDocu.AddItem(new PushButtonData("Doc", "Help!", thisAssemblyPath, "CityBIM.GUI.Cmd_Documentation")) as PushButton;
            buttonDoc.ToolTip = "Open documentation (GitHub)";
            buttonDoc.LargeImage = getBitmapFromResx(ResourcePictures.HelpIcon_32px);
            #endregion Documentation

            return Result.Succeeded;

        }

        public Result OnShutdown(UIControlledApplication application)
        {
            Log.CloseAndFlush();
            // nothing to clean up in this simple case
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

        //public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        //{
        //    throw new NotImplementedException();
        //}
    }
}