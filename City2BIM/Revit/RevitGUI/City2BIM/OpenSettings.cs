using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace City2BIM
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class OpenSettings : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            Document doc = revit.Application.ActiveUIDocument.Document;

            GeoRefSettings.SetInitialSettings(doc);

            var process = new RevitCommands.City2BIM.CityGML_settings();
            process.ShowDialog();

            return Result.Succeeded;
        }
    }
}
