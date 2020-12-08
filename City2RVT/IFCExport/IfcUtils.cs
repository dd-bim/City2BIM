using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Xbim.Ifc;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.GeometricConstraintResource;

namespace City2RVT.IFCExport
{
    class IfcUtils
    {
        public static IfcAxis2Placement3D getStandardAxis2Placement3D(IfcStore model)
        {
            var a2p3D = model.Instances.New<IfcAxis2Placement3D>();
            a2p3D.Location = model.Instances.New<IfcCartesianPoint>(point => point.SetXYZ(0.0, 0.0, 0.0));
            a2p3D.RefDirection = model.Instances.New<IfcDirection>(refDir => refDir.SetXYZ(1.0, 0.0, 0.0));
            a2p3D.Axis = model.Instances.New<IfcDirection>(axis => axis.SetXYZ(0.0, 0.0, 1.0));
            return a2p3D;
        }

        public static IfcPropertySet createPropertySetFromDict(IfcStore model, Dictionary<string, Dictionary<string, string>> attributes)
        {
            var propertySet = model.Instances.New<IfcPropertySet>(pSet =>
            {
                pSet.Name = attributes.Keys.First();

                foreach (var entry in attributes.Values)
                {
                    foreach (KeyValuePair<string, string> attr in entry)
                    {
                        pSet.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                        {
                            p.Name = attr.Key;
                            p.NominalValue = new IfcText(attr.Value);
                        }));
                    }
                }
            });

            return propertySet;
        }


        private static IfcTriangulatedFaceSet makeFaceSetFromRevitMesh(IfcStore model, List<XYZ> meshPoints, Mesh mesh)
        {
            var pointList = model.Instances.New<IfcCartesianPointList3D>(cpl =>
            {
                for (int i = 0; i < meshPoints.Count; i++)
                {
                    var index = cpl.CoordList.GetAt(i);
                    index.Add(meshPoints[i].X);
                    index.Add(meshPoints[i].Y);
                    index.Add(meshPoints[i].Z);
                }

            });

            var faceSet = model.Instances.New<IfcTriangulatedFaceSet>(fs =>
            {
                fs.Closed = false;
                fs.Coordinates = pointList;
            });

            for (int i = 0; i < mesh.NumTriangles; i++)
            {
                var index = faceSet.CoordIndex.GetAt(i);
                index.Add(mesh.get_Triangle(i).get_Index(0) + 1); //ifc starts counting at 1, Revit however at 0
                index.Add(mesh.get_Triangle(i).get_Index(1) + 1);
                index.Add(mesh.get_Triangle(i).get_Index(2) + 1);
            }

            return faceSet;
        }

        public static void addIfcGeographicElementFromMesh(IfcStore model, List<XYZ> meshPoints, Mesh mesh, string usageType, List<Dictionary<string, Dictionary<string, string>>> optionalProperties = null)
        {
            var faceSet = makeFaceSetFromRevitMesh(model, meshPoints, mesh);

            var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
            var shape = model.Instances.New<IfcShapeRepresentation>();
            shape.ContextOfItems = modelContext;
            //shape.RepresentationType = "Tesselation";
            shape.RepresentationIdentifier = "Body";
            shape.Items.Add(faceSet);

            var representation = model.Instances.New<IfcProductDefinitionShape>();
            representation.Representations.Add(shape);

            var geogElement = model.Instances.New<IfcGeographicElement>();
            geogElement.Representation = representation;

            var site = model.Instances.OfType<IfcSite>().FirstOrDefault();
            var siteObjPlcmt = site.ObjectPlacement;

            var localPlcmtGeogElement = model.Instances.New<IfcLocalPlacement>();
            localPlcmtGeogElement.PlacementRelTo = siteObjPlcmt;
            localPlcmtGeogElement.RelativePlacement = getStandardAxis2Placement3D(model);

            geogElement.ObjectPlacement = localPlcmtGeogElement;
            geogElement.Name = usageType;

            //add geog element to spatial structure at site level
            site.AddElement(geogElement);

            //add attributes if any
            if (optionalProperties != null && optionalProperties.Count > 0)
            {
                foreach (var attributeCollection in optionalProperties)
                {
                    var pSet = createPropertySetFromDict(model, attributeCollection);

                    var pSetRel = model.Instances.New<IfcRelDefinesByProperties>(r =>
                    {
                        r.RelatingPropertyDefinition = pSet;
                    });
                    pSetRel.RelatedObjects.Add(geogElement);
                }
            }

        }



    }
}
