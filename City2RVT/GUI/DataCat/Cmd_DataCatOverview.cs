using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;

using CityBIM.ExternalDataCatalog;

namespace CityBIM.GUI.DataCat
{
    [Transaction(TransactionMode.Manual)]
    public class Cmd_DataCatOverview: IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var schema = CityBIM.utils.getSchemaByName("ExternalDataCatalogSchema");

            if (schema == null)
            {
                TaskDialog.Show("Error", "No external data information in document!");
                return Result.Failed;
            }

            var collector = new FilteredElementCollector(commandData.Application.ActiveUIDocument.Document);
            var filter = new ExtensibleStorageFilter(schema.GUID);
            var affectedRevitElementIds = collector.WherePasses(filter).ToElementIds();

            var entryList = new ObservableCollection<DataGridEntry>();

            foreach (var elementId in affectedRevitElementIds)
            {
                var category = commandData.Application.ActiveUIDocument.Document.GetElement(elementId).Category.Name;
                var entity = commandData.Application.ActiveUIDocument.Document.GetElement(elementId).GetEntity(schema);

                var objectsStringDict = entity.Get<IDictionary<string, string>>("Objects");
                var externalObjectsList = objectsStringDict.Values.Select(obj => CityBIM.ExternalDataCatalog.ExternalDataSchemaObject.fromJSONString(obj)).ToList();

                foreach (var obj in externalObjectsList)
                {
                    entryList.Add(new DataGridEntry { RevitID = elementId.ToString(), RevitCategory = category, IfcClassification = obj.IfcClassification.Name, IfcClassificationReference = obj.IfcClassification.RefList.Last().Name });
                }
            }

            var dialog = new DataCatOverview(commandData, entryList);
            dialog.ShowDialog();

            return Result.Succeeded;
        }
    }
}
