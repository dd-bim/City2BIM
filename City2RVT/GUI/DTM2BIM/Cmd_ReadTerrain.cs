//include Revit API
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using City2RVT.GUI.DTM2BIM; //include for terrain gui (Remove by sucess)

//short cut for user controler
using uC = GuiHandler.userControler;

//shortcut to set json settings
using init = GuiHandler.InitClass;

namespace City2RVT.GUI
{
    /*/TODO DTM2BIM
    - eventuell Settings window
    - Einstellungen für Ausdünnung bei großen Rastern
    - Umkreis einschränken als Option
    /*/

    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_ReadTerrain : IExternalCommand
    {
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
            //TODO: postgis reader

            //init main window
            Terrain_ImportUI terrainUI = new Terrain_ImportUI();

            #region version handler
            //get current revit version
            utils.rvtVersion rvtVersion = utils.GetVersionInfo(doc.Application);

            if (rvtVersion.Equals(utils.rvtVersion.NotSupported))
            {
                //error massage
                TaskDialog.Show("Supported version", "The used Revit version is not supported!\nProcessing failed!");
                
                return Result.Failed;
            }
            else
            {
                //enable delauny triangulation as conversion option
                if (rvtVersion.Equals(utils.rvtVersion.R20))
                {
                    terrainUI.cbDelauny.IsEnabled = true;
                }
                
                //show main window to user (start dialog for settings)
                terrainUI.ShowDialog();
            }
            #endregion

            if(terrainUI.startTerrainImport)
            {
                //start mapping process
                var res = BIMGISInteropLibs.RvtTerrain.ConnectionInterface.mapProcess(init.config);

                //init surface builder
                var rev = new Builder.RevitTopoSurfaceBuilder(doc);

                if (!terrainUI.useDelaunyTriangulation)
                {
                    //create dtm (TODO - update)
                    rev.CreateDTM(res);
                }
                else
                {
                    //error handling
                    TaskDialog.Show("Error - Implementation failed","Sorry!,\nSomething went wrong.");

                    return Result.Failed;
                }

                //show info dialog (may update to better solution)
                TaskDialog.Show("DTM import", "DTM import finished!");

                //process successfuly
                return Result.Succeeded;
            }
            else
            {
                //user canceld / closed window
                return Result.Cancelled;
            }
        }
    }
}