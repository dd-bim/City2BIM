using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Xbim.Ifc;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.PropertyResource;
using Newtonsoft.Json;

using CommonRevit.Semantics;
using DataCatPlugin.ExternalDataCatalog;
using CommonRevit.IFC;
using Xbim.Ifc4.MeasureResource;

namespace DataCatPlugin.IFCExport
{
    internal class ExternalDataIfcExporter : RevitIfcExporter
    {
        
        public ExternalDataIfcExporter(Document doc) : base(doc)
        {
            
        }

        public void addExternalData(IfcStore model, Document doc)
        {
            var externalDataSchema = SchemaUtils.getSchemaByName("ExternalDataCatalogSchema");

            if (externalDataSchema == null)
            {
                return;
            }

            Dictionary<string, string> ifc2ExternalDataGuidDic = getIfc2ExternalDataGuidDic();

            if (ifc2ExternalDataGuidDic.Count < 1)
            {
                return;
            }

            addIfcClassifications(model, doc, ifc2ExternalDataGuidDic);

            foreach (KeyValuePair<string, string> entry in ifc2ExternalDataGuidDic)
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

        private Dictionary<string, string> getIfc2ExternalDataGuidDic()
        {
            Dictionary<string, string> IfcToRevitDic = new Dictionary<string, string>();
            List<Element> elements = new List<Element>();
            var externalDataSchema = SchemaUtils.getSchemaByName("ExternalDataCatalogSchema");

            FilteredElementCollector collector = new FilteredElementCollector(this.doc);
            collector.WherePasses(new ExtensibleStorageFilter(externalDataSchema.GUID));

            elements.AddRange(collector.ToElements());

            foreach (var element in elements)
            {
                Guid cSharpGuid = ExportUtils.GetExportId(doc, element.Id);
                string ifcGuid = IfcGuid.ToIfcGuid(cSharpGuid);
                IfcToRevitDic.Add(ifcGuid, element.UniqueId);
            }

            return IfcToRevitDic;

        }

        private void addIfcClassifications(IfcStore model, Document doc, Dictionary<string, string> ifc2ExternalDataGuidDic)
        {
            var externalDataSchema = SchemaUtils.getSchemaByName("ExternalDataCatalogSchema");
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
    }


}
