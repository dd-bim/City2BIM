using System.Collections.Generic;

//include Revit API
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using City2RVT.GUI.DTM2BIM; //include for terrain gui (Remove by sucess)

//short cut for user controler
using uC = GuiHandler.userControler;

//shortcut to set json settings
using init = GuiHandler.InitClass;

//namespace for C2BPoint class (may need to be removed)
using C2BPoint = BIMGISInteropLibs.Geometry.C2BPoint;

using City2RVT.GUI;

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
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            //get revit document
            Document doc = revit.Application.ActiveUIDocument.Document;

            //get georef settings based on revit document
            Prop_GeoRefSettings.SetInitialSettings(doc);

            //init user controler (otherwise will not be able to init the window)
            uC.Dxf.Read ucDxf = new uC.Dxf.Read();
            uC.Grid.Read ucGrid = new uC.Grid.Read();

            //init main window
            Terrain_ImportUI terrainUI = new Terrain_ImportUI();

            //show main window to user (start dialog for settings)
            terrainUI.ShowDialog();

            if(terrainUI.startTerrainImport)
            {
                //start mapping process
                //var res = BIMGISInteropLibs.RvtTerrain.ConnectionInterface.mapProcess(init.config);

                //
                //var rev = new Builder.RevitTopoSurfaceBuilder(doc);
                //rev.CreateDTM(res);

                var process = new Reader.ReadTerrain(doc);


            }
            else
            {
                TaskDialog.Show("Import canceld!", "The import has been canceld by user.");
            }

            



            

            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }    
}