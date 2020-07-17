﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NETGeographicLib;
//using System.Windows.Media.Media3D;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.PresentationAppearanceResource;
using System.Windows.Forms;
using Xbim.Ifc4.TopologyResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.MaterialResource;
using System.Xml;
//using CoordIndex = System.Tuple<int, int, int>;

using City2RVT.Calc;
using Xbim.Ifc4.ActorResource;
using Xbim.Ifc4.QuantityResource;
using System.Globalization;

namespace City2RVT.GUI.XPlan2BIM
{
    public class IfcXBim
    {
        private static double feetToMeter = 1.0 / 0.3048;

        /// <summary>
        /// Initalizes a new IFC Model (currently not used, because topography is loaded to an already existing IFC file).  
        /// </summary>

        /// <summary>
        /// Dictionary for rgb definition for different categories.  
        /// </summary>
        public static Dictionary<string, string> CreateColors()
        {
            var colorDict = new Dictionary<string, string>();

            colorDict.Add("BP_StrassenVerkehrsFlaeche", "220/220/220");
            colorDict.Add("BP_GewaesserFlaeche", "100/149/237");
            colorDict.Add("BP_UeberbaubareGrundstuecksFlaeche", "160/082/045");
            colorDict.Add("BP_Bereich", "0/0/0");
            colorDict.Add("BP_Plan", "0/0/0");
            colorDict.Add("BP_BaugebietsTeilFlaeche", "233/150/122");
            colorDict.Add("BP_GemeinbedarfsFlaeche", "255/106/106");
            colorDict.Add("BP_KennzeichnungsFlaeche", "110/139/061");
            colorDict.Add("BP_ErhaltungsBereichFlaeche", "0/255/0");

            return colorDict;
        }

        /// <summary>
        /// Get values for project properties from Revit project information and saves them to the ifc file. 
        /// </summary>
        public void getProjectProperties(IfcPropertySet pset, IfcStore model, Document doc, string name)
        {
            pset.HasProperties.AddRange(new[]
                            {
                                model.Instances.New<IfcPropertySingleValue>(p =>
                                    {
                                        Parameter projInfoParam = doc.ProjectInformation.LookupParameter(name);
                                        string projInfoParamValue = projInfoParam.AsString();
                                        string rfaNameAttri = name;
                                        p.Name = rfaNameAttri;
                                        p.NominalValue = new IfcLabel(projInfoParamValue);
                                    }),
                            });
        }

        public void getProjectEnums(IfcPropertySet pset, IfcStore model, Document doc, string name, List<string>enums)
        {
            pset.HasProperties.AddRange(new[]
                {
                model.Instances.New<IfcPropertyEnumeratedValue>(pev =>
                    {
                        Parameter projInfoParam = doc.ProjectInformation.LookupParameter(name);

                        pev.EnumerationReference = model.Instances.New<IfcPropertyEnumeration>(pe =>
                        {
                            pe.Name = name + "_enum";
                            foreach (var enu in enums)
                            {
                                pe.EnumerationValues.Add(new IfcLabel(enu));
                            }

                            string rfaNameAttri = name;
                            pev.Name = rfaNameAttri;

                            if (pe.EnumerationValues.Contains(new IfcLabel(projInfoParam.AsString())))
                            {
                                string projInfoParamValue = projInfoParam.AsString();
                                pev.EnumerationValues.Add(new IfcLabel(projInfoParamValue));
                            }
                            else
                            {
                                //TaskDialog.Show("Warning","Please select an permitted value for '" + name + "'. Value for '" + projInfoParam.AsString() + "' is set to '-'. " +
                                //    "See 'Modellierungsrichtlinie für den BIM-basierten Bauantrag ZUKUNFT BAU' for further information. ");
                                //string projInfoParamValue = "-";
                                //pev.EnumerationValues.Add(new IfcLabel(projInfoParamValue));
                            }
                        });                                    
                    }),
            });
        }

