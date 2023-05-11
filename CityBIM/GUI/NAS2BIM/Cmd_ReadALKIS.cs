using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMGISInteropLibs.Alkis;
using CityBIM.Reader;
using CityBIM.Builder;
using CityBIM.GUI.NAS2BIM;
using BIMGISInteropLibs.OGR;
using Serilog;

namespace CityBIM.GUI
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_ReadALKIS : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            ElementId terrainId = utils.getHTWDDTerrainID(doc);

            // if a base dtm is loaded then terrainAvailable => true
            bool terrainAvailable = (terrainId != null) ? true : false;

            if (!utils.configureOgr())
            {
                Log.Error("Could not configure GDAL/OGR");
                TaskDialog.Show("Error", "Could not configure GDAL/OGR");
                return Result.Failed;
            }

            var dialog = new ImportDialogAlkis(terrainAvailable, doc);
            dialog.ShowDialog();

            if (dialog.StartImport)
            {

                var layerNameList = dialog.LayerNamesToImport;

                var ogrReader = new OGRALKISReader(dialog.FilePath);
                var GeoObjBuilder = new GeoObjectBuilder(doc);
                Log.Information("Starting ALKIS-Import");
                foreach (var layerName in layerNameList)
                {
                    var layer = ogrReader.getLayerByName(layerName);
                    var GeoObjs = ogrReader.getGeoObjectsForLayer(layer, dialog.SpatialFilter);
                    Log.Information(string.Format("Total of {0} features in layer {1}", GeoObjs.Count, layerName));
                    var fieldList = ogrReader.getFieldNamesForLayer(layer);

                    var newFieldList = new List<string>();
                    for (int i = 0; i < fieldList.Count; i++)
                    {
                        var newEntry = fieldList[i].Replace("|", "_").Replace("-", "_"); ;
                        
                        if (newEntry.Length > GeoObjectBuilder.REVIT_MAX_FIELD_NAME_LENGTH)
                        {
                            newEntry = newEntry.Substring(0, GeoObjectBuilder.REVIT_MAX_FIELD_NAME_LENGTH);
                        }
                        
                        newFieldList.Add(newEntry);
                    }
                    fieldList = newFieldList;

                    foreach (var GeoObj in GeoObjs)
                    {
                        Dictionary<string, string> newPropDict = new Dictionary<string, string>();

                        foreach (KeyValuePair<string, string> entry in GeoObj.Properties)
                        {
                            var currentKey = entry.Key.Replace("|", "_").Replace("-","_");
                            
                            if (currentKey.Length > GeoObjectBuilder.REVIT_MAX_FIELD_NAME_LENGTH)
                            {
                                currentKey = currentKey.Substring(0, GeoObjectBuilder.REVIT_MAX_FIELD_NAME_LENGTH);
                            }
                            newPropDict[currentKey] = entry.Value;
                        }
                        GeoObj.Properties = newPropDict;
                    }

                    GeoObjBuilder.buildGeoObjectsFromList(GeoObjs, dialog.Drape, fieldList);
                    Log.Information(string.Format("Finished importing layer {0}", layerName));
                }

                ogrReader.destroy();

            }

            return Result.Succeeded;

        }
    }
}