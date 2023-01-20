using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using DataCatPlugin.ExternalDataCatalog;

namespace DataCatPlugin.GUI
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class Cmd_DataCatSubjQuery : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;

            if (ExternalDataUtils.testTokenValidity() == false)
            {
                TaskDialog.Show("Error!", "You are currently not logged into the external server!");
                return Result.Failed;
            }

            findSubjectResultForm resultPopUp = new findSubjectResultForm(uiDoc);

            resultPopUp.ShowDialog();

            return Result.Succeeded;
        }

    }
}
