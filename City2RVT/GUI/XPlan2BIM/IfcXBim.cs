using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

namespace City2RVT.GUI.XPlan2BIM
{
    public class IfcXBim
    {
        public static IfcStore CreateandInitModel(string projectName, Document doc)
        {
            //first we need to set up some credentials for ownership of data in the new model
            var credentials = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "LandBIMTeam",
                ApplicationFullName = "Revit to IFC Application",
                ApplicationIdentifier = "IFC.exe",
                ApplicationVersion = "1.0",
                EditorsFamilyName = "Team",
                EditorsGivenName = "LandBIM",
                EditorsOrganisationName = "LandBIMTeam"
            };
            //now we can create an IfcStore, it is in Ifc4 format and will be held in memory rather than in a database
            //database is normally better in performance terms if the model is large >50MB of Ifc or if robust transactions are required

            using (var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, Xbim.IO.XbimStoreType.InMemoryModel))
            {
                //Begin a transaction as all changes to a model are ACID
                using (var txn = model.BeginTransaction("Initialise Model"))
                {
                    //create a project
                    var project = model.Instances.New<IfcProject>();
                    //var project = model.Instances.FirstOrDefault<IfcProject>();
                    //set the units to metres
                    project.Initialize(ProjectUnits.SIUnitsUK);
                    project.Name = projectName;
                    project.UnitsInContext.SetOrChangeSiUnit(IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE, null);

                    //set a few basic properties
                    model.Instances.New<IfcRelDefinesByProperties>(rel =>
                    {
                        rel.RelatedObjects.Add(project);
                        rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                        {
                            pset.Name = "BauantragAllgemein";

                            pset.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    Parameter projInfoParam = doc.ProjectInformation.LookupParameter("Bezeichnung des Bauvorhabens");
                                    string projInfoParamValue = projInfoParam.AsString();
                                    string rfaNameAttri = "Bezeichnung des Bauvorhabens";
                                    p.Name = rfaNameAttri;
                                    p.NominalValue = new IfcLabel(projInfoParamValue);
                                }),
                        });
                            pset.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertyEnumeratedValue>(p =>
                                {
                                    Parameter projInfoParam = doc.ProjectInformation.LookupParameter("Art der Maßnahme");
                                    string projInfoParamValue = projInfoParam.AsString();
                                    string rfaNameAttri = "Art der Maßnahme";
                                    p.Name = rfaNameAttri;
                                    p.EnumerationValues.Add(new IfcLabel(projInfoParamValue));
                                    //p.EnumerationValues.Add(new IfcLabel("ERRICHTUNG"));
                                    //p.EnumerationValues.Add(new IfcLabel("AENDERUNG"));
                                    //p.EnumerationValues.Add(new IfcLabel("NUTZUNGSAENDERUNG_OHNE_BAULICHE_AENDERUNG"));
                                    //p.EnumerationValues.Add(new IfcLabel("NUTZUNGSAENDERUNG_MIT_BAULICHER_AENDERUNG"));
                                    //p.EnumerationValues.Add(new IfcLabel("BESEITIGUNG"));

                                }),
                        });
                            pset.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    Parameter projInfoParam = doc.ProjectInformation.LookupParameter("Art des Gebäudes");
                                    string projInfoParamValue = projInfoParam.AsString();
                                    string rfaNameAttri = "Art des Gebäudes";
                                    p.Name = rfaNameAttri;
                                    p.NominalValue = new IfcLabel(projInfoParamValue);
                                }),
                        });
                            pset.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertyEnumeratedValue>(p =>
                                {
                                    Parameter projInfoParam = doc.ProjectInformation.LookupParameter("Gebäudeklasse");
                                    string projInfoParamValue = projInfoParam.AsString();
                                    string rfaNameAttri = "Gebäudeklasse";
                                    p.Name = rfaNameAttri;
                                    p.EnumerationValues.Add(new IfcLabel(projInfoParamValue));
                                    //p.EnumerationValues.Add(new IfcLabel("GEBAEUDEKLASSE_1"));
                                    //p.EnumerationValues.Add(new IfcLabel("GEBAEUDEKLASSE_2"));
                                    //p.EnumerationValues.Add(new IfcLabel("GEBAEUDEKLASSE_3"));
                                    //p.EnumerationValues.Add(new IfcLabel("GEBAEUDEKLASSE_4"));
                                    //p.EnumerationValues.Add(new IfcLabel("GEBAEUDEKLASSE_5"));

                                }),
                        });
                            pset.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertyEnumeratedValue>(p =>
                                {
                                    Parameter projInfoParam = doc.ProjectInformation.LookupParameter("Bauweise");
                                    string projInfoParamValue = projInfoParam.AsString();
                                    string rfaNameAttri = "Bauweise";
                                    p.Name = rfaNameAttri;
                                    p.EnumerationValues.Add(new IfcLabel(projInfoParamValue));
                                    //p.EnumerationValues.Add(new IfcLabel("OFFENE_BAUWEISE_EINZELHAUS"));
                                    //p.EnumerationValues.Add(new IfcLabel("OFFENE_BAUWEISE_DOPPELHAUS"));
                                    //p.EnumerationValues.Add(new IfcLabel("OFFENE_BAUWEISE_HAUSGRUPPE"));
                                    //p.EnumerationValues.Add(new IfcLabel("OFFENE_BAUWEISE_GESCHOSSBAU"));
                                    //p.EnumerationValues.Add(new IfcLabel("GESCHLOSSENE_BAUWEISE"));
                                    //p.EnumerationValues.Add(new IfcLabel("ABWEICHENDE_BAUWEISE"));
                                }),
                        });
                        });
                    });

                    //now commit the changes, else they will be rolled back at the end of the scope of the using statement
                    txn.Commit();
                }
                return model;
            }


            
        }

        public static Dictionary<string, string> CreateMaterial()
        {
            var colorList = new Dictionary<string, string>();

            colorList.Add("BP_StrassenVerkehrsFlaeche", "220/220/220");
            colorList.Add("BP_GewaesserFlaeche", "100/149/237");
            colorList.Add("BP_UeberbaubareGrundstuecksFlaeche", "160/082/045");
            colorList.Add("BP_Bereich", "0/0/0");
            colorList.Add("BP_Plan", "0/0/0");
            colorList.Add("BP_BaugebietsTeilFlaeche", "233/150/122");
            colorList.Add("BP_GemeinbedarfsFlaeche", "255/106/106");
            colorList.Add("BP_KennzeichnungsFlaeche", "110/139/061");
            colorList.Add("BP_ErhaltungsBereichFlaeche", "0/255/0");

            return colorList;
        }

        public static IfcSite CreateSite(IfcStore model, IList<XYZ> topoPoints, /*Transform transf, */ParameterSet topoParams, string bezeichnung, TopographySurface topoSurf, XYZ pbp, Document doc)
        {
            double feetToMeter = 1.0 / 0.3048;

            using (var txn = model.BeginTransaction("Create Ifc Export for topography"))
            {
                var site = model.Instances.New<IfcSite>();
                site.Name = "Site for " + "Test";
                site.RefElevation = 0.0;
                site.Description = "Site fuer Nutzungsflaeche " + bezeichnung;
                site.RefLatitude = new List<long> { 1, 2, 3, 4 };
                

                site.GlobalId = Guid.NewGuid();

                var ifcAddress = model.Instances.New<Xbim.Ifc4.ActorResource.IfcPostalAddress>(a =>
                {
                    a.Description = "Keine Addresse pro Flurstück/Nutzungfläche sondern für das Baugrundstück bzw. Bauobjekt. ";

                    Parameter addressLineParam = doc.ProjectInformation.LookupParameter("Address Line");
                    string addressLineParamValue = addressLineParam.AsString();
                    a.AddressLines.Add(addressLineParamValue);

                    Parameter postalCodeParam = doc.ProjectInformation.LookupParameter("Postal Code");
                    string postalCodeParamValue = postalCodeParam.AsString();
                    a.PostalCode = postalCodeParamValue;

                    Parameter townParam = doc.ProjectInformation.LookupParameter("Town");
                    string townParamValue = townParam.AsString();
                    a.Town = townParamValue;

                    Parameter regionParam = doc.ProjectInformation.LookupParameter("Region");
                    string regionParamValue = regionParam.AsString();
                    a.Region = regionParamValue;

                    Parameter countryParam = doc.ProjectInformation.LookupParameter("Country");
                    string countryParamValue = countryParam.AsString();
                    a.Country = countryParamValue;

                });
                site.SiteAddress = ifcAddress;

                var curveSet = model.Instances.New<IfcPolyline>();
                var loop = model.Instances.New<IfcPolyLoop>();
                int i = 0;
                foreach (var x in topoPoints)
                {
                    //XYZ transPoint = transf.OfPoint(new XYZ(topoPoints[i].X, topoPoints[i].Y, topoPoints[i].Z));
                    XYZ transPoint = new XYZ(topoPoints[i].X, topoPoints[i].Y, topoPoints[i].Z);
                    var cartPoint = model.Instances.New<IfcCartesianPoint>();
                    cartPoint.SetXYZ(Convert.ToDouble(transPoint.X, System.Globalization.CultureInfo.InvariantCulture)/feetToMeter,
                        Convert.ToDouble(transPoint.Y, System.Globalization.CultureInfo.InvariantCulture) / feetToMeter,
                        Convert.ToDouble(transPoint.Z, System.Globalization.CultureInfo.InvariantCulture) / feetToMeter);

                    curveSet.Points.Add(cartPoint);
                    loop.Polygon.Add(cartPoint);

                    i++;
                }

                var projectBasePoint = model.Instances.New<IfcCartesianPoint>();
                //projectBasePoint.SetXYZ(0, 0, 0);
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

                var colorList = new Dictionary<string, string>();
                var bezeichner = new IfcXBim();
                colorList = CreateMaterial();

                double rot;
                double gruen;
                double blau;
                if (colorList.ContainsKey(bezeichnung))
                {
                    rot = Convert.ToDouble(colorList[bezeichnung].Split('/')[0]);
                    gruen = Convert.ToDouble(colorList[bezeichnung].Split('/')[1]);
                    blau = Convert.ToDouble(colorList[bezeichnung].Split('/')[2]);
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
                surfaceStyleRendering.Transparency = 50;

                var surfaceStyle = model.Instances.New<IfcSurfaceStyle>();
                surfaceStyle.Styles.Add(surfaceStyleRendering);
                surfaceStyle.Name = bezeichnung;

                var presentation = model.Instances.New<IfcPresentationStyleAssignment>();
                presentation.Styles.Add(surfaceStyle);

                var curveSetFace = model.Instances.New<IfcFaceBasedSurfaceModel>();
                curveSetFace.FbsmFaces.Add(connFaceSet);

                var style = model.Instances.New<IfcStyledItem>();
                style.Item = curveSetFace;
                style.Styles.Add(presentation);
                curveSetFace.StyledByItem.Append(style);

                var styledRepresentation = model.Instances.New<IfcStyledRepresentation>();
                styledRepresentation.Items.Add(style);
                //style.Item.re

                var materialDefRepresentation = model.Instances.New<IfcMaterialDefinitionRepresentation>();
                materialDefRepresentation.Representations.Add(styledRepresentation);

                material.HasRepresentation.Append(materialDefRepresentation);


                //shape definition 1
                var umringShape = model.Instances.New<IfcShapeRepresentation>();
                var modelContextKrone = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                umringShape.ContextOfItems = modelContextKrone;
                umringShape.RepresentationIdentifier = "Body";
                umringShape.RepresentationType = "SweptSolid";
                umringShape.Items.Add(curveSetFace);

                //shape definition 2
                var umringShape2 = model.Instances.New<IfcShapeRepresentation>();
                var modelContextKrone2 = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                umringShape2.ContextOfItems = modelContextKrone;
                umringShape2.RepresentationIdentifier = "FootPrint";
                umringShape2.RepresentationType = "Curve2D";
                umringShape2.Items.Add(curveSet);

                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(umringShape);
                rep.Representations.Add(umringShape2);
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
                        //key.Trim(' ');
                        //value.Trim(' ');

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

                foreach (var s in paramDict)
                {
                    //set a few basic properties
                    model.Instances.New<IfcRelDefinesByProperties>(rel =>
                    {
                        rel.RelatedObjects.Add(site);
                        rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                        {
                            pset.Name = "Basic set of properties";
                            pset.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    string rfaNameAttri = s.Key.Substring(s.Key.LastIndexOf(':') + 1);
                                    p.Name = rfaNameAttri;
                                    p.NominalValue = new IfcText(s.Value);
                                }),
                            });
                        });
                    });
                }

                foreach (var s in paramDict)
                {
                    //set a few basic properties
                    model.Instances.New<IfcRelDefinesByProperties>(rel =>
                    {
                        rel.RelatedObjects.Add(site);
                        rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                        {
                            pset.Name = "BauantragGeokodierung";

                            string verortung = s.Key.Substring(s.Key.LastIndexOf(':') + 1);
                            string verortungTrim = verortung.Trim(' ');


                            if (verortungTrim == "Gemarkung_Nummer" || verortungTrim == "Flure" || verortungTrim == "Flurstuecksnummer")
                            {
                                pset.HasProperties.AddRange(new[]
                                {
                                model.Instances.New<IfcPropertySingleValue>(p =>
                                    {
                                        //System.Windows.Forms.MessageBox.Show(s.Key.Substring(s.Key.LastIndexOf(':') + 1));
                                        //string verortung = s.Key.Substring(s.Key.LastIndexOf(':') + 1);
                                        //const string gemNummer = "Gemarkung_Nummer";
                                        //string verortungTrim = verortung.Trim(' ');
                                        //System.Windows.Forms.MessageBox.Show("ä" + verortungTrim);
                                        switch (verortungTrim)
                                        {
                                            case "Gemarkung_Nummer":
                                                //System.Windows.Forms.MessageBox.Show("switch");
                                                p.Name = "Gemarkungen";
                                                p.NominalValue = new IfcText(s.Value);
                                                break;
                                            case ("Flure"):
                                                p.Name = "Flure";
                                                p.NominalValue = new IfcText(s.Value);
                                                break;
                                            case ("Flurstuecksnummer"):
                                                p.Name = "Flurstücke";
                                                p.NominalValue = new IfcText(s.Value);
                                                break;
                                            default:
                                                break;
                                        }
                                    }),
                                });
                            }


                        });
                    });
                }

                txn.Commit();
                return site;
            }
        }

        //public static IfcProject UpdateProject()
        //{

        //}
    }
}
