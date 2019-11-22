using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using City2BIM.RevitCommands.Georeferencing;

namespace City2BIM
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class GeorefUI : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            Document doc = revit.Application.ActiveUIDocument.Document;

            GeoRefSettings.SetInitialSettings(doc);

            var form = new GeoRef_Form(doc);
            form.ShowDialog();

            return Result.Succeeded;
        }
    }
}