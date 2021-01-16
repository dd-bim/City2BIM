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

        public Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            

            //var findResponse = Prop_Revit.DataClient.querySubjects("Leitung");

            var resultPopUp = new findSubjectResultForm();

            resultPopUp.Show();

            //TaskDialog.Show("Message", findResponse);

            return Result.Succeeded;
        }

    }
}
