using System;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CityBIM.GUI
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class Cmd_GeoRefUI : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            Document doc = revit.Application.ActiveUIDocument.Document;

            Prop_GeoRefSettings.SetInitialSettings(doc);

            var form = new GUI.Wpf_GeoRef_Form(doc);
            form.ShowDialog();

            return Result.Succeeded;
        }
    }
}