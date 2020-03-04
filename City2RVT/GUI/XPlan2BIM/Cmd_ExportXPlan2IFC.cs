using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace City2RVT.GUI.XPlan2BIM
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_ExportXPlan2IFC : IExternalCommand
    {
        
        //ExternalCommandData commandData;
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            //MessageBox.Show("Hallo Export");
            Document doc = revit.Application.ActiveUIDocument.Document;

            Prop_GeoRefSettings.SetInitialSettings(doc);

            var process = new XPlan2BIM.Wpf_XPlan2IFC(revit);
            process.ShowDialog();

            return Result.Succeeded;
        }
    }
}
