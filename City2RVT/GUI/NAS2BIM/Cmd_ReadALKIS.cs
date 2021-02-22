using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMGISInteropLibs.Alkis;
using City2RVT.Reader;
using City2RVT.Builder;
using City2RVT.GUI.NAS2BIM;
using BIMGISInteropLibs.OGR;

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

            var dialog = new ImportDialogAlkis(terrainAvailable);
            dialog.ShowDialog();

            if (dialog.StartImport)
            {
                MetaInformation.createALKISSchema(doc);
                //AlkisReader alkisReader = new AlkisReader(dialog.FilePath);
                //var alkisObjs = alkisReader.AlkisObjects;

                var layerNameList = dialog.LayerNamesToImport;

                var ogrReader = new OGRALKISReader(dialog.FilePath);
                foreach (var layerName in layerNameList)
                {
                    var GeoObjs = ogrReader.getGeoObjectsForLayer(ogrReader.getLayerByName(layerName));
                    var GeoObjBuilder = new GeoObjectBuilder(doc);
                    GeoObjBuilder.buildGeoObjectsFromList(GeoObjs, dialog.Drape);
                }

                // https://stackoverflow.com/questions/10745900/filter-a-list-by-another-list-c-sharp :)
                //filter object list based on usage type string in layer name list
                //List<AX_Object> objsToBuild = alkisObjs.Where(item => layerNameList.Any(category => category.Equals(item.UsageType))).ToList();

                //AlkisBuilder alkisBuilder = new AlkisBuilder(doc);
                //alkisBuilder.buildRevitObjectsFromAlkisList(alkisObjs, dialog.Drape);

            }

            return Result.Succeeded;

        }
    }
}