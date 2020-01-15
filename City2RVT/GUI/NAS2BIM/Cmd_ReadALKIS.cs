using City2BIM.Alkis;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace City2RVT.GUI
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_ReadALKIS : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            Document doc = revit.Application.ActiveUIDocument.Document;

            Prop_GeoRefSettings.SetInitialSettings(doc);

            if (Prop_NAS_settings.FileUrl == "")
                TaskDialog.Show("No file path set!", "Please enter a file path in the settings window first!");
            else
            {
                var alkis = new Reader.ReadALKIS(doc);
            }
            return Result.Succeeded;

        }
    }
}