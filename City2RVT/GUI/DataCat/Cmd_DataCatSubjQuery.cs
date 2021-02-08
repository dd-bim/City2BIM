using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using City2RVT.ExternalDataCatalog;

namespace City2RVT.GUI.DataCat
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

            var resultPopUp = new findSubjectResultForm(uiDoc);

            resultPopUp.ShowDialog();

            return Result.Succeeded;
        }

    }
}
