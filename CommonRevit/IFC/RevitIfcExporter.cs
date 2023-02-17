using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using Xbim.Ifc;
using Xbim.Ifc4;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.ProductExtension;
using Newtonsoft.Json;
using Serilog;

using CommonRevit.Semantics;

namespace CommonRevit.IFC
{

    public class RevitIfcExporter
    {
        
        public Document doc { get; protected set; }

        private XbimEditorCredentials editor = new XbimEditorCredentials
        {
            ApplicationDevelopersName = "HTW Dresden",
            ApplicationFullName = "HTW Dresden Plugin",
            ApplicationIdentifier = "htwdd",
            ApplicationVersion = "1.0.0",
            EditorsOrganisationName = "HTW Dresden"
        };

        public RevitIfcExporter(Document doc)
        {
            this.doc = doc;

            //setting unit to meters --> for ifc export
            // TODO check if that is working as expected
            /*
            using (Transaction trans = new Transaction(this.doc, "set units to meter"))
            {
                trans.Start();
                var units = this.doc.GetUnits();
                FormatOptions format = new FormatOptions(DisplayUnitType.DUT_METERS);
                units.SetFormatOptions(UnitType.UT_Length, format);
                doc.SetUnits(units);
                trans.Commit();
            }
            */

        }

        public virtual void startRevitIfcExport(string outFilePath, ExternalCommandData commandData)
        {
            var view = commandData.Application.ActiveUIDocument.ActiveView;

        
            using (Transaction exportTrans = new Transaction(doc, "default export"))
            {
                exportTrans.Start();

                IFCExportOptions IFCOptions = new IFCExportOptions();
                IFCOptions.FileVersion = IFCVersion.IFC4;
                IFCOptions.FilterViewId = view.Id;
                IFCOptions.AddOption("ActiveViewId", view.Id.ToString());
                IFCOptions.AddOption("ExportVisibleElementsInView", "true");
                IFCOptions.AddOption("VisibleElementsOfCurrentView", "true");

                Log.Information("Startet Revit IFC-Export");
                doc.Export(Path.GetDirectoryName(outFilePath), Path.GetFileName(outFilePath), IFCOptions);


                exportTrans.Commit();
            }

            Log.Information("Finished Revit IFC-Export");

            }

    }
}