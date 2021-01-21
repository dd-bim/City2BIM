using System;
using System.IO;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Xbim.Ifc;
using Xbim.Ifc4.ProductExtension;

using City2RVT.IFCExport;
using City2RVT.GUI.IFCExport;
using City2RVT.ExternalDataCatalog;

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

            string outFolder = @"D:\LandBIM\OrdnerÜbergabe\Testdaten\IFC_ExportTest_Tim";
            string outFileName = "ExportIfc.ifc";
            string completePath = Path.Combine(outFolder, outFileName);

            var cityGMLBuildingList = utils.getIfc2CityGMLGuidDic(doc);

            var dialog = new IfcExportDialog();
            dialog.ShowDialog();

            if (dialog.startExport)
            {
                RevitIfcExporter exporter = new RevitIfcExporter(doc);

                exporter.startRevitIfcExport(outFolder, outFileName, commandData);

                using (var model = IfcStore.Open(completePath))
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

            //RevitIfcExporter exporter = new RevitIfcExporter(doc);
            //exporter.startRevitIfcExport(outFolder, outFileName, commandData);

            //var guidDic = utils.getIfc2CityGMLGuidDic(doc);
            //exporter.addCityGMLAttributes(completePath, doc, guidDic);


            /*string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "City2BIM");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            TaskDialog.Show("Ifc Export", "Please use Revit Ifc-Exporter. For correct naming of georef property set please import" +
                " City2BIM_ParameterSet.txt in " + folder + " as user-defined Property Set in the IFC Exporter!");

            var ifcSem = new Builder.Revit_Semantic(commandData.Application.ActiveUIDocument.Document);
            ifcSem.CreateParameterSetFile();
            */
            return Result.Succeeded;
        }
    }
}
