using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.IO;

namespace City2RVT.GUI
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_ExportIFC : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            string modulePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "City2BIM");

            TaskDialog.Show("Ifc Export", "Please use Revit Ifc-Exporter. For correct naming of georef property set please import" +
                " City2BIM_ParameterSet.txt in " + modulePath + " as user-defined Property Set in the IFC Exporter!");

            var ifcSem = new Builder.Revit_Semantic(revit.Application.ActiveUIDocument.Document);
            ifcSem.CreateParameterSetFile();

            return Result.Succeeded;
        }
    }
}
