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
using CityBIM.Builder;
using CityBIM.Reader;
using BIMGISInteropLibs.OGR;
using Serilog;

using CityBIM.GUI.XPlan2BIM;

namespace CityBIM.GUI
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
            //MetaInformation.createXPlanungSchema(doc);

            ElementId terrainId = utils.getHTWDDTerrainID(doc);

            // if a base dtm is loaded then terrainAvailable => true
            bool terrainAvailable = (terrainId != null) ? true : false;

            if (!utils.configureOgr())
            {
                Log.Error("Could not configure GDAL/OGR");
                TaskDialog.Show("Error", "Could not configure GDAL/OGR");
                return Result.Failed;
            }

            var dialog = new ImportXPlanDialog(terrainAvailable, doc);
            dialog.ShowDialog();

            if (dialog.StartImport)
            {
                var gmlReader = new OGRGMLReader(dialog.FilePath);
                var geoObjBuilder = new GeoObjectBuilder(doc);

                Log.Information("Starting XPLan import");

                foreach (var layerName in dialog.LayerNamesToImport)
                {
                    var layer = gmlReader.getLayerByName(layerName);
                    var GeoObjs = gmlReader.getGeoObjectsForLayer(layer, dialog.SpatialFilter);
                    Log.Information(string.Format("Total of {0} features in layer {1}", GeoObjs.Count, layerName));
                    var fieldList = gmlReader.getFieldNamesForLayer(layer);
                    try
                    {
                        geoObjBuilder.buildGeoObjectsFromList(GeoObjs, dialog.Drape, fieldList);
                        Log.Information(string.Format("Finished importing layer {0}", layerName));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Format("Error during import of layer {0}", layerName));
                        TaskDialog.Show("Error", ex.ToString());
                        Log.Error(ex.ToString());
                        continue;
                    }
                }

                gmlReader.destroy();

                //XDocument xDoc = XDocument.Load(dialog.FilePath);
                //XPlanungReader xPlanReader = new XPlanungReader(xDoc);
                //xPlanReader.readData();

                //var xPlanObjx = xPlanReader.XPlanungObjects;
                //var layerNameList = dialog.LayerNamesToImport;

                // https://stackoverflow.com/questions/10745900/filter-a-list-by-another-list-c-sharp :)
                //filter object list based on usage type string in layer name list
                //List<XPlanungObject> objsToBuild = xPlanObjx.Where(item => layerNameList.Any(category => category.Equals(item.UsageType))).ToList();

                //XPlanBuilder xPlanBuilder = new XPlanBuilder(doc);
                //xPlanBuilder.buildRevitObjects(objsToBuild, dialog.Drape);
            }

            return Result.Succeeded;

        }
    }
}
