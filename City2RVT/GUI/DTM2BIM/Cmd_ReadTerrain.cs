﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using City2RVT.GUI.DTM2BIM; //include for terrain gui (Remove by sucess)

using uC = GuiHandler.userControler;

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
    public class Cmd_ReadTerrainXYZ : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            //get revit document
            Document doc = revit.Application.ActiveUIDocument.Document;

            //get georef settings based on revit document
            Prop_GeoRefSettings.SetInitialSettings(doc);

            //init dialog for revit import
            //var dialog = new Terrain_ImportUI();

            //init user controler (otherwise will not be able to init the window)
            uC.Dxf.Read dxfReader = new uC.Dxf.Read();

            //init main window
            Terrain_ImportUI terrainUI = new Terrain_ImportUI();

            //show main window to user (start dialog for settings)
            terrainUI.Show();

            
            
            //var process = new Reader.ReadTerrain(doc);

            return Result.Succeeded;
        }
    }
}