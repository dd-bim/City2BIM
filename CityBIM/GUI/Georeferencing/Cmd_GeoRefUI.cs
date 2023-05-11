using System;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CityBIM.GUI.Georeferencing;

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

            var viewModel = new GeoRefViewModel(doc);
            var dialog = new NewGeoRefWindow();
            dialog.DataContext = viewModel;
            var result = dialog.ShowDialog();
            
            return Result.Succeeded;
        }
    }
}