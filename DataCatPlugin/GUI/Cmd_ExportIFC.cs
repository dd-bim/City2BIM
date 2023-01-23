using System;
using System.IO;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Xbim.Ifc;
using Xbim.Ifc4.ProductExtension;
using Serilog;

using CommonRevit.IFC;
using DataCatPlugin.GUI;
using DataCatPlugin.IFCExport;

namespace DataCatPlugin.GUI
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

            var dialog = new IfcExportDialog();
            dialog.ShowDialog();

            if (dialog.startExport)
            {
                var exporter = new ExternalDataIfcExporter(doc);

                exporter.startRevitIfcExport(dialog.ExportPath, commandData);

                Log.Information("start IFC export");

                using (var model = IfcStore.Open(dialog.ExportPath))
                {
                    using (var txn = model.BeginTransaction("Edit standard Revit export"))
                    {
                        
                        var site = model.Instances.OfType<IfcSite>().FirstOrDefault();

                        exporter.addExternalData(model, doc);

                        txn.Commit();
                    }
                    model.SaveAs(dialog.ExportPath);
                }

                Log.Information("finished custom IFC export");
                TaskDialog.Show("Information", "IFC export finished!");
            }

            return Result.Succeeded;
        }
    }
}
