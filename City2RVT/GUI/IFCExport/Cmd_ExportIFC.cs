using System;
using System.IO;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Xbim.Ifc;
using Xbim.Ifc4.ProductExtension;
using Serilog;

using City2RVT.IFCExport;
using City2RVT.GUI.IFCExport;


namespace City2RVT.GUI
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_ExportIFC : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Prop_GeoRefSettings.SetInitialSettings(doc);


            var cityGMLBuildingList = utils.getIfc2CityGMLGuidDic(doc);

            var dialog = new IfcExportDialog();
            dialog.ShowDialog();

            if (dialog.startExport)
            {
                RevitIfcExporter exporter = new RevitIfcExporter(doc);

                exporter.startRevitIfcExport(dialog.ExportPath, commandData);

                Log.Information("start custom IFC export");

                using (var model = IfcStore.Open(dialog.ExportPath))
                {
                    using (var txn = model.BeginTransaction("Edit standard Revit export"))
                    {
                        exporter.createLoGeoRef50(model);
                        exporter.exportDTM(model);

                        var site = model.Instances.OfType<IfcSite>().FirstOrDefault();

                        exporter.exportSurfaces(model, dialog.ExportType, site);

                        if (cityGMLBuildingList.Count > 0)
                        {
                            exporter.addCityGMLAttributes(model, doc, cityGMLBuildingList);
                        }

                        exporter.addExternalData(model, doc);

                        txn.Commit();
                    }
                    model.SaveAs(dialog.ExportPath);
                }
            }

            Log.Information("finished custom IFC export");

            return Result.Succeeded;
        }
    }
}