        /// <summary>
        /// Get values for surface properties from Revit project information and saves them to the ifc file. 
        /// </summary>
        public void setSurfaceProperties(IfcPropertySet pset, IfcStore model, KeyValuePair<string,string> topoParam)
        {
            pset.HasProperties.AddRange(new[]
{
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    string rfaNameAttri = topoParam.Key.Substring(topoParam.Key.LastIndexOf(':') + 1);
                                    p.Name = rfaNameAttri;
                                    p.NominalValue = new IfcText(topoParam.Value);
                                }),
                            });
        }

        private static void CreateSiteQuantity(IfcStore model, IfcSite site, TopographySurface topoSurf)
        {
            //Create a IfcElementQuantity
            //first we need a IfcPhysicalSimpleQuantity,first will use IfcQuantityLength
            var ifcQuantityArea = model.Instances.New<IfcQuantityArea>(qa =>
            {
                qa.Name = "GrossArea";
                qa.Description = "";
                qa.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.AREAUNIT;
                    //siu.Prefix = IfcSIPrefix.MILLI;
                    siu.Name = IfcSIUnitName.SQUARE_METRE;
                });
                var area = topoSurf.get_Parameter(BuiltInParameter.PROJECTED_SURFACE_AREA).AsValueString();
                string[] areaSplit = area.Split(' ');
                string areaWithoutUnit = areaSplit[0];
                qa.AreaValue = Convert.ToDouble(areaWithoutUnit,CultureInfo.InvariantCulture);

            });

            //lets create the IfcElementQuantity
            var ifcElementQuantity = model.Instances.New<IfcElementQuantity>(eq =>
            {
                eq.Name = "Qto_SiteBaseQuantities";
                eq.Description = "Measurement quantity";
                eq.Quantities.Add(ifcQuantityArea);
            });

            //need to create the relationship
            model.Instances.New<IfcRelDefinesByProperties>(rdbp =>
            {
                rdbp.Name = "Qto_SiteBaseQuantities";
                rdbp.Description = "";
                rdbp.RelatedObjects.Add(site);
                rdbp.RelatingPropertyDefinition = ifcElementQuantity;
            });
        }

        public static IfcQuantityArea AreaSpaceQuantity(IfcStore model, double value, string qsetName, string qName)
        {
            //Create a IfcElementQuantity
            //first we need a IfcPhysicalSimpleQuantity,first will use IfcQuantityLength
            var ifcQuantityArea = model.Instances.New<IfcQuantityArea>(qa =>
            {
                qa.Name = qName;
                qa.Description = "";
                qa.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.AREAUNIT;
                    //siu.Prefix = IfcSIPrefix.MILLI;
                    siu.Name = IfcSIUnitName.SQUARE_METRE;

                });
                double areaWithoutUnitDouble = value;
                qa.AreaValue = areaWithoutUnitDouble;
            });
            //lets create the IfcElementQuantity
            var ifcElementQuantity = model.Instances.New<IfcElementQuantity>(eq =>
            {
                eq.Name = qsetName;
                eq.Description = "Measurement area quantity";
                eq.Quantities.Add(ifcQuantityArea);
            });
            return ifcQuantityArea;            
        }

        public static IfcQuantityLength HeightSpaceQuantity(IfcStore model, double value, string qsetName, string qName)
        {
            //Create a IfcElementQuantity
            //first we need a IfcPhysicalSimpleQuantity,first will use IfcQuantityLength
            var ifcQuantityHeight = model.Instances.New<IfcQuantityLength>(qa =>
            {
                qa.Name = qName;
                qa.Description = "";
                qa.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.LENGTHUNIT;
                    siu.Name = IfcSIUnitName.METRE;

                });
                double areaWithoutUnitDouble = value;
                qa.LengthValue = areaWithoutUnitDouble;
            });
            var ifcElementQuantity = model.Instances.New<IfcElementQuantity>(eq =>
            {
                eq.Name = qsetName;
                eq.Description = "Measurement length quantity";
                eq.Quantities.Add(ifcQuantityHeight);
            });

            return ifcQuantityHeight;
        }

        public static IfcQuantityVolume VolumeSpaceQuantity(IfcStore model, double value, string qsetName, string qName)
        {
            //Create a IfcElementQuantity
            //first we need a IfcPhysicalSimpleQuantity,first will use IfcQuantityLength
            var ifcQuantityVolume = model.Instances.New<IfcQuantityVolume>(qa =>
            {
                qa.Name = qName;
                qa.Description = "";
                qa.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.VOLUMEUNIT;
                    siu.Name = IfcSIUnitName.CUBIC_METRE;

                });
                double areaWithoutUnitDouble = value;
                qa.VolumeValue = areaWithoutUnitDouble;
            });
            var ifcElementQuantity = model.Instances.New<IfcElementQuantity>(eq =>
            {
                eq.Name = qsetName;
                eq.Description = "Measurement volume quantity";
                eq.Quantities.Add(ifcQuantityVolume);
            });
            return ifcQuantityVolume;
        }

        public void setAddressLine(Document doc, string address, IfcPostalAddress a)
        {
            Parameter addressLineParam = doc.ProjectInformation.LookupParameter(address);
            string addressLineParamValue = addressLineParam.AsString();
            a.AddressLines.Add(addressLineParamValue);
        }

        public string setAddressValues(Document doc, string addressParam)
        {
            Parameter param = doc.ProjectInformation.LookupParameter(addressParam);
            string paramValue = param.AsString();

            return paramValue;
        }

        public string setCrsValue(Document doc, string crsParam)
        {
            string paramValue = default;
            if (doc.ProjectInformation.LookupParameter(crsParam) != null)
            {
                Parameter param = doc.ProjectInformation.LookupParameter(crsParam);
                paramValue = param.AsString();
            }
            return paramValue;
        }

        /// <summary>
        /// Creates an IFCSite which includes geometry and properties taken from revit project along with some basic predefined properties. 
        /// </summary>
        public static IfcSite CreateSite(IfcStore model, IList<XYZ> topoPoints, ParameterSet topoParams, string bezeichnung, TopographySurface topoSurf, XYZ pbp, Document doc, Mesh mesh)
        {
            double feetToMeter = 1.0 / 0.3048;
            var ifcProject = model.Instances.OfType<IfcProject>().FirstOrDefault();

            using (var txn = model.BeginTransaction("Create Ifc Export for topography"))
            {
                var site = model.Instances.New<IfcSite>();
                site.Name = "Site for " + bezeichnung;
                site.RefElevation = 0.0;
                site.Description = "Site fuer Nutzungsflaeche " + bezeichnung;                

                double lat, lon, gamma, scale;
                UTMcalc.UtmGrs80Reverse(33, false, pbp.X, pbp.Y, out lat, out lon, out gamma, out scale);
                var lat_geo = UTMcalc.DegToString(lat, true);
                var lon_geo = UTMcalc.DegToString(lon, true);
                var latSplit = UTMcalc.SplitSexagesimal(lat_geo);
                var lonSplit = UTMcalc.SplitSexagesimal(lon_geo);

                site.RefLatitude = new List<long> { latSplit[0], latSplit[1], latSplit[2], latSplit[3] };
                site.RefLongitude = new List<long> { lonSplit[0], lonSplit[1], lonSplit[2], lonSplit[3] };

                site.GlobalId = Guid.NewGuid();

                var ifcAddress = model.Instances.New<IfcPostalAddress>(a =>
                {
                    a.Description = "Adresse fuer Baugrundstueck. ";
                    IfcXBim ifcXBim = new IfcXBim();
                    ifcXBim.setAddressLine(doc, "Address Line", a);
                    a.Town = ifcXBim.setAddressValues(doc, "Town");
                    a.Region = ifcXBim.setAddressValues(doc, "Region");
                    a.PostalCode = ifcXBim.setAddressValues(doc, "Postal Code");
                    a.Country = ifcXBim.setAddressValues(doc, "Country");
                });
                site.SiteAddress = ifcAddress;

                var curveSet = model.Instances.New<IfcPolyline>();
                var loop = model.Instances.New<IfcPolyLoop>();

                foreach (var tp in topoPoints)
                {
                    XYZ topoPoint = new XYZ(tp.X, tp.Y, tp.Z);
                    var cartPoint = model.Instances.New<IfcCartesianPoint>();
                    cartPoint.SetXYZ(Convert.ToDouble(topoPoint.X, CultureInfo.InvariantCulture)/feetToMeter,
                        Convert.ToDouble(topoPoint.Y, CultureInfo.InvariantCulture) / feetToMeter,
                        Convert.ToDouble(topoPoint.Z, CultureInfo.InvariantCulture) / feetToMeter);

                    curveSet.Points.Add(cartPoint);
                    loop.Polygon.Add(cartPoint);
                }

                var projectBasePoint = model.Instances.New<IfcCartesianPoint>();
                projectBasePoint.SetXYZ(pbp.X, pbp.Y, pbp.Z);
                //projectBasePoint.SetXYZ(0,0,0);

                var facebound = model.Instances.New<IfcFaceBound>();
                facebound.Bound = loop;

                var outerFace = model.Instances.New<IfcFaceOuterBound>();
                outerFace.Bound = loop;

                var face = model.Instances.New<IfcFace>();
                face.Bounds.Add(outerFace);

                var connFaceSet = model.Instances.New<IfcConnectedFaceSet>();
                connFaceSet.CfsFaces.Add(face);

                var material = model.Instances.New<IfcMaterial>();
                material.Name = "transparent";

                var colorDict = new Dictionary<string, string>();
                colorDict = CreateColors();

                double rot,gruen,blau;
                if (colorDict.ContainsKey(bezeichnung))
                {
                    rot = Convert.ToDouble(colorDict[bezeichnung].Split('/')[0]);
                    gruen = Convert.ToDouble(colorDict[bezeichnung].Split('/')[1]);
                    blau = Convert.ToDouble(colorDict[bezeichnung].Split('/')[2]);
                }
                else
                {
                    rot = 255;
                    gruen = 250;
                    blau = 240;
                }

                var colourRgb = model.Instances.New<IfcColourRgb>();
                colourRgb.Red = rot / 255.0;
                colourRgb.Green = gruen / 255.0;
                colourRgb.Blue = blau / 255.0;

                var surfaceStyleRendering = model.Instances.New<IfcSurfaceStyleRendering>();
                surfaceStyleRendering.SurfaceColour = colourRgb;
                //surfaceStyleRendering.Transparency = 0.5;

                var surfaceStyle = model.Instances.New<IfcSurfaceStyle>();
                surfaceStyle.Styles.Add(surfaceStyleRendering);
                surfaceStyle.Name = bezeichnung;

                var presentation = model.Instances.New<IfcPresentationStyleAssignment>();
                presentation.Styles.Add(surfaceStyle);

                //var pointList3D = model.Instances.New<IfcCartesianPointList3D>();
                //pointList3D.CoordList.Add(new ifclengthm(1.0);

                //foreach (var tp in topoPoints)
                //{
                //    XYZ topoPoint = new XYZ(tp.X, tp.Y, tp.Z);
                //    var cartPoint = model.Instances.New<IfcCartesianPoint>();
                //    cartPoint.SetXYZ(Convert.ToDouble(topoPoint.X, CultureInfo.InvariantCulture) / feetToMeter,
                //        Convert.ToDouble(topoPoint.Y, CultureInfo.InvariantCulture) / feetToMeter,
                //        Convert.ToDouble(topoPoint.Z, CultureInfo.InvariantCulture) / feetToMeter);

                //    curveSet.Points.Add(cartPoint);
                //    loop.Polygon.Add(cartPoint);
                //}

                //       List<CoordIndex> coordIndex = new List<CoordIndex>() {
                //       new CoordIndex(1, 6, 5), new CoordIndex(1, 2, 6), new CoordIndex(6, 2, 7),
                //       new CoordIndex(7, 2, 3), new CoordIndex(7, 8, 6), new CoordIndex(6, 8, 5),
                //       new CoordIndex(5, 8, 1), new CoordIndex(1, 8, 4), new CoordIndex(4, 2, 1),
                //       new CoordIndex(2, 4, 3), new CoordIndex(4, 8, 7), new CoordIndex(7, 3, 4)


                //};

                //var coordIndex = model.Instances.New<IItemSet<IItemSet<IfcPositiveInteger>>>();

                //////////////var coordIndex = model.Instances.New<IItemSet<IfcPositiveInteger>>(ccl =>
                //////////////{
                //////////////    int posInt1 = 1;
                //////////////    int posInt2 = 2;
                //////////////    int posInt3 = 3;

                //////////////    //ccl.AddRange(posInt1);
                //////////////});

                //IItemSet<IfcPositiveInteger> coordIndex;
                //var coordIndex = model.Instances.New<IItemSet>();
                var pointList = model.Instances.New<IfcCartesianPointList3D>(cpl =>
                {
                    foreach (var tp in topoPoints)
                    {
                        cpl.CoordList.GetAt(0).AddRange(new IfcLengthMeasure[] { tp.X, tp.Y, tp.Z });
                        //cpl.CoordList.GetAt(0).AddRange(new IfcLengthMeasure[] { tp.X, tp.Y, tp.Z });
                        //cpl.CoordList.GetAt(1).AddRange(new IfcLengthMeasure[] { 137.671, 442.768, 13.7401 });
                        //cpl.CoordList.GetAt(2).AddRange(new IfcLengthMeasure[] { 142.393, 462.543, 11.4145 });
                    }
                });

                var triangIrregularNetwork = model.Instances.New<IfcTriangulatedFaceSet>();
                triangIrregularNetwork.Coordinates = pointList;

                var numTriangles = mesh.NumTriangles;

                var topoCount = topoPoints.Count();
                int cnt = 0;
                for (int i = 0; i < numTriangles; i++)
                {
                    var fi = triangIrregularNetwork.CoordIndex.GetAt(cnt++);
                    fi.Add(mesh.get_Triangle(i).get_Index(0));
                    fi.Add(mesh.get_Triangle(i).get_Index(1));
                    fi.Add(mesh.get_Triangle(i).get_Index(2));
                }

                //var triangIrregularNetwork = model.Instances.New<IfcTriangulatedIrregularNetwork>();
                //triangIrregularNetwork.Coordinates = pointList;

                //var curveSetFace = model.Instances.New<IfcFaceBasedSurfaceModel>();
                //curveSetFace.FbsmFaces.Add(connFaceSet);

                //var style = model.Instances.New<IfcStyledItem>();
                //style.Item = curveSetFace;
                //style.Styles.Add(presentation);
                //curveSetFace.StyledByItem.Append(style);

                //var style = model.Instances.New<IfcStyledItem>();
                //style.Item = triangFaceSet;
                //style.Styles.Add(presentation);
                //triangFaceSet.StyledByItem.Append(style);

                var style = model.Instances.New<IfcStyledItem>();
                style.Item = triangIrregularNetwork;
                style.Styles.Add(presentation);
                triangIrregularNetwork.StyledByItem.Append(style);

                var styledRepresentation = model.Instances.New<IfcStyledRepresentation>();
                styledRepresentation.Items.Add(style);
                

                var materialDefRepresentation = model.Instances.New<IfcMaterialDefinitionRepresentation>();
                materialDefRepresentation.Representations.Add(styledRepresentation);
                materialDefRepresentation.RepresentedMaterial = material;

                material.HasRepresentation.Append(materialDefRepresentation);

                //shape definition 1
                var shapeRepresentation = model.Instances.New<IfcShapeRepresentation>();
                var geomRepContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                //shapeRepresentation.RepresentationType = new IfcLabel("SurfaceOrSolidModel");

                //ProjectLocation projloc = doc.ActiveProjectLocation;
                //ProjectPosition position_data = projloc.GetProjectPosition(XYZ.Zero);
                //double angle = position_data.Angle;
                //double[] richtung = UTMcalc.AzimuthToLocalVector(angle);
                //geomRepContext.TrueNorth = model.Instances.New<IfcDirection>();
                //geomRepContext.TrueNorth.SetXY(richtung[0], richtung[1]); 

                shapeRepresentation.ContextOfItems = geomRepContext;
                shapeRepresentation.RepresentationIdentifier = "Body";
                shapeRepresentation.RepresentationType = "SurfaceModel";
                //shapeRepresentation.Items.Add(curveSetFace);
                //shapeRepresentation.Items.Add(triangFaceSet);
                shapeRepresentation.Items.Add(triangIrregularNetwork);



                var layerAssignment = model.Instances.New<IfcPresentationLayerAssignment>();
                layerAssignment.Name = "LandBIM";
                layerAssignment.AssignedItems.Add(shapeRepresentation);

                styledRepresentation.ContextOfItems = geomRepContext;

                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shapeRepresentation);
                site.Representation = rep;

                var localPlacement = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>();
                ax3D.Location = projectBasePoint;
                ax3D.RefDirection = model.Instances.New<IfcDirection>();
                ax3D.RefDirection.SetXYZ(1, 0, 0);
                ax3D.Axis = model.Instances.New<IfcDirection>();
                ax3D.Axis.SetXYZ(0, 0, 1);
                localPlacement.RelativePlacement = ax3D;
                site.ObjectPlacement = localPlacement;

                Dictionary<string, string> paramDict = new Dictionary<string, string>();
                paramDict.Add("Bezeichnung", bezeichnung);

                foreach (Parameter p in topoParams)
                {
                    if (p.IsShared)
                    {
                        string key = topoSurf.get_Parameter(new Guid(p.GUID.ToString())).Definition.Name;
                        string value = topoSurf.get_Parameter(new Guid(p.GUID.ToString())).AsString();

                        if (paramDict.ContainsKey(key) == false)
                        {
                            if (value == null)
                            {

                            }
                            else
                            {
                                paramDict.Add(key, value);
                            }
                        }
                    }
                }

                //set a few basic properties
                model.Instances.New<IfcRelDefinesByProperties>(relDef =>
                {
                    relDef.RelatedObjects.Add(site);
                    relDef.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pSet =>
                    {
                        pSet.Name = "Surface properties";
                        foreach (var topoParam in paramDict)
                        {
                            IfcXBim ifcXBim = new IfcXBim();
                            ifcXBim.setSurfaceProperties(pSet, model, topoParam);
                        }
                    });
                });

                //set Quantities
                CreateSiteQuantity(model, site, topoSurf);

                model.Instances.New<IfcRelDefinesByProperties>(relGeokod =>
                {
                    relGeokod.RelatedObjects.Add(site);
                    relGeokod.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(psetGeokod =>
                    {
                        psetGeokod.Name = "BauantragGeokodierung";

                        if (paramDict.ContainsKey("alkis:Gemarkung_Nummer") || paramDict.ContainsKey("alkis:Flure") || paramDict.ContainsKey("alkis:Flurstuecksnummer"))
                        {
                            foreach (var s in paramDict)
                            {
                                string verortung = s.Key.Substring(s.Key.LastIndexOf(':') + 1);
                                string verortungTrim = verortung.Trim(' ');

                                if (verortungTrim == "Gemarkung_Nummer" || verortungTrim == "Flure" || verortungTrim == "Flurstuecksnummer")
                                {
                                    psetGeokod.HasProperties.AddRange(new[]
                                    {
                                    model.Instances.New<IfcPropertySingleValue>(p =>
                                        {
                                            switch (verortungTrim)
                                            {
                                                case "Gemarkung_Nummer":
                                                    p.Name = "Gemarkungen";
                                                    p.NominalValue = new IfcText(s.Value);
                                                    break;
                                                case "Flure":
                                                    p.Name = "Flure";
                                                    p.NominalValue = new IfcText(s.Value);
                                                    break;
                                                case "Flurstuecksnummer":
                                                    p.Name = "Flurstücke";
                                                    p.NominalValue = new IfcText(s.Value);
                                                    break;
                                                default:
                                                    break;
                                            }
                                        }),
                                    });
                                }  
                            }
                        }
                        else
                        {
                            psetGeokod.HasProperties.AddRange(new[]
                            {
                                 model.Instances.New<IfcPropertySingleValue>(p =>
                                     {
                                         p.Name = "Gemarkungen";
                                         p.NominalValue = new IfcText("empty");

                                     }),
                                 model.Instances.New<IfcPropertySingleValue>(p =>
                                     {
                                         p.Name = "Flure";
                                         p.NominalValue = new IfcText("empty");

                                     }),
                                 model.Instances.New<IfcPropertySingleValue>(p =>
                                     {
                                         p.Name = "Flurstuecksnummer";
                                         p.NominalValue = new IfcText("empty");

                                     }),
                             });
                        }
                    });
                });

                var relAggregates = model.Instances.New<IfcRelAggregates>();
                relAggregates.RelatingObject = ifcProject;
                relAggregates.RelatedObjects.Add(site);

                txn.Commit();
                return site;
            }
        }

        public static IfcSpace createSpace(IfcStore model, TopographySurface topoSurf, ExternalCommandData commandData, XYZ pbp, string bezeichnung)
        {
            var ifcProject = model.Instances.OfType<IfcProject>().FirstOrDefault();

            using (var txn = model.BeginTransaction("Create IfcSpace for surfaces"))
            {
                var space = model.Instances.New<IfcSpace>();
                space.Name = "Space for " + bezeichnung;
                space.Description = "ifcspace";

                var view = commandData.Application.ActiveUIDocument.ActiveView as View3D;
                BoundingBoxXYZ boundingBox = topoSurf.get_BoundingBox(view);

                var cpbbMin = model.Instances.New<IfcCartesianPoint>();
                cpbbMin.SetXYZ(Convert.ToDouble(boundingBox.Min.X / feetToMeter), Convert.ToDouble(boundingBox.Min.Y) / feetToMeter, Convert.ToDouble(boundingBox.Min.Z) / feetToMeter);
                var cpbbMax = model.Instances.New<IfcCartesianPoint>();
                cpbbMax.SetXYZ(Convert.ToDouble(boundingBox.Max.X) / feetToMeter, Convert.ToDouble(boundingBox.Max.Y) / feetToMeter, Convert.ToDouble(boundingBox.Max.Z) / feetToMeter);

                var material = model.Instances.New<IfcMaterial>();
                material.Name = "transparent";

                var colorDict = new Dictionary<string, string>();
                colorDict = CreateColors();

                double rot, gruen, blau;
                if (colorDict.ContainsKey(bezeichnung))
                {
                    rot = Convert.ToDouble(colorDict[bezeichnung].Split('/')[0]);
                    gruen = Convert.ToDouble(colorDict[bezeichnung].Split('/')[1]);
                    blau = Convert.ToDouble(colorDict[bezeichnung].Split('/')[2]);
                }
                else
                {
                    rot = 255;
                    gruen = 250;
                    blau = 240;
                }

                var colourRgb = model.Instances.New<IfcColourRgb>();
                colourRgb.Red = rot / 255.0;
                colourRgb.Green = gruen / 255.0;
                colourRgb.Blue = blau / 255.0;

                var surfaceStyleRendering = model.Instances.New<IfcSurfaceStyleRendering>();
                surfaceStyleRendering.SurfaceColour = colourRgb;
                //surfaceStyleRendering.Transparency = 0.5;

                var surfaceStyle = model.Instances.New<IfcSurfaceStyle>();
                surfaceStyle.Styles.Add(surfaceStyleRendering);
                surfaceStyle.Name = bezeichnung;

                var presentation = model.Instances.New<IfcPresentationStyleAssignment>();
                presentation.Styles.Add(surfaceStyle);

                var rectProf = model.Instances.New<IfcRectangleProfileDef>();
                rectProf.ProfileType = IfcProfileTypeEnum.AREA;
                rectProf.XDim = (cpbbMax.X - cpbbMin.X); 
                rectProf.YDim = (cpbbMax.Y - cpbbMin.Y);

                //insert point
                var insertPoint = model.Instances.New<IfcCartesianPoint>();
                insertPoint.SetXY(0, 0);
                rectProf.Position = model.Instances.New<IfcAxis2Placement2D>();
                rectProf.Position.Location = insertPoint;
                rectProf.Position.RefDirection = model.Instances.New<IfcDirection>();
                rectProf.Position.RefDirection.SetXY(1, 0);

                var spaceSolid = model.Instances.New<IfcExtrudedAreaSolid>();
                spaceSolid.Depth = (cpbbMax.Z - cpbbMin.Z); 
                spaceSolid.SweptArea = rectProf;
                spaceSolid.ExtrudedDirection = model.Instances.New<IfcDirection>();
                spaceSolid.ExtrudedDirection.SetXYZ(0, 0, 1);

                double midX = ((cpbbMax.X + cpbbMin.X)) / 2;
                double midY = ((cpbbMax.Y + cpbbMin.Y)) / 2;
                double midZ = (cpbbMin.Z);

                var position = model.Instances.New<IfcCartesianPoint>();
                position.SetXYZ(midX, midY, midZ);
                spaceSolid.Position = model.Instances.New<IfcAxis2Placement3D>();
                spaceSolid.Position.Location = position;

                spaceSolid.Position.RefDirection = model.Instances.New<IfcDirection>();
                spaceSolid.Position.Axis = model.Instances.New<IfcDirection>();
                spaceSolid.Position.RefDirection.SetXYZ(1, 0, 0);
                spaceSolid.Position.Axis.SetXYZ(0, 0, 1);

                var style = model.Instances.New<IfcStyledItem>();
                style.Item = spaceSolid;
                style.Styles.Add(presentation);
                spaceSolid.StyledByItem.Append(style);

                var styledRepresentation = model.Instances.New<IfcStyledRepresentation>();
                styledRepresentation.Items.Add(style);

                var shapeRepresentation = model.Instances.New<IfcShapeRepresentation>();
                var geomRepContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();

                shapeRepresentation.Items.Add(spaceSolid);
                shapeRepresentation.ContextOfItems = geomRepContext;
                shapeRepresentation.RepresentationIdentifier = "Body";
                shapeRepresentation.RepresentationType = "SweptSolid";

                styledRepresentation.ContextOfItems = geomRepContext;

                var spaceRep = model.Instances.New<IfcProductDefinitionShape>();
                spaceRep.Representations.Add(shapeRepresentation);
                space.Representation = spaceRep;

                var layerAssignment = model.Instances.New<IfcPresentationLayerAssignment>();
                layerAssignment.Name = "LandBIM";
                layerAssignment.AssignedItems.Add(shapeRepresentation);

                var projectBasePoint = model.Instances.New<IfcCartesianPoint>();
                projectBasePoint.SetXYZ(pbp.X, pbp.Y, pbp.Z);

                var localPlacement = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>();
                ax3D.Location = projectBasePoint;
                ax3D.RefDirection = model.Instances.New<IfcDirection>();
                ax3D.RefDirection.SetXYZ(1, 0, 0);
                ax3D.Axis = model.Instances.New<IfcDirection>();
                ax3D.Axis.SetXYZ(0, 0, 1);
                localPlacement.RelativePlacement = ax3D;
                space.ObjectPlacement = localPlacement;

                var relAggregates = model.Instances.New<IfcRelAggregates>();
                relAggregates.RelatingObject = ifcProject;
                relAggregates.RelatedObjects.Add(space);

                //set a few basic properties
                model.Instances.New<IfcRelDefinesByProperties>(relSpace =>
                {
                    relSpace.RelatedObjects.Add(space);
                    relSpace.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pSetSpace =>
                    {
                        pSetSpace.Name = "BauantragGrundstück";
                        pSetSpace.HasProperties.AddRange(new[]
{
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = "IstGrundstücksfläche";
                                    p.NominalValue = new IfcBoolean(true);
                                }),
                            });
                    });
                });

                // Propertyset für versiegelte Flächen sowie Spiel- und Freizeitflächen
                model.Instances.New<IfcRelDefinesByProperties>(relSpaceArea =>
                {
                    relSpaceArea.RelatedObjects.Add(space);
                    // Propertyset für versiegelte Flächen sowie Spiel- und Freizeitflächen
                    relSpaceArea.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pSetSpaceArea =>
                    {
                        pSetSpaceArea.Name = "BauantragGrundstücksflächen";
                        pSetSpaceArea.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = "IstVersiegelt";
                                    p.NominalValue = new IfcBoolean(false);
                                }),
                            });
                        pSetSpaceArea.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = "IstSpielUndFreizeitfläche";
                                    p.NominalValue = new IfcBoolean(false);
                                }),
                            });
                        pSetSpaceArea.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = "IstVerkaufsfläche";
                                    p.NominalValue = new IfcBoolean(false);
                                }),
                            });
                        pSetSpaceArea.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = "IstNebenanlage";
                                    p.NominalValue = new IfcBoolean(false);
                                }),
                            });
                        pSetSpaceArea.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = "IstGemeinschaftsanlage";
                                    p.NominalValue = new IfcBoolean(false);
                                }),
                            });
                    });
                });

                var area = topoSurf.get_Parameter(BuiltInParameter.PROJECTED_SURFACE_AREA).AsValueString();
                string[] areaSplit = area.Split(' ');
                string areaWithoutUnit = areaSplit[0];
                double areaWithoutUnitDouble = Convert.ToDouble(areaWithoutUnit, CultureInfo.InvariantCulture);

                var ifcQuantityArea = AreaSpaceQuantity(model, areaWithoutUnitDouble, "Qto_SpaceBaseQuantites", "GrossFloorArea");

                var ifcElementQuantity = model.Instances.New<IfcElementQuantity>(eq =>
                {
                    eq.Name = "Qto_SpaceBaseQuantites";
                    eq.Description = "Measurement quantity";
                    eq.Quantities.Add(ifcQuantityArea);
                });

                model.Instances.New<IfcRelDefinesByProperties>(rdbp =>
                {
                    rdbp.Name = "Qto_SpaceBaseQuantities";
                    rdbp.Description = "";
                    rdbp.RelatedObjects.Add(space);
                    rdbp.RelatingPropertyDefinition = ifcElementQuantity;
                });

                txn.Commit();
                return space;
            }
        }

        public static IfcSpace CreateBuildingSpace(IfcStore model, FilteredElementCollector buildingElements, ExternalCommandData commandData, XYZ pbp)
        {
            var ifcProject = model.Instances.OfType<IfcProject>().FirstOrDefault();

            using (var txn = model.BeginTransaction("Create IfcSpace for buildings"))
            {
                var space = model.Instances.New<IfcSpace>();
                space.Name = "Space for building";
                space.Description = "ifcspace";

                var view = commandData.Application.ActiveUIDocument.ActiveView as View3D;

                List<double> minListX = new List<double>();
                List<double> minListY = new List<double>();
                List<double> minListZ = new List<double>();
                List<double> maxListX = new List<double>();
                List<double> maxListY = new List<double>();
                List<double> maxListZ = new List<double>();

                foreach (var x in buildingElements)
                {
                    if (x.get_BoundingBox(view) != null)
                    {
                        var cbb = x.get_BoundingBox(view);
                        minListX.Add(cbb.Min.X);
                        minListY.Add(cbb.Min.Y);
                        minListZ.Add(cbb.Min.Z);
                        maxListX.Add(cbb.Max.X);
                        maxListY.Add(cbb.Max.Y);
                        maxListZ.Add(cbb.Max.Z);
                    }
                }

                var boundingBox = new BoundingBoxXYZ();
                boundingBox.Min = new XYZ(minListX.Min(), minListY.Min(), minListZ.Min());
                boundingBox.Max = new XYZ(maxListX.Max(), maxListY.Max(), maxListZ.Max());

                var cpbbMin = model.Instances.New<IfcCartesianPoint>();
                cpbbMin.SetXYZ(Convert.ToDouble(boundingBox.Min.X / feetToMeter), Convert.ToDouble(boundingBox.Min.Y) / feetToMeter, Convert.ToDouble(boundingBox.Min.Z) / feetToMeter);
                var cpbbMax = model.Instances.New<IfcCartesianPoint>();
                cpbbMax.SetXYZ(Convert.ToDouble(boundingBox.Max.X) / feetToMeter, Convert.ToDouble(boundingBox.Max.Y) / feetToMeter, Convert.ToDouble(boundingBox.Max.Z) / feetToMeter);

                var material = model.Instances.New<IfcMaterial>();
                material.Name = "transparent";

                var colorDict = new Dictionary<string, string>();
                colorDict = CreateColors();

                string bezeichnung = "123";

                double rot, gruen, blau;
                if (colorDict.ContainsKey(bezeichnung))
                {
                    rot = Convert.ToDouble(colorDict[bezeichnung].Split('/')[0]);
                    gruen = Convert.ToDouble(colorDict[bezeichnung].Split('/')[1]);
                    blau = Convert.ToDouble(colorDict[bezeichnung].Split('/')[2]);
                }
                else
                {
                    rot = 255;
                    gruen = 250;
                    blau = 240;
                }

                var colourRgb = model.Instances.New<IfcColourRgb>();
                colourRgb.Red = rot / 255.0;
                colourRgb.Green = gruen / 255.0;
                colourRgb.Blue = blau / 255.0;

                var surfaceStyleRendering = model.Instances.New<IfcSurfaceStyleRendering>();
                surfaceStyleRendering.SurfaceColour = colourRgb;
                //surfaceStyleRendering.Transparency = 0.5;

                var surfaceStyle = model.Instances.New<IfcSurfaceStyle>();
                surfaceStyle.Styles.Add(surfaceStyleRendering);
                surfaceStyle.Name = "building";

                var presentation = model.Instances.New<IfcPresentationStyleAssignment>();
                presentation.Styles.Add(surfaceStyle);

                var rectProf = model.Instances.New<IfcRectangleProfileDef>();
                rectProf.ProfileType = IfcProfileTypeEnum.AREA;
                rectProf.XDim = (cpbbMax.X - cpbbMin.X);
                rectProf.YDim = (cpbbMax.Y - cpbbMin.Y);

                double midX = (cpbbMax.X + cpbbMin.X) / 2;
                double midY = (cpbbMax.Y + cpbbMin.Y) / 2;
                double midZ = cpbbMin.Z;

                //insert point
                var insertPoint = model.Instances.New<IfcCartesianPoint>();
                insertPoint.SetXY(0, 0);
                rectProf.Position = model.Instances.New<IfcAxis2Placement2D>();
                rectProf.Position.Location = insertPoint;
                rectProf.Position.RefDirection = model.Instances.New<IfcDirection>();
                rectProf.Position.RefDirection.SetXY(1, 0);

                var spaceSolid = model.Instances.New<IfcExtrudedAreaSolid>();
                spaceSolid.Depth = cpbbMax.Z - cpbbMin.Z;
                spaceSolid.SweptArea = rectProf;
                spaceSolid.ExtrudedDirection = model.Instances.New<IfcDirection>();
                spaceSolid.ExtrudedDirection.SetXYZ(0, 0, 1);

                var position = model.Instances.New<IfcCartesianPoint>();
                position.SetXYZ(midX, midY, midZ);
                spaceSolid.Position = model.Instances.New<IfcAxis2Placement3D>();
                spaceSolid.Position.Location = position;

                spaceSolid.Position.RefDirection = model.Instances.New<IfcDirection>();
                spaceSolid.Position.Axis = model.Instances.New<IfcDirection>();
                spaceSolid.Position.RefDirection.SetXYZ(1, 0, 0);
                spaceSolid.Position.Axis.SetXYZ(0, 0, 1);

                var style = model.Instances.New<IfcStyledItem>();
                style.Item = spaceSolid;
                style.Styles.Add(presentation);
                spaceSolid.StyledByItem.Append(style);

                var styledRepresentation = model.Instances.New<IfcStyledRepresentation>();
                styledRepresentation.Items.Add(style);

                var shapeRepresentation = model.Instances.New<IfcShapeRepresentation>();
                var geomRepContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();

                shapeRepresentation.Items.Add(spaceSolid);
                shapeRepresentation.ContextOfItems = geomRepContext;
                shapeRepresentation.RepresentationIdentifier = "Body";
                shapeRepresentation.RepresentationType = "SweptSolid";

                styledRepresentation.ContextOfItems = geomRepContext;

                var spaceRep = model.Instances.New<IfcProductDefinitionShape>();
                spaceRep.Representations.Add(shapeRepresentation);
                space.Representation = spaceRep;

                var layerAssignment = model.Instances.New<IfcPresentationLayerAssignment>();
                layerAssignment.Name = "LandBIM";
                layerAssignment.AssignedItems.Add(shapeRepresentation);

                var projectBasePoint = model.Instances.New<IfcCartesianPoint>();
                projectBasePoint.SetXYZ(pbp.X, pbp.Y, pbp.Z);

                var localPlacement = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>();
                ax3D.Location = projectBasePoint;
                ax3D.RefDirection = model.Instances.New<IfcDirection>();
                ax3D.RefDirection.SetXYZ(1, 0, 0);
                ax3D.Axis = model.Instances.New<IfcDirection>();
                ax3D.Axis.SetXYZ(0, 0, 1);
                localPlacement.RelativePlacement = ax3D;
                space.ObjectPlacement = localPlacement;

                var relAggregates = model.Instances.New<IfcRelAggregates>();
                relAggregates.RelatingObject = ifcProject;
                relAggregates.RelatedObjects.Add(space);

                //set a few basic properties
                model.Instances.New<IfcRelDefinesByProperties>(relSpace =>
                {
                    relSpace.RelatedObjects.Add(space);
                    relSpace.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pSetSpace =>
                    {
                        pSetSpace.Name = "BauantragGebäude";
                        pSetSpace.HasProperties.AddRange(new[]
{
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = "IstGebäudehülle";
                                    p.NominalValue = new IfcBoolean(true);
                                }),
                            });
                    });
                });

                double buildingHeight = cpbbMax.Z - cpbbMin.Z;

                var ifcQuantityHeight = HeightSpaceQuantity(model, buildingHeight, "Qto_SpaceBaseQuantites", "Height");

                var ifcElementQuantity = model.Instances.New<IfcElementQuantity>(eq =>
                {
                    eq.Name = "Qto_SpaceBaseQuantites";
                    eq.Description = "Measurement quantity";
                    eq.Quantities.Add(ifcQuantityHeight);
                });

                model.Instances.New<IfcRelDefinesByProperties>(rdbp =>
                {
                    rdbp.Name = "Qto_SpaceBaseQuantities";
                    rdbp.Description = "";
                    rdbp.RelatedObjects.Add(space);
                    rdbp.RelatingPropertyDefinition = ifcElementQuantity;
                });

                txn.Commit();
                return space;
            }
        }

        public static IfcSpace createStoreySpace(IfcStore model, IfcBuildingStorey buildingStorey, ExternalCommandData commandData, double storeyHeight, FilteredElementCollector buildingElements)
        {
            var ifcProject = model.Instances.OfType<IfcProject>().FirstOrDefault();

            using (var txn = model.BeginTransaction("Create IfcSpace for buildings"))
            {
                var space = model.Instances.New<IfcSpace>();
                space.Name = "Space for storey space";
                space.Description = "ifcspace";

                var objectPlacement = buildingStorey.ObjectPlacement as IfcLocalPlacement;
                var lp1 = objectPlacement.PlacementRelTo as IfcLocalPlacement;
                var lp2 = lp1.PlacementRelTo as IfcLocalPlacement;            
                var axis3d = lp2.RelativePlacement as IfcAxis2Placement3D;    
                var location = axis3d.Location; 

                double relZ = buildingStorey.Elevation.Value;

                var view = commandData.Application.ActiveUIDocument.ActiveView as View3D;

                List<double> minListX = new List<double>();
                List<double> minListY = new List<double>();
                List<double> minListZ = new List<double>();
                List<double> maxListX = new List<double>();
                List<double> maxListY = new List<double>();
                List<double> maxListZ = new List<double>();

                foreach (var x in buildingElements)
                {
                    if (x.get_BoundingBox(view) != null)
                    {
                        var cbb = x.get_BoundingBox(view);
                        minListX.Add(cbb.Min.X);
                        minListY.Add(cbb.Min.Y);
                        minListZ.Add(cbb.Min.Z);
                        maxListX.Add(cbb.Max.X);
                        maxListY.Add(cbb.Max.Y);
                        maxListZ.Add(cbb.Max.Z);
                    }
                }

                var boundingBox = new BoundingBoxXYZ();
                boundingBox.Min = new XYZ(minListX.Min(), minListY.Min(), minListZ.Min());
                boundingBox.Max = new XYZ(maxListX.Max(), maxListY.Max(), maxListZ.Max());

                var cpbbMin = model.Instances.New<IfcCartesianPoint>();
                cpbbMin.SetXYZ(Convert.ToDouble(boundingBox.Min.X / feetToMeter), Convert.ToDouble(boundingBox.Min.Y) / feetToMeter, relZ);
                var cpbbMax = model.Instances.New<IfcCartesianPoint>();
                cpbbMax.SetXYZ(Convert.ToDouble(boundingBox.Max.X) / feetToMeter, Convert.ToDouble(boundingBox.Max.Y) / feetToMeter, storeyHeight);

                var material = model.Instances.New<IfcMaterial>();
                material.Name = "transparent";

                var colorDict = new Dictionary<string, string>();
                colorDict = CreateColors();

                string bezeichnung = "123";

                double rot, gruen, blau;
                if (colorDict.ContainsKey(bezeichnung))
                {
                    rot = Convert.ToDouble(colorDict[bezeichnung].Split('/')[0]);
                    gruen = Convert.ToDouble(colorDict[bezeichnung].Split('/')[1]);
                    blau = Convert.ToDouble(colorDict[bezeichnung].Split('/')[2]);
                }
                else
                {
                    rot = 255;
                    gruen = 250;
                    blau = 240;
                }

                var colourRgb = model.Instances.New<IfcColourRgb>();
                colourRgb.Red = rot / 255.0;
                colourRgb.Green = gruen / 255.0;
                colourRgb.Blue = blau / 255.0;

                var surfaceStyleRendering = model.Instances.New<IfcSurfaceStyleRendering>();
                surfaceStyleRendering.SurfaceColour = colourRgb;
                //surfaceStyleRendering.Transparency = 0.5;

                var surfaceStyle = model.Instances.New<IfcSurfaceStyle>();
                surfaceStyle.Styles.Add(surfaceStyleRendering);
                surfaceStyle.Name = "building";

                var presentation = model.Instances.New<IfcPresentationStyleAssignment>();
                presentation.Styles.Add(surfaceStyle);

                var rectProf = model.Instances.New<IfcRectangleProfileDef>();
                rectProf.ProfileType = IfcProfileTypeEnum.AREA;
                rectProf.XDim = (cpbbMax.X - cpbbMin.X);
                rectProf.YDim = (cpbbMax.Y - cpbbMin.Y);

                //insert point
                var insertPoint = model.Instances.New<IfcCartesianPoint>();
                insertPoint.SetXY(0, 0);
                rectProf.Position = model.Instances.New<IfcAxis2Placement2D>();
                rectProf.Position.Location = insertPoint;
                rectProf.Position.RefDirection = model.Instances.New<IfcDirection>();
                rectProf.Position.RefDirection.SetXY(1, 0);

                var spaceSolid = model.Instances.New<IfcExtrudedAreaSolid>();
                spaceSolid.Depth = cpbbMax.Z - cpbbMin.Z;
                spaceSolid.SweptArea = rectProf;
                spaceSolid.ExtrudedDirection = model.Instances.New<IfcDirection>();
                spaceSolid.ExtrudedDirection.SetXYZ(0, 0, 1);

                double midX = (cpbbMax.X + cpbbMin.X) / 2;
                double midY = (cpbbMax.Y + cpbbMin.Y) / 2;
                double midZ = cpbbMin.Z;

                var position = model.Instances.New<IfcCartesianPoint>();
                position.SetXYZ(midX, midY, midZ);
                spaceSolid.Position = model.Instances.New<IfcAxis2Placement3D>();
                spaceSolid.Position.Location = position;

                spaceSolid.Position.RefDirection = model.Instances.New<IfcDirection>();
                spaceSolid.Position.Axis = model.Instances.New<IfcDirection>();
                spaceSolid.Position.RefDirection.SetXYZ(1, 0, 0);
                spaceSolid.Position.Axis.SetXYZ(0, 0, 1);

                var style = model.Instances.New<IfcStyledItem>();
                style.Item = spaceSolid;
                style.Styles.Add(presentation);
                spaceSolid.StyledByItem.Append(style);

                var styledRepresentation = model.Instances.New<IfcStyledRepresentation>();
                styledRepresentation.Items.Add(style);

                var shapeRepresentation = model.Instances.New<IfcShapeRepresentation>();
                var geomRepContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();

                shapeRepresentation.Items.Add(spaceSolid);
                shapeRepresentation.ContextOfItems = geomRepContext;
                shapeRepresentation.RepresentationIdentifier = "Body";
                shapeRepresentation.RepresentationType = "SweptSolid";

                styledRepresentation.ContextOfItems = geomRepContext;

                var spaceRep = model.Instances.New<IfcProductDefinitionShape>();
                spaceRep.Representations.Add(shapeRepresentation);
                space.Representation = spaceRep;

                var layerAssignment = model.Instances.New<IfcPresentationLayerAssignment>();
                layerAssignment.Name = "LandBIM";
                layerAssignment.AssignedItems.Add(shapeRepresentation);

                var projectBasePoint = model.Instances.New<IfcCartesianPoint>();
                projectBasePoint.SetXYZ(location.X, location.Y, location.Z);

                var localPlacement = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>();
                ax3D.Location = projectBasePoint;
                ax3D.RefDirection = model.Instances.New<IfcDirection>();
                ax3D.RefDirection.SetXYZ(1, 0, 0);
                ax3D.Axis = model.Instances.New<IfcDirection>();
                ax3D.Axis.SetXYZ(0, 0, 1);
                localPlacement.RelativePlacement = ax3D;
                space.ObjectPlacement = localPlacement;

                var relAggregates = model.Instances.New<IfcRelAggregates>();
                relAggregates.RelatingObject = ifcProject;
                relAggregates.RelatedObjects.Add(space);

                //set a few basic properties
                model.Instances.New<IfcRelDefinesByProperties>(relSpace =>
                {
                    relSpace.RelatedObjects.Add(space);
                    relSpace.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pSetSpace =>
                    {
                        pSetSpace.Name = "BauantragGeschoss";
                        pSetSpace.HasProperties.AddRange(new[]
{
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = "IstGF";
                                    p.NominalValue = new IfcBoolean(true);
                                }),
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = "IstVollgeschoss";
                                    p.NominalValue = new IfcBoolean(true);
                                }),
                            });
                    });
                });

                double bbHeight = cpbbMax.Z - cpbbMin.Z;
                double bbLength = cpbbMax.X - cpbbMin.X;
                double bbWidth = cpbbMax.Y - cpbbMin.Y;
                double bbFloorArea = bbLength * bbWidth;
                double bbFloorVolume = bbFloorArea * bbHeight;

                var ifcQuantityHeight = HeightSpaceQuantity(model, bbHeight, "Qto_SpaceBaseQuantites", "Height");
                var ifcQuantityArea = AreaSpaceQuantity(model, bbFloorArea, "Qto_SpaceBaseQuantites", "GrossFloorArea");
                var ifcQuantityVolume = VolumeSpaceQuantity(model, bbFloorVolume, "Qto_SpaceBaseQuantites", "GrossVolume");

                var ifcElementQuantity = model.Instances.New<IfcElementQuantity>(eq =>
                {
                    eq.Name = "Qto_SpaceBaseQuantites";
                    eq.Description = "Measurement quantity";
                    eq.Quantities.Add(ifcQuantityHeight);
                    eq.Quantities.Add(ifcQuantityArea);
                    eq.Quantities.Add(ifcQuantityVolume);
                });

                model.Instances.New<IfcRelDefinesByProperties>(rdbp =>
                {
                    rdbp.Name = "Qto_SpaceBaseQuantities";
                    rdbp.Description = "";
                    rdbp.RelatedObjects.Add(space);
                    rdbp.RelatingPropertyDefinition = ifcElementQuantity;
                });

                txn.Commit();
                return space;
            }
        }

        public static IfcSpace CreateFloorSpace(IfcStore model, FilteredElementCollector buildingElements, ExternalCommandData commandData, XYZ pbp)
        {
            var ifcProject = model.Instances.OfType<IfcProject>().FirstOrDefault();

            using (var txn = model.BeginTransaction("Create FloorSpace for buildings"))
            {
                var space = model.Instances.New<IfcSpace>();
                space.Name = "Space for floor space";
                space.Description = "ifcspace";

                var view = commandData.Application.ActiveUIDocument.ActiveView as View3D;

                List<double> minListX = new List<double>();
                List<double> minListY = new List<double>();
                List<double> minListZ = new List<double>();
                List<double> maxListX = new List<double>();
                List<double> maxListY = new List<double>();
                List<double> maxListZ = new List<double>();

                foreach (var x in buildingElements)
                {
                    if (x.get_BoundingBox(view) != null)
                    {
                        var cbb = x.get_BoundingBox(view);
                        maxListZ.Add(cbb.Max.Z);
                    }
                }

                var walls = buildingElements.OfCategory(BuiltInCategory.OST_Walls);

                foreach (var w in walls)
                {
                    if (w.get_BoundingBox(view) != null)
                    {
                        var cbb = w.get_BoundingBox(view);
                        minListX.Add(cbb.Min.X);
                        minListY.Add(cbb.Min.Y);
                        minListZ.Add(cbb.Min.Z);
                        maxListX.Add(cbb.Max.X);
                        maxListY.Add(cbb.Max.Y);
                    }
                }

                var boundingBox = new BoundingBoxXYZ();
                boundingBox.Min = new XYZ(minListX.Min(), minListY.Min(), minListZ.Min());
                boundingBox.Max = new XYZ(maxListX.Max(), maxListY.Max(), maxListZ.Max());

                var cpbbMin = model.Instances.New<IfcCartesianPoint>();
                cpbbMin.SetXYZ(Convert.ToDouble(boundingBox.Min.X / feetToMeter), Convert.ToDouble(boundingBox.Min.Y) / feetToMeter, Convert.ToDouble(boundingBox.Min.Z) / feetToMeter);
                var cpbbMax = model.Instances.New<IfcCartesianPoint>();
                cpbbMax.SetXYZ(Convert.ToDouble(boundingBox.Max.X) / feetToMeter, Convert.ToDouble(boundingBox.Max.Y) / feetToMeter, Convert.ToDouble(boundingBox.Max.Z) / feetToMeter);

                var material = model.Instances.New<IfcMaterial>();
                material.Name = "transparent";

                var colorDict = new Dictionary<string, string>();
                colorDict = CreateColors();

                string bezeichnung = "123";

                double rot, gruen, blau;
                if (colorDict.ContainsKey(bezeichnung))
                {
                    rot = Convert.ToDouble(colorDict[bezeichnung].Split('/')[0]);
                    gruen = Convert.ToDouble(colorDict[bezeichnung].Split('/')[1]);
                    blau = Convert.ToDouble(colorDict[bezeichnung].Split('/')[2]);
                }
                else
                {
                    rot = 255;
                    gruen = 250;
                    blau = 240;
                }

                var colourRgb = model.Instances.New<IfcColourRgb>();
                colourRgb.Red = rot / 255.0;
                colourRgb.Green = gruen / 255.0;
                colourRgb.Blue = blau / 255.0;

                var surfaceStyleRendering = model.Instances.New<IfcSurfaceStyleRendering>();
                surfaceStyleRendering.SurfaceColour = colourRgb;
                //surfaceStyleRendering.Transparency = 0.5;

                var surfaceStyle = model.Instances.New<IfcSurfaceStyle>();
                surfaceStyle.Styles.Add(surfaceStyleRendering);
                surfaceStyle.Name = "building";

                var presentation = model.Instances.New<IfcPresentationStyleAssignment>();
                presentation.Styles.Add(surfaceStyle);

                var rectProf = model.Instances.New<IfcRectangleProfileDef>();
                rectProf.ProfileType = IfcProfileTypeEnum.AREA;
                rectProf.XDim = (cpbbMax.X - cpbbMin.X);
                rectProf.YDim = (cpbbMax.Y - cpbbMin.Y);

                double midX = (cpbbMax.X + cpbbMin.X) / 2;
                double midY = (cpbbMax.Y + cpbbMin.Y) / 2;
                double midZ = cpbbMin.Z;

                //insert point
                var insertPoint = model.Instances.New<IfcCartesianPoint>();
                insertPoint.SetXY(0, 0);
                rectProf.Position = model.Instances.New<IfcAxis2Placement2D>();
                rectProf.Position.Location = insertPoint;
                rectProf.Position.RefDirection = model.Instances.New<IfcDirection>();
                rectProf.Position.RefDirection.SetXY(1, 0);

                var spaceSolid = model.Instances.New<IfcExtrudedAreaSolid>();
                spaceSolid.Depth = cpbbMax.Z - cpbbMin.Z;
                spaceSolid.SweptArea = rectProf;
                spaceSolid.ExtrudedDirection = model.Instances.New<IfcDirection>();
                spaceSolid.ExtrudedDirection.SetXYZ(0, 0, 1);

                var position = model.Instances.New<IfcCartesianPoint>();
                position.SetXYZ(midX, midY, midZ);
                spaceSolid.Position = model.Instances.New<IfcAxis2Placement3D>();
                spaceSolid.Position.Location = position;

                spaceSolid.Position.RefDirection = model.Instances.New<IfcDirection>();
                spaceSolid.Position.Axis = model.Instances.New<IfcDirection>();
                spaceSolid.Position.RefDirection.SetXYZ(1, 0, 0);
                spaceSolid.Position.Axis.SetXYZ(0, 0, 1);

                var style = model.Instances.New<IfcStyledItem>();
                style.Item = spaceSolid;
                style.Styles.Add(presentation);
                spaceSolid.StyledByItem.Append(style);

                var styledRepresentation = model.Instances.New<IfcStyledRepresentation>();
                styledRepresentation.Items.Add(style);

                var shapeRepresentation = model.Instances.New<IfcShapeRepresentation>();
                var geomRepContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();

                shapeRepresentation.Items.Add(spaceSolid);
                shapeRepresentation.ContextOfItems = geomRepContext;
                shapeRepresentation.RepresentationIdentifier = "Body";
                shapeRepresentation.RepresentationType = "SweptSolid";

                styledRepresentation.ContextOfItems = geomRepContext;

                var spaceRep = model.Instances.New<IfcProductDefinitionShape>();
                spaceRep.Representations.Add(shapeRepresentation);
                space.Representation = spaceRep;

                var layerAssignment = model.Instances.New<IfcPresentationLayerAssignment>();
                layerAssignment.Name = "LandBIM";
                layerAssignment.AssignedItems.Add(shapeRepresentation);

                var projectBasePoint = model.Instances.New<IfcCartesianPoint>();
                projectBasePoint.SetXYZ(pbp.X, pbp.Y, pbp.Z);

                var localPlacement = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>();
                ax3D.Location = projectBasePoint;
                ax3D.RefDirection = model.Instances.New<IfcDirection>();
                ax3D.RefDirection.SetXYZ(1, 0, 0);
                ax3D.Axis = model.Instances.New<IfcDirection>();
                ax3D.Axis.SetXYZ(0, 0, 1);
                localPlacement.RelativePlacement = ax3D;
                space.ObjectPlacement = localPlacement;

                var relAggregates = model.Instances.New<IfcRelAggregates>();
                relAggregates.RelatingObject = ifcProject;
                relAggregates.RelatedObjects.Add(space);

                //set a few basic properties
                model.Instances.New<IfcRelDefinesByProperties>(relSpace =>
                {
                    relSpace.RelatedObjects.Add(space);
                    relSpace.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pSetSpace =>
                    {
                        pSetSpace.Name = "BauantragBruttoflächen";
                        pSetSpace.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = "IstBGF";
                                    p.NominalValue = new IfcBoolean(true);
                                }),
                            });
                        pSetSpace.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertyEnumeratedValue>(p =>
                                {
                                    p.EnumerationReference = model.Instances.New<IfcPropertyEnumeration>(pe =>
                                    {
                                        pe.Name = "Raumumschließung";
                                        pe.EnumerationValues.Add(new IfcLabel("REGELFALL"));
                                        pe.EnumerationValues.Add(new IfcLabel("SONDERFALL"));
                                    });
                                    p.Name = "Raumumschließung";
                                    p.EnumerationValues.Add(new IfcLabel("REGELFALL"));
                                }),
                            });
                    });
                });

                double bbHeight = cpbbMax.Z - cpbbMin.Z;
                double bbLength = cpbbMax.X - cpbbMin.X;
                double bbWidth = cpbbMax.Y - cpbbMin.Y;
                double bbFloorArea = bbLength * bbWidth;
                double bbFloorVolume = bbFloorArea * bbHeight;

                var ifcQuantityHeight = HeightSpaceQuantity(model, bbHeight, "Qto_SpaceBaseQuantites", "Height");
                var ifcQuantityArea  = AreaSpaceQuantity(model, bbFloorArea, "Qto_SpaceBaseQuantites", "GrossFloorArea");
                var ifcQuantityVolume = VolumeSpaceQuantity(model, bbFloorVolume, "Qto_SpaceBaseQuantites", "GrossVolume");

                var ifcElementQuantity = model.Instances.New<IfcElementQuantity>(eq =>
                {
                    eq.Name = "Qto_SpaceBaseQuantites";
                    eq.Description = "Measurement quantity";
                    eq.Quantities.Add(ifcQuantityHeight);
                    eq.Quantities.Add(ifcQuantityArea);
                    eq.Quantities.Add(ifcQuantityVolume);
                });

                model.Instances.New<IfcRelDefinesByProperties>(rdbp =>
                {
                    rdbp.Name = "Qto_SpaceBaseQuantities";
                    rdbp.Description = "";
                    rdbp.RelatedObjects.Add(space);
                    rdbp.RelatingPropertyDefinition = ifcElementQuantity;
                });

                txn.Commit();
                return space;
            }
        }
    }
}
