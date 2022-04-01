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

using City2RVT.Calc;
using City2RVT.ExternalDataCatalog;

namespace City2RVT.IFCExport
{
    public class RevitIfcExporter
    {
        public enum ExportType
        {
            IfcSite, IfcGeographicElement
        }
        private Document doc { get; set; }

        private XbimEditorCredentials editor = new XbimEditorCredentials
        {
            ApplicationDevelopersName = "HTW Dresden",
            ApplicationFullName = "HTW Dresden City2RVT Plugin",
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

        public void createLoGeoRef50(IfcStore model)
        {
            var projectBasePoint = utils.getProjectBasePointMeter(this.doc);
            var projectAngleDeg = utils.getProjectAngleDeg(this.doc);
            var rotationVector = UTMcalc.AzimuthToVector(projectAngleDeg);

            var geomRepContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();

            var mapConversion = model.Instances.New<IfcMapConversion>();
            mapConversion.SourceCRS = geomRepContext;
            mapConversion.Eastings = projectBasePoint.X;
            mapConversion.Northings = projectBasePoint.Y;
            mapConversion.OrthogonalHeight = projectBasePoint.Z;
            mapConversion.XAxisAbscissa = rotationVector[0];
            mapConversion.XAxisOrdinate = rotationVector[1];
            mapConversion.Scale = 1.0;

            var projCRS = model.Instances.New<IfcProjectedCRS>();
            projCRS.Name = "EPSG:25832"; //TODO: make variable for EPSG-Code

            mapConversion.TargetCRS = projCRS;

        }

        public void exportDTM(IfcStore model)
        {
            ElementId terrainId = utils.getHTWDDTerrainID(this.doc);

            if (terrainId != null)
            {
                using (Transaction trans = new Transaction(this.doc, "export terrain"))
                {
                    trans.Start();
                    TopographySurface terrain = this.doc.GetElement(terrainId) as TopographySurface;
                    Mesh mesh = null;
                    Options options = new Options();
                    options.ComputeReferences = true;

                    IEnumerator<GeometryObject> terrainRepresentations = terrain.get_Geometry(options).GetEnumerator();

                    while (terrainRepresentations.MoveNext())
                    {
                        var geoObj = terrainRepresentations.Current; //as GeometryObject;

                        if (geoObj is Mesh)
                        {
                            mesh = geoObj as Mesh;
                            break;
                        }
                    }

                    if (mesh == null)
                    {
                        return;
                    }

                    var nrOfTriangles = mesh.NumTriangles;
                    var meshPoints = mesh.Vertices;

                    var meshPointsMeter = meshPoints.Select(point => point.Multiply(0.3048)).ToList();

                    IfcUtils.addIfcGeographicElementFromMesh(model, meshPointsMeter, mesh, "DTM");

                    trans.Commit();
                }
            }
        }

        public void exportSurfaces(IfcStore model, ExportType exportType, IfcSite referenceSite)
        {
            ElementId terrainId = utils.getHTWDDTerrainID(this.doc);
            FilteredElementCollector topoCollector = default;
            
            if (terrainId != null )
            {
                ElementId[] excludeIds = { terrainId };
                topoCollector = new FilteredElementCollector(this.doc).OfCategory(BuiltInCategory.OST_Topography).Excluding(excludeIds);
            }
            else
            {
                topoCollector = new FilteredElementCollector(this.doc).OfCategory(BuiltInCategory.OST_Topography);
            }
            

            if (topoCollector.GetElementCount() == 0)
            {
                return;
            }

            using (Transaction trans = new Transaction(this.doc, "export remaining surfaces"))
            {
                trans.Start();

                foreach (var topoSurf in topoCollector)
                {
                    TopographySurface importedObj = topoSurf as TopographySurface;

                    if (importedObj.IsSiteSubRegion == false)
                    {
                        continue;
                    }

                    Mesh mesh = null;
                    Options options = new Options();
                    options.ComputeReferences = true;

                    IEnumerator<GeometryObject> terrainRepresentations = importedObj.get_Geometry(options).GetEnumerator();

                    while (terrainRepresentations.MoveNext())
                    {
                        var geoObj = terrainRepresentations.Current; //as GeometryObject;

                        if (geoObj is Mesh)
                        {
                            mesh = geoObj as Mesh;
                            break;
                        }
                    }

                    if (mesh == null)
                    {
                        continue;
                    }

                    var usageType = Schema.Lookup(importedObj.GetEntitySchemaGuids().First()).SchemaName;

                    var meshPoints = mesh.Vertices;
                    var meshPointsMeter = meshPoints.Select(point => point.Multiply(0.3048)).ToList();

                    var attributes = utils.getSchemaAttributesForElement(importedObj);

                    switch (exportType)
                    {
                        case ExportType.IfcGeographicElement:
                            IfcUtils.addIfcGeographicElementFromMesh(model, meshPointsMeter, mesh, usageType, optionalProperties: attributes);
                            break;
                        case ExportType.IfcSite:
                            IfcUtils.addIfcSiteFromMesh(model, meshPointsMeter, mesh, usageType, referenceSite, optionalProperties: attributes);
                            break;
                    }
                }
                trans.Commit();
            }
        }

        public void addCityGMLAttributes(IfcStore model, Document doc, IDictionary<string, string> ifc2CityGMLGuidDic)
        {
            //retrieve ifc to revit id dictionary
            //IDictionary<string, string> ifc2CityGMLGuidDic = utils.getIfc2CityGMLGuidDic(doc);
            var CityGMLImportSchema = utils.getSchemaByName("CityGMLImportSchema");

            foreach (KeyValuePair<string, string> entry in ifc2CityGMLGuidDic)
            {
                Element revitElement = doc.GetElement(entry.Value);
                Entity bpEntity = revitElement.GetEntity(CityGMLImportSchema);
                if (bpEntity.IsValid())
                {
                    var buildingProxy = model.Instances.FirstOrDefault<IfcBuildingElementProxy>(d => d.GlobalId == entry.Key);

                    //create new propertiy set with attributes
                    //read attributes from revit extensible storage
                    var pSetRel = model.Instances.New<IfcRelDefinesByProperties>(r =>
                    {
                        r.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pSet =>
                        {
                            pSet.Name = "CityGML Attributes";

                            foreach (var field in CityGMLImportSchema.ListFields())
                            {
                                var value = bpEntity.Get<string>(field);
                                if (value != "")
                                {
                                    pSet.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = field.FieldName;
                                        p.NominalValue = new IfcText(value);
                                    }));
                                }
                            }
                        });
                    });
                    pSetRel.RelatedObjects.Add(buildingProxy);
                }
            }
        }

        public void addExternalData(IfcStore model, Document doc)
        {
            var externalDataSchema = utils.getSchemaByName("ExternalDataCatalogSchema");

            if (externalDataSchema == null)
            {
                return;
            }

            Dictionary<string, string> ifc2ExternalDataGuidDic = ExternalDataUtils.getIfc2ExternalDataGuidDic(doc);

            if (ifc2ExternalDataGuidDic.Count < 1)
            {
                return;
            }

            addIfcClassifications(model, doc, ifc2ExternalDataGuidDic);

            foreach (KeyValuePair<string,string> entry in ifc2ExternalDataGuidDic)
            {
                Element revitElement = doc.GetElement(entry.Value);
                Entity bpEntity = revitElement.GetEntity(externalDataSchema);

                if (bpEntity.IsValid())
                {
                    var ifcEntity = model.Instances.FirstOrDefault<IfcProduct>(p => p.GlobalId == entry.Key);

                    //create new property set with attributes
                    //read attributes from revit extensible storage

                    //var dict = bpEntity.Get<IDictionary<string, string>>("data");
                    var schemaObjList = bpEntity.Get<IDictionary<string, string>>("Objects");

                    foreach (KeyValuePair<string, string> externalObjContainer in schemaObjList)
                    {
                        var schemaObject = JsonConvert.DeserializeObject<ExternalDataSchemaObject>(externalObjContainer.Value);

                        var pSetRel = model.Instances.New<IfcRelDefinesByProperties>(r =>
                        {
                            r.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pSet =>
                            {
                                pSet.Name = schemaObject.ObjectType;

                                foreach (KeyValuePair<string, string> property in schemaObject.Properties)
                                {
                                    pSet.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                                    {
                                        p.Name = property.Key;
                                        p.NominalValue = new IfcText(property.Value);
                                    }));
                                }
                            });
                        });
                        pSetRel.RelatedObjects.Add(ifcEntity);
                    }
                }
            }
        }

        private void addIfcClassifications(IfcStore model, Document doc, Dictionary<string, string> ifc2ExternalDataGuidDic)
        {
            var externalDataSchema = utils.getSchemaByName("ExternalDataCatalogSchema");
            List<IfcClassification> classList = new List<IfcClassification>();
            List<IfcClassificationReference> refClassList = new List<IfcClassificationReference>();

            List<Autodesk.Revit.DB.ExtensibleStorage.Entity> entityList = new List<Entity>();
            List<IfcClassification> classifications = new List<IfcClassification>();
            List<IfcClassificationReference> classRefs = new List<IfcClassificationReference>();
            Dictionary<string, List<string>> IfcClassRefId2RevitUniqueId = new Dictionary<string, List<string>>();

            foreach (KeyValuePair<string, string> entry in ifc2ExternalDataGuidDic)
            {
                var revitEntity = doc.GetElement(entry.Value).GetEntity(externalDataSchema);
                entityList.Add(revitEntity);

                var schemaObjects = revitEntity.Get<IDictionary<string, string>>("Objects");
                foreach (KeyValuePair<string, string> objectTypeAndObject in schemaObjects)
                {
                    var schemaObject = JsonConvert.DeserializeObject<ExternalDataSchemaObject>(objectTypeAndObject.Value);
                    classifications.Add(schemaObject.IfcClassification);
                    foreach (var classRef in schemaObject.IfcClassification.RefList)
                    {
                        classRefs.Add(classRef);

                        if (IfcClassRefId2RevitUniqueId.ContainsKey(classRef.ID))
                        {
                            IfcClassRefId2RevitUniqueId[classRef.ID].Add(entry.Value);
                        }
                        else
                        {
                            IfcClassRefId2RevitUniqueId.Add(classRef.ID, new List<string> { entry.Value });
                        }
                    }
                }
            }

            var uniqueClassifications = classifications.GroupBy(c => c.ID).Select(c => c.First()).ToList();
            var uniqueClassificationReferences = classRefs.GroupBy(cf => cf.ID).Select(cf => cf.First()).ToList();

            Dictionary<string, List<string>> hierarchy = new Dictionary<string, List<string>>();

            foreach (var entry in uniqueClassificationReferences)
            {
                var key = entry.ReferencedSource;
                var value = entry.ID;

                if (hierarchy.ContainsKey(key))
                {
                    hierarchy[key].Add(value);
                }
                else
                {
                    hierarchy.Add(key, new List<string> { value });
                }
            }

            //loop through keys -> new ifclcassification for each entry
            foreach (KeyValuePair<string, List<string>> entry in hierarchy)
            {
                var topReference = model.Instances.New<Xbim.Ifc4.ExternalReferenceResource.IfcClassification>(cls =>
                {
                    cls.Name = uniqueClassifications.Where(c => c.ID == entry.Key).First().Name;
                    cls.Source = uniqueClassifications.Where(c => c.ID == entry.Key).First().Source;
                    cls.Location = uniqueClassifications.Where(c => c.ID == entry.Key).First().Location;
                    cls.Edition = uniqueClassifications.Where(c => c.ID == entry.Key).First().Edition;
                    cls.EditionDate = uniqueClassifications.Where(c => c.ID == entry.Key).First().EditionDate;
                });

                

                foreach (var classRef in entry.Value)
                {
                    var ifcClassRef = model.Instances.New<Xbim.Ifc4.ExternalReferenceResource.IfcClassificationReference>(err =>
                    {
                        err.Name = uniqueClassificationReferences.Where(cr => cr.ID == classRef).First().Name;
                        err.Location = uniqueClassificationReferences.Where(cr => cr.ID == classRef).First().Location;
                        err.ReferencedSource = topReference;
                    });

                    var affectedRevitUniqueIds = IfcClassRefId2RevitUniqueId[classRef];
                    List<Xbim.Ifc4.Kernel.IfcObjectDefinition> affectedIfcObjects = new List<IfcObjectDefinition>();

                    foreach (var element in affectedRevitUniqueIds)
                    {
                        Guid cSharpGuid = ExportUtils.GetExportId(doc, doc.GetElement(element).Id);
                        string ifcGuid = IfcGuid.ToIfcGuid(cSharpGuid);
                        affectedIfcObjects.Add(model.Instances.FirstOrDefault<IfcObjectDefinition>(o => o.GlobalId == ifcGuid));
                    }

                    var mapping = model.Instances.New<Xbim.Ifc4.Kernel.IfcRelAssociatesClassification>(relClass =>
                    {
                        relClass.RelatingClassification = ifcClassRef;
                        relClass.RelatedObjects.AddRange(affectedIfcObjects);
                    });
                }
            }
        }

        public void exportModelCurves(IfcStore model)
        {
            var elementIdList = new List<ElementId>();
            var htwSchemaList = utils.getHTWSchemas(this.doc);

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
                    var schemattributes = utils.getSchemaAttributesForElement(modelCurve);
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
