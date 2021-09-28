using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//include Revit API
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

using City2RVT.GUI.Surveyorsplan2BIM;

namespace City2RVT.GUI
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Cmd_Surveyorsplan : IExternalCommand
    {
        /// <summary>
        /// 
        /// </summary>
        public Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            //get current revit document
            Document doc = revit.Application.ActiveUIDocument.Document;

            //get georef settings based on revit doc
            //Prop_GeoRefSettings.SetInitialSettings(doc);

            //init import ui
            Surveyorsplan_ImportUI importUI = new Surveyorsplan_ImportUI();

            //show main window
            importUI.ShowDialog();

            //TODO
            return Result.Succeeded;
        }
    }
}
