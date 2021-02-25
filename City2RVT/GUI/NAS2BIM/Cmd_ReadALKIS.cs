using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMGISInteropLibs.Alkis;
using City2RVT.Reader;
using City2RVT.Builder;
using City2RVT.GUI.NAS2BIM;
using BIMGISInteropLibs.OGR;
using Serilog;

namespace City2RVT.GUI
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
            Prop_GeoRefSettings.SetInitialSettings(doc);

            ElementId terrainId = utils.getHTWDDTerrainID(doc);

            // if a base dtm is loaded then terrainAvailable => true
            bool terrainAvailable = (terrainId != null) ? true : false;

            if (!utils.configureOgr())
            {
                Log.Error("Could not configure GDAL/OGR");
                TaskDialog.Show("Error", "Could not configure GDAL/OGR");
                return Result.Failed;
            }

            var dialog = new ImportDialogAlkis(terrainAvailable);
            dialog.ShowDialog();

            if (dialog.StartImport)
            {

                var layerNameList = dialog.LayerNamesToImport;

                var ogrReader = new OGRALKISReader(dialog.FilePath);
                var GeoObjBuilder = new GeoObjectBuilder(doc);
                foreach (var layerName in layerNameList)
                {
                    var layer = ogrReader.getLayerByName(layerName);
                    var GeoObjs = ogrReader.getGeoObjectsForLayer(layer);
                    var fieldList = ogrReader.getFieldNamesForLayer(layer);

                    GeoObjBuilder.buildGeoObjectsFromList(GeoObjs, dialog.Drape, fieldList);
                }

                ogrReader.destroy();

            }

            return Result.Succeeded;

        }
    }
}