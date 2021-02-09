using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

/// <summary>
/// </summary>
using System.Xml.Linq;
using BIMGISInteropLibs.XPlanung;
using City2RVT.Builder;
using City2RVT.Reader;

using City2RVT.GUI.XPlan2BIM;

namespace City2RVT.GUI
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_ImportXPlan : IExternalCommand
    {
        //ExternalCommandData commandData;
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = revit.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Prop_GeoRefSettings.SetInitialSettings(doc);
            MetaInformation.createXPlanungSchema(doc);

            ElementId terrainId = utils.getHTWDDTerrainID(doc);

            // if a base dtm is loaded then terrainAvailable => true
            bool terrainAvailable = (terrainId != null) ? true : false;

            var dialog = new ImportXPlanDialog(terrainAvailable);
            dialog.ShowDialog();

            if (dialog.StartImport)
            {
                XDocument xDoc = XDocument.Load(dialog.FilePath);
                XPlanungReader xPlanReader = new XPlanungReader(xDoc);
                xPlanReader.readData();
                
                var xPlanObjx = xPlanReader.XPlanungObjects;
                var layerNameList = dialog.LayerNamesToImport;

                // https://stackoverflow.com/questions/10745900/filter-a-list-by-another-list-c-sharp :)
                //filter object list based on usage type string in layer name list
                List<XPlanungObject> objsToBuild = xPlanObjx.Where(item => layerNameList.Any(category => category.Equals(item.UsageType))).ToList();

                XPlanBuilder xPlanBuilder = new XPlanBuilder(doc);
                xPlanBuilder.buildRevitObjects(objsToBuild, dialog.Drape);
            }

            return Result.Succeeded;

        }
    }
}
