using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Autodesk.Revit.DB;
using Xbim.Ifc;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.TopologyResource;

namespace CommonRevit.IFC
{
    internal class IfcUtils
    {
        public static IfcAxis2Placement3D getStandardAxis2Placement3D(IfcStore model)
        {
            var a2p3D = model.Instances.New<IfcAxis2Placement3D>();
            a2p3D.Location = model.Instances.New<IfcCartesianPoint>(point => point.SetXYZ(0.0, 0.0, 0.0));
            //a2p3D.RefDirection = model.Instances.New<IfcDirection>(refDir => refDir.SetXYZ(1.0, 0.0, 0.0));
            //a2p3D.Axis = model.Instances.New<IfcDirection>(axis => axis.SetXYZ(0.0, 0.0, 1.0));
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

        public static void addIfcSiteFromMesh(IfcStore model, List<XYZ> meshPoints, Mesh mesh, string usageType, IfcSite referenceSite, List<Dictionary<string, Dictionary<string, string>>> optionalProperties = default)
        {
            var pds = model.Instances.New<IfcProductDefinitionShape>();

            var geomRepContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();

            var sbsm = createShellBasedSurfaceModelFromMesh(model, mesh);
            var shapeRepShellBasedSurfaceModel = model.Instances.New<IfcShapeRepresentation>();
            shapeRepShellBasedSurfaceModel.RepresentationIdentifier = new IfcLabel("Body");
            shapeRepShellBasedSurfaceModel.RepresentationType = new IfcLabel("SurfaceModel");
            shapeRepShellBasedSurfaceModel.Items.Add(sbsm);
            shapeRepShellBasedSurfaceModel.ContextOfItems = geomRepContext;


            var geomCurveSet = createIfcGeometricCurveSetFromMesh(model, mesh);
            var shapeRepCurve = model.Instances.New<IfcShapeRepresentation>();
            shapeRepCurve.RepresentationIdentifier = new IfcLabel("SurveyPoints");
            shapeRepCurve.RepresentationType = new IfcLabel("GeometricCurveSet");
            shapeRepCurve.Items.Add(geomCurveSet);
            shapeRepCurve.ContextOfItems = geomRepContext;

            pds.Representations.Add(shapeRepShellBasedSurfaceModel);
            pds.Representations.Add(shapeRepCurve);

            var siteObjPlcmt = model.Instances.New<IfcLocalPlacement>();
            siteObjPlcmt.RelativePlacement = getStandardAxis2Placement3D(model);
            siteObjPlcmt.PlacementRelTo = referenceSite.ObjectPlacement;

            var site = model.Instances.New<IfcSite>();
            site.Representation = pds;
            site.ObjectPlacement = siteObjPlcmt;
            site.Name = usageType;

            referenceSite.AddSite(site);

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
                    pSetRel.RelatedObjects.Add(site);
                }
            }
        }

        private static IfcGeometricCurveSet createIfcGeometricCurveSetFromMesh(IfcStore model, Mesh mesh)
        {
            var meshPointsMeter = mesh.Vertices.Select(p => p.Multiply(0.3048)).ToList();
            var cartesianPointList = meshPointsMeter.Select(p => model.Instances.New<IfcCartesianPoint>(c => c.SetXYZ(p.X, p.Y, p.Z))).ToList();

            var gcs = model.Instances.New<IfcGeometricCurveSet>(g =>
            {
                g.Elements.AddRange(cartesianPointList);
            });

            return gcs;

        }

        private static IfcShellBasedSurfaceModel createShellBasedSurfaceModelFromMesh(IfcStore model, Mesh mesh)
        {
            int nrOfTriangles = mesh.NumTriangles;

            var openShell = model.Instances.New<IfcOpenShell>();


            for (int i = 0; i < nrOfTriangles; i++)
            {
                var triangle = mesh.get_Triangle(i);

                var loop = model.Instances.New<IfcPolyLoop>(l =>
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var pointMeter = triangle.get_Vertex(j).Multiply(0.3048);
                        l.Polygon.Add(model.Instances.New<IfcCartesianPoint>(cp => cp.SetXYZ(pointMeter.X, pointMeter.Y, pointMeter.Z)));
                    }
                });

