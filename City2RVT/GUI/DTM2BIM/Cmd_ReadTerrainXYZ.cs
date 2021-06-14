using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using City2RVT.GUI.DTM2BIM; //include for terrain gui

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
            Document doc = revit.Application.ActiveUIDocument.Document;

            Prop_GeoRefSettings.SetInitialSettings(doc);

            var dialog = new Terrain_ImportUI();

            dialog.ShowDialog();

            //var process = new Reader.ReadTerrain(doc);

            return Result.Succeeded;
        }

        
    }
}