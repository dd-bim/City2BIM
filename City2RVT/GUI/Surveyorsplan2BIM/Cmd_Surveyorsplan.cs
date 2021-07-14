using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//include Revit API
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace City2RVT.GUI
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_Surveyorsplan : IExternalCommand
    {
        public Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            //get current revit document
            Document doc = revit.Application.ActiveUIDocument.Document;

            //get georef settings based on revit doc
            //Prop_GeoRefSettings.SetInitialSettings(doc);

            //init import ui
            Surveyorsplan2BIM.Surveyorsplan_ImportUI importUI = new Surveyorsplan2BIM.Surveyorsplan_ImportUI(doc);

            //show main window
            importUI.ShowDialog();

            if (importUI.startSurvImport)
            {
                //TODO
                return Result.Failed;
            }
            else
            {
                //return cancelled --> user has canceled dialog ui
                return Result.Cancelled;
            }
        }
    }
}
