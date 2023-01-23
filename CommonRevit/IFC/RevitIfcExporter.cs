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
        public enum ExportType
        {
            IfcSite, IfcGeographicElement
        }
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

        public void startRevitIfcExport(string outFilePath, ExternalCommandData commandData)
        {
            FilteredElementCollector topoCollector = new FilteredElementCollector(this.doc).OfCategory(BuiltInCategory.OST_Topography);
            var view = commandData.Application.ActiveUIDocument.ActiveView;
            List<ElementId> elementsToHide = topoCollector.Select(element => element.Id).ToList();

            using (TransactionGroup transGroup = new TransactionGroup(doc, "start default Revit export"))
            {
                transGroup.Start();

                if (elementsToHide.Count > 0)
                {
                    using (Transaction hideTrans = new Transaction(doc, "hide topography elements"))
                    {
                        hideTrans.Start();

                        view.HideElements(elementsToHide);

                        hideTrans.Commit();
                    }
                }


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

                if (elementsToHide.Count > 0)
                {
                    using (Transaction unHideTrans = new Transaction(doc, "unhide topo elements"))
                    {
                        unHideTrans.Start();
                        view.UnhideElements(elementsToHide);
                        unHideTrans.Commit();
                    }
                }

                transGroup.Commit();
            }
        }
        

        public void exportModelCurves(IfcStore model)
        {
            var elementIdList = new List<ElementId>();
            var htwSchemaList = SchemaUtils.getHTWSchemas();

            //Filter ModelCurves --> ModelLines and ModelArcs
            ElementClassFilter modelCurveFilter = new ElementClassFilter(typeof(CurveElement));

            foreach (var schema in htwSchemaList)
            {
                FilteredElementCollector modelCurveCollector = new FilteredElementCollector(this.doc);
                //Filter elements that have a HTWDD Schema entity associated with
                ExtensibleStorageFilter storageFilter = new ExtensibleStorageFilter(schema.GUID);

                LogicalAndFilter combinedFilter = new LogicalAndFilter(modelCurveFilter, storageFilter);

                elementIdList.AddRange(modelCurveCollector.WherePasses(combinedFilter).ToElementIds());
            }

            if (elementIdList.Count > 0)
            {
                Log.Information(string.Format("A total of {0} model lines are going to be exported", elementIdList.Count));
                foreach (var elementId in elementIdList)
                {
                    var modelCurve = this.doc.GetElement(elementId) as ModelCurve;
                    var schemattributes = SchemaUtils.getSchemaAttributesForElement(modelCurve);
                    var usageType = Schema.Lookup(modelCurve.GetEntitySchemaGuids().First()).SchemaName;

                    if (schemattributes.Count > 0)
                    {
                        IfcUtils.addIfcGeographicElementFromModelLine(model, modelCurve, usageType, schemattributes);
                    }
                    else
                    {
                        IfcUtils.addIfcGeographicElementFromModelLine(model, modelCurve, usageType);
                    }
                }

            }
        }
    }
}