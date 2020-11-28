using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using City2BIM;
using City2BIM.Alkis;
using City2RVT.Reader;
using City2RVT.Builder;
using City2RVT.GUI.NAS2BIM;

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
                AlkisReader alkisReader = new AlkisReader(dialog.FilePath);
                var alkisObjs = alkisReader.AlkisObjects;

                var layerNameList = dialog.LayerNamesToImport;

                // https://stackoverflow.com/questions/10745900/filter-a-list-by-another-list-c-sharp :)
                //filter object list based on usage type string in layer name list
                List<AX_Object> objsToBuild = alkisObjs.Where(item => layerNameList.Any(category => category.Equals(item.UsageType))).ToList();

                AlkisBuilder alkisBuilder = new AlkisBuilder(doc);
                alkisBuilder.buildRevitObjectsFromAlkisList(alkisObjs, dialog.Drape);

            }

            return Result.Succeeded;




            /*
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIApplication uiapp = commandData.Application;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            Prop_GeoRefSettings.SetInitialSettings(doc);

            //var alkis = new Reader.ReadALKIS(doc, app, commandData);

            if (Prop_NAS_settings.FileUrl == "")
                TaskDialog.Show("No file path set!", "Please enter a file path in the settings window first!");
            else
            {
                var alkis = new Reader.ReadALKIS(doc, app, commandData);
            }
            return Result.Succeeded;
            */
        }
    }
}