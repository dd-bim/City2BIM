using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace lageplanImport
{
    public class attributeList
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            var laubbaum = new Tuple<string, string, string, string>("Laubbaum", "Art", "Stammumfang", "Kronendurchmesser");
            return Result.Succeeded;
        }
        
    }
}
