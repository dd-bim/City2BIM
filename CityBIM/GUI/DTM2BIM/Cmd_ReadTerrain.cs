using System;
using System.IO;
using System.Reflection;

//include Revit API
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

//short cut for user controler
using uC = GuiHandler.userControler;

//
using Serilog;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages


namespace CityBIM.GUI
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_ReadTerrain : IExternalCommand
    {
        public static int numPoints { get; set; }
        public static int numFacets { get; set; }

        // The main Execute method (inherited from IExternalCommand) must be public
        public Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            //get revit document
            Document doc = revit.Application.ActiveUIDocument.Document;

            //init user controler (otherwise will not be able to init the window)
            uC.Dxf.Read ucDxf = new uC.Dxf.Read();
            uC.Grid.Read ucGrid = new uC.Grid.Read();
            uC.Reb.Read ucReb = new uC.Reb.Read();
            uC.Grafbat.Read ucGrafbat = new uC.Grafbat.Read();
            uC.XML.Read ucXml = new uC.XML.Read();
            uC.PostGIS.Read ucPostGIS = new uC.PostGIS.Read();
            uC.GeoJSON.Read ucGeoJson = new uC.GeoJSON.Read();

            //init main window
            DTM2BIM.Terrain_ImportUI terrainUI = new DTM2BIM.Terrain_ImportUI();

            #region version handler
            //get current revit version
            utils.rvtVersion rvtVersion = utils.GetVersionInfo(doc.Application);

            BIMGISInteropLibs.IfcTerrain.Config config = new BIMGISInteropLibs.IfcTerrain.Config();

            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            string logPath = Path.GetDirectoryName(thisAssemblyPath);

            //set logfile path
            config.logFilePath = logPath;

            if (rvtVersion.Equals(utils.rvtVersion.NotSupported))
            {
                //error massage
                TaskDialog.Show("Supported version", "The used Revit version is not supported!" + Environment.NewLine + "Processing failed!");
                
                return Result.Failed;
            }
            else
            {
                //
                Log.Debug(" Opening the settings for processing DTMs");

                //
                terrainUI.DataContext = config as BIMGISInteropLibs.IfcTerrain.Config;

                //show main window to user (start dialog for settings)
                terrainUI.ShowDialog();

                //get config from window
                config = terrainUI.DataContext as BIMGISInteropLibs.IfcTerrain.Config;

                if (!string.IsNullOrEmpty(config.logFilePath))
                {
                    LogWriter.initLogger(config);
                }
                else
                {
                    Log.Warning("Log file path has been empty! Set to: " + logPath);
                    config.logFilePath = logPath;
                    
                    LogWriter.initLogger(config);
                }
                Log.Information("RvtTerrain log file is stored under: " + Environment.NewLine + "'" + config.logFilePath + "'");
            }
            #endregion

            if(terrainUI.startTerrainImport)
            {
                Log.Debug("Start terrain processing...");

                //start mapping process
                var res = BIMGISInteropLibs.RvtTerrain.ConnectionInterface.mapProcess(config);

                //init surface builder
                var rev = new Builder.RevitTopoSurfaceBuilder(doc);

                if (res != null && config.rvtReadPoints.GetValueOrDefault())
                {
                    //create dtm (via points)
                    rev.createDTMviaPoints(res);
                }
                else if(res != null && !config.rvtReadPoints.GetValueOrDefault())
                {
                    //create dtm via points & faces
                    rev.createDTM(res);
                }
                else
                {
                    #region define task dialog
                    TaskDialog dialog = new TaskDialog("DTM processing");
                    dialog.MainIcon = TaskDialogIcon.TaskDialogIconError;
                    dialog.Title = "DTM processing failed!";
                    dialog.AllowCancellation = true;
                    dialog.MainInstruction = "Check log file!";
                    dialog.MainContent = "DTM processing failed - for more information check log file or try other settings. Want to open storage of log file?";
                    dialog.FooterText = "Error caused by RvtTerrain.";

                    //define "shown buttons"
                    dialog.CommonButtons =
                        TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;

                    //define pre selected button
                    dialog.DefaultButton = TaskDialogResult.Yes;
                    #endregion define task dialog

                    //open dialog
                    TaskDialogResult dialogResult = dialog.Show();

                    //react on dialog
                    if (dialogResult.Equals(TaskDialogResult.Yes))
                    {
                        if (Directory.Exists(Path.GetDirectoryName(config.logFilePath)))
                        {
                            System.Diagnostics.Process.Start("explorer.exe", config.logFilePath);
                        }
                    }
                    return Result.Failed;
                }

                //error handlings
                if (rev.terrainImportSuccesful)
                {
                    dynamic resLog;
                    
                    if (config.rvtReadPoints.GetValueOrDefault())
                    {
                        resLog = "Points: " + numPoints;
                    }
                    else
                    {
                        resLog = "Points: " + numPoints + " Faces: " + numFacets;
                    }
                    //show info dialog (may update to better solution)
                    TaskDialog.Show("DTM import", "DTM import finished! Result:" + Environment.NewLine + resLog);

                    //process successfuly
                    return Result.Succeeded;
                }
                else
                {
                    //TODO improve error message
                    TaskDialog.Show("DTM import failed!", "The DTM import failed.");

                    return Result.Failed;
                }
            }
            else
            {
                //user canceld / closed window
                return Result.Cancelled;
            }
        }
    }
}