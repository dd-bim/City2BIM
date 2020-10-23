using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Xml;
using netDxf;
using netDxf.Entities;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

using Excel = Microsoft.Office.Interop.Excel;
using lageplanImport.Lines;

namespace lageplanImport
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class importLageplan : IExternalCommand
    {    
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;            

            mainForm mf = new mainForm(commandData);
            var result = mf.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {

            }

            return Result.Succeeded;
        }
    }
}