                var face = model.Instances.New<IfcFace>(f =>
                {
                    f.Bounds.Add(model.Instances.New<IfcFaceOuterBound>(b =>
                    {
                        b.Bound = loop;
                    }));
                });
                openShell.CfsFaces.Add(face);
            }

            var sbsm = model.Instances.New<IfcShellBasedSurfaceModel>(s =>
            {
                s.SbsmBoundary.Add(openShell);
            });
            return sbsm;
        }

        private static IfcCurve createIfcCurveFromModelLine(IfcStore model, ModelCurve modelCurve)
        {
            //check if modelCurve is straigth modelline
            if (typeof(ModelLine).IsInstanceOfType(modelCurve))
            {
                var modelLine = modelCurve as ModelLine;
                XYZ startPointRevit = modelLine.GeometryCurve.GetEndPoint(0).Multiply(0.3048); //convert from feet to meter
                XYZ endPointRevit = modelLine.GeometryCurve.GetEndPoint(1).Multiply(0.3048);

                var startPoint = model.Instances.New<IfcCartesianPoint>();
                startPoint.SetXYZ(startPointRevit.X, startPointRevit.Y, startPointRevit.Z);
                var endPoint = model.Instances.New<IfcCartesianPoint>();
                endPoint.SetXYZ(endPointRevit.X, endPointRevit.Y, endPointRevit.Z);

                var ifcPolyLine = model.Instances.New<IfcPolyline>();
                ifcPolyLine.Points.AddRange(new IfcCartesianPoint[] { startPoint, endPoint });

                return ifcPolyLine;
            }

            //check if modelCurve is arc --> kurvengeometrie
            else if (typeof(ModelArc).IsInstanceOfType(modelCurve))
            {
                var arc = modelCurve as ModelArc;

                var startPointRevit = arc.GeometryCurve.GetEndPoint(0).Multiply(0.3048);
                var endPointRevit = arc.GeometryCurve.GetEndPoint(1).Multiply(0.3048);
                var midPointRevit = arc.GeometryCurve.Evaluate(0.5, true).Multiply(0.3048);

                var coordinates = model.Instances.New<IfcCartesianPointList3D>();
                coordinates.CoordList.GetAt(0).AddRange(new List<IfcLengthMeasure> { startPointRevit.X, startPointRevit.Y, startPointRevit.Z });
                coordinates.CoordList.GetAt(1).AddRange(new List<IfcLengthMeasure> { midPointRevit.X, midPointRevit.Y, midPointRevit.Z });
                coordinates.CoordList.GetAt(2).AddRange(new List<IfcLengthMeasure> { endPointRevit.X, endPointRevit.Y, endPointRevit.Z });

                var indexList = new List<IfcPositiveInteger> { 1, 2, 3 };
                var arcIndex = new IfcArcIndex(indexList);

                var ipc = model.Instances.New<IfcIndexedPolyCurve>();
                ipc.Points = coordinates;
                ipc.Segments.Add(arcIndex);

                return ipc;
            }

            return null;

        }

        public static void addIfcGeographicElementFromModelLine(IfcStore model, ModelCurve modelCurve, string usageType, List<Dictionary<string, Dictionary<string, string>>> optionalProperties = null)
        {
            var curve = createIfcCurveFromModelLine(model, modelCurve);

            var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
            var shape = model.Instances.New<IfcShapeRepresentation>();
            shape.ContextOfItems = modelContext;
            shape.RepresentationType = "Curve3D";
            shape.RepresentationIdentifier = "Body";
            shape.Items.Add(curve);

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

    internal static class IfcGuid
    {
        #region Static Fields

        /// <summary>
        ///     The replacement table
        /// </summary>
        private static readonly char[] Base64Chars =
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C',
                'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
                'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c',
                'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
                'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '_', '$'
            };

        #endregion

        /// <summary>
        /// The reverse function to calculate the number from the characters
        /// </summary>
        /// <param name="str">
        /// The char array to convert from
        /// </param>
        /// <param name="start">
        /// Position in array to start read
        /// </param>
        /// <param name="len">
        /// The length to read
        /// </param>
        /// <returns>
        /// The calculated nuber
        /// </returns>
        public static uint CvFrom64(char[] str, int start, int len)
        {
            int i;
            uint res = 0;

            Debug.Assert(len <= 4, "Length must be equal or lett than 4");

            for (i = 0; i < len; i++)
            {
                int index = -1;
                int j;
                for (j = 0; j < 64; j++)
                {
                    if (Base64Chars[j] == str[start + i])
                    {
                        index = j;
                        break;
                    }
                }

                Debug.Assert(index >= 0, "Index is less than 0");

                res = (res * 64) + ((uint)index);
            }

            return res;
        }

        /// <summary>
        /// Conversion of an integer into a characters with base 64
        ///     using the table Base64Chars
        /// </summary>
        /// <param name="number">
        /// The number to convert
        /// </param>
        /// <param name="result">
        /// The result char array to write to
        /// </param>
        /// <param name="start">
        /// The position in the char array to start writing
        /// </param>
        /// <param name="len">
        /// The length to write
        /// </param>
        public static void CvTo64(uint number, ref char[] result, int start, int len)
        {
            int digit;

            Debug.Assert(len <= 4, "Length must be equal or lett than 4");

            uint act = number;
            int digits = len;

            for (digit = 0; digit < digits; digit++)
            {
                result[start + len - digit - 1] = Base64Chars[(int)(act % 64)];
                act /= 64;
            }

            Debug.Assert(act == 0, "Logic failed, act was not null: " + act);
        }

        /// <summary>
        /// Reconstruction of the GUID from an IFC GUID string (base64)
        /// </summary>
        /// <param name="guid">
        /// The GUID string to convert. Must be 22 characters long
        /// </param>
        /// <returns>
        /// GUID correspondig to the string
        /// </returns>
        public static Guid FromIfcGuid(string guid)
        {
            Debug.Assert(guid.Length == 22, "Input string must not be longer that 22 chars");
            var num = new uint[6];
            char[] str = guid.ToCharArray();
            int n = 2, pos = 0, i;
            for (i = 0; i < 6; i++)
            {
                num[i] = CvFrom64(str, pos, n);
                pos += n;
                n = 4;
            }

            var a = (int)((num[0] * 16777216) + num[1]);
            var b = (short)(num[2] / 256);
            var c = (short)(((num[2] % 256) * 256) + (num[3] / 65536));
            var d = new byte[8];
            d[0] = Convert.ToByte((num[3] / 256) % 256);
            d[1] = Convert.ToByte(num[3] % 256);
            d[2] = Convert.ToByte(num[4] / 65536);
            d[3] = Convert.ToByte((num[4] / 256) % 256);
            d[4] = Convert.ToByte(num[4] % 256);
            d[5] = Convert.ToByte(num[5] / 65536);
            d[6] = Convert.ToByte((num[5] / 256) % 256);
            d[7] = Convert.ToByte(num[5] % 256);

            return new Guid(a, b, c, d);
        }

        /// <summary>
        /// Conversion of a GUID to a string representing the GUID
        /// </summary>
        /// <param name="guid">
        /// The GUID to convert
        /// </param>
        /// <returns>
        /// IFC (base64) encoded GUID string
        /// </returns>
        public static string ToIfcGuid(Guid guid)
        {
            var num = new uint[6];
            var str = new char[22];
            byte[] b = guid.ToByteArray();

            // Creation of six 32 Bit integers from the components of the GUID structure
            num[0] = BitConverter.ToUInt32(b, 0) / 16777216;
            num[1] = BitConverter.ToUInt32(b, 0) % 16777216;
            num[2] = (uint)((BitConverter.ToUInt16(b, 4) * 256) + (BitConverter.ToUInt16(b, 6) / 256));
            num[3] = (uint)(((BitConverter.ToUInt16(b, 6) % 256) * 65536) + (b[8] * 256) + b[9]);
            num[4] = (uint)((b[10] * 65536) + (b[11] * 256) + b[12]);
            num[5] = (uint)((b[13] * 65536) + (b[14] * 256) + b[15]);

            // Conversion of the numbers into a system using a base of 64
            int n = 2;
            int pos = 0;
            for (int i = 0; i < 6; i++)
            {
                CvTo64(num[i], ref str, pos, n);
                pos += n;
                n = 4;
            }

            return new string(str);
        }
    }

}

