using System;
using System.IO;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Xbim.Ifc;

using City2RVT.IFCExport;
using City2RVT;

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

            RevitIfcExporter exporter = new RevitIfcExporter(doc);

            exporter.startRevitIfcExport(outFolder, outFileName, commandData);

            using (var model = IfcStore.Open(completePath))
            {
                using (var txn = model.BeginTransaction("Edit standard Revit export"))
                {
                    exporter.createLoGeoRef50(model);
                    exporter.exportDTM(model);
                    exporter.exportSurfaces(model);
                    
                    if (cityGMLBuildingList.Count > 0)
                    {
                        exporter.addCityGMLAttributes(model, doc, cityGMLBuildingList);
                    }
                    txn.Commit();
                }
                model.SaveAs(completePath);
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
