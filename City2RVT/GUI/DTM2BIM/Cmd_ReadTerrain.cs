using System;

//include Revit API
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using City2RVT.GUI.DTM2BIM; //include for terrain gui (Remove by sucess)

//short cut for user controler
using uC = GuiHandler.userControler;

//shortcut to set json settings
using init = GuiHandler.InitClass;

using rvtRes = BIMGISInteropLibs.RvtTerrain;

//embed for file logging
using BIMGISInteropLibs.Logging;                                    //acess to logger
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain;    //to set log messages

namespace City2RVT.GUI
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

            //get georef settings based on revit document
            Prop_GeoRefSettings.SetInitialSettings(doc);

            //init user controler (otherwise will not be able to init the window)
            uC.Dxf.Read ucDxf = new uC.Dxf.Read();
            uC.Grid.Read ucGrid = new uC.Grid.Read();
            uC.Reb.Read ucReb = new uC.Reb.Read();
            uC.Grafbat.Read ucGrafbat = new uC.Grafbat.Read();
            uC.XML.Read ucXml = new uC.XML.Read();
            uC.PostGIS.Read ucPostGIS = new uC.PostGIS.Read();

            //init main window
            Terrain_ImportUI terrainUI = new Terrain_ImportUI();

            #region version handler
            //get current revit version
            utils.rvtVersion rvtVersion = utils.GetVersionInfo(doc.Application);

            //logwriter
            LogWriter.initLogger(init.config);

            if (rvtVersion.Equals(utils.rvtVersion.NotSupported))
            {
                //error massage
                TaskDialog.Show("Supported version", "The used Revit version is not supported!" + Environment.NewLine + "Processing failed!");
                
                return Result.Failed;
            }
            else
            {
                //show main window to user (start dialog for settings)
                terrainUI.ShowDialog();
            }
            #endregion

            if(terrainUI.startTerrainImport)
            {
                //start mapping process
                var res = rvtRes.ConnectionInterface.mapProcess(init.config);

                //init surface builder
                var rev = new Builder.RevitTopoSurfaceBuilder(doc);

                if (res != null && init.config.readPoints.GetValueOrDefault())
                {
                    //create dtm (via points)
                    rev.createDTMviaPoints(res);
                }
                else if(res != null && !init.config.readPoints.GetValueOrDefault())
                {
                    //create dtm via points & faces
                    rev.createDTM(res);
                }
                else
                {
                    //
                    TaskDialog.Show("DTM processing", "File reading failed. Please check log file!");

                    return Result.Failed;
                }

                //error handlings
                if (rev.terrainImportSuccesful)
                {
                    dynamic resLog;
                    
                    if (init.config.readPoints.GetValueOrDefault())
                    {
                        resLog = "Points: " + numPoints;
                    }
                    else
                    {
                        resLog = "Points: " + numPoints + " Faces: " + numFacets;
                    }

                    //reset config
                    init.clearConfig();

                    //show info dialog (may update to better solution)
                    TaskDialog.Show("DTM import", "DTM import finished!" + Environment.NewLine + resLog);

                    //process successfuly
                    return Result.Succeeded;
                }
                else
                {
                    //reset config
                    init.clearConfig();

                    //TODO improve error message
                    TaskDialog.Show("DTM import failed!", "The DTM import failed.");

                    return Result.Failed;
                }
            }
            else
            {
                //reset config
                init.clearConfig();

                //user canceld / closed window
                return Result.Cancelled;
            }
        }
    }
}