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

using City2RVT.Calc;
using Xbim.Ifc4.ActorResource;
using Xbim.Ifc4.QuantityResource;
using System.Globalization;

namespace City2RVT.GUI.XPlan2BIM
{
    public class IfcXBim
    {
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

        private static void CreateQuantity(IfcStore model, IfcSite site, TopographySurface topoSurf)
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
                //qa.AreaValue = 100.0;  
                var area = topoSurf.get_Parameter(BuiltInParameter.PROJECTED_SURFACE_AREA).AsValueString();
                string[] areaSplit = area.Split(' ');
                string areaWithoutUnit = areaSplit[0];
                qa.AreaValue = Convert.ToDouble(areaWithoutUnit,CultureInfo.InvariantCulture);

            });

            //lets create the IfcElementQuantity
            var ifcElementQuantity = model.Instances.New<IfcElementQuantity>(eq =>
            {
                eq.Name = "Test:IfcElementQuantity";
                eq.Description = "Measurement quantity";
                eq.Quantities.Add(ifcQuantityArea);
                //eq.Quantities.Add(ifcQuantityCount);
                //eq.Quantities.Add(ifcQuantityLength);
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
        public static IfcSite CreateSite(IfcStore model, IList<XYZ> topoPoints, ParameterSet topoParams, string bezeichnung, TopographySurface topoSurf, 
            XYZ pbp, Document doc)
        {
            double feetToMeter = 1.0 / 0.3048;
            var ifcProject = model.Instances.OfType<IfcProject>().FirstOrDefault();




            using (var txn = model.BeginTransaction("Create Ifc Export for topography"))
            {
                var site = model.Instances.New<IfcSite>();
                //site.Representatio
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

                var ifcAddress = model.Instances.New<Xbim.Ifc4.ActorResource.IfcPostalAddress>(a =>
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
                int i = 0;
                foreach (var x in topoPoints)
                {
                    XYZ topoPoint = new XYZ(topoPoints[i].X, topoPoints[i].Y, topoPoints[i].Z);
                    var cartPoint = model.Instances.New<IfcCartesianPoint>();
                    cartPoint.SetXYZ(Convert.ToDouble(topoPoint.X, System.Globalization.CultureInfo.InvariantCulture)/feetToMeter,
                        Convert.ToDouble(topoPoint.Y, System.Globalization.CultureInfo.InvariantCulture) / feetToMeter,
                        Convert.ToDouble(topoPoint.Z, System.Globalization.CultureInfo.InvariantCulture) / feetToMeter);

                    curveSet.Points.Add(cartPoint);
                    loop.Polygon.Add(cartPoint);

                    i++;
                }

                var projectBasePoint = model.Instances.New<IfcCartesianPoint>();
                projectBasePoint.SetXYZ(pbp.X, pbp.Y, pbp.Z);

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
                var bezeichner = new IfcXBim();
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
                surfaceStyleRendering.Transparency = 0.5;

                var surfaceStyle = model.Instances.New<IfcSurfaceStyle>();
                surfaceStyle.Styles.Add(surfaceStyleRendering);
                surfaceStyle.Name = bezeichnung;

                var presentation = model.Instances.New<IfcPresentationStyleAssignment>();
                presentation.Styles.Add(surfaceStyle);

                var curveSetFace = model.Instances.New<IfcFaceBasedSurfaceModel>();
                //var curveSetFace = model.Instances.New<IfcGeometricRepresentationItem>();
                curveSetFace.FbsmFaces.Add(connFaceSet);

                var style = model.Instances.New<IfcStyledItem>();
                style.Item = curveSetFace;
                style.Styles.Add(presentation);
                curveSetFace.StyledByItem.Append(style);

                var styledRepresentation = model.Instances.New<IfcStyledRepresentation>();
                styledRepresentation.Items.Add(style);

                var materialDefRepresentation = model.Instances.New<IfcMaterialDefinitionRepresentation>();
                materialDefRepresentation.Representations.Add(styledRepresentation);

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
                shapeRepresentation.Items.Add(curveSetFace);

                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shapeRepresentation);
                site.Representation = rep;

                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>();
                ax3D.Location = projectBasePoint;
                ax3D.RefDirection = model.Instances.New<IfcDirection>();
                ax3D.RefDirection.SetXYZ(0, 1, 0);
                ax3D.Axis = model.Instances.New<IfcDirection>();
                ax3D.Axis.SetXYZ(0, 0, 1);
                lp.RelativePlacement = ax3D;
                site.ObjectPlacement = lp;


                List<string> guidList = new List<string>();
                foreach (Parameter v in topoParams)
                {
                    if (v.IsShared)
                    {
                        guidList.Add(v.GUID.ToString());
                    }
                }

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
                model.Instances.New<IfcRelDefinesByProperties>(relBasic =>
                {
                    relBasic.RelatedObjects.Add(site);
                    relBasic.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(psetBasic =>
                    {
                        psetBasic.Name = "Surface properties";
                        foreach (var topoParam in paramDict)
                        {
                            IfcXBim ifcXBim = new IfcXBim();
                            ifcXBim.setSurfaceProperties(psetBasic, model, topoParam);
                        }
                    });
                });

                //set Quantities
                CreateQuantity(model, site, topoSurf);

                //set a few basic properties
                model.Instances.New<IfcRelDefinesByProperties>(relGeokod =>
                {
                    relGeokod.RelatedObjects.Add(site);
                    relGeokod.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(psetGeokod =>
                    {
                        psetGeokod.Name = "BauantragGeokodierung";

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
                    });
                });

                var relAggregates = model.Instances.New<IfcRelAggregates>();
                relAggregates.RelatingObject = ifcProject;
                relAggregates.RelatedObjects.Add(site);

                txn.Commit();
                return site;
            }
        }
    }
}
