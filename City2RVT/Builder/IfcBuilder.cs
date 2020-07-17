using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
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
using City2RVT.GUI.XPlan2BIM;

namespace City2RVT.Builder
{
    public class IfcBuilder
    {
        /// <summary>
        /// The path string for the standard revit ifc export without surfaces. 
        /// </summary>
        /// <param name="ifc_Location"></param>
        /// <param name="doc"></param>
        /// <param name="commandData"></param>
        /// <returns></returns>
        public string startRevitIfcExport(string ifc_Location, Document doc, ExternalCommandData commandData)
        {
            FilteredElementCollector topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);
            var view = commandData.Application.ActiveUIDocument.ActiveView as View3D;

            string ifcWithoutTopo = "ifc_export_without_topography";

            using (TransactionGroup transGroup = new TransactionGroup(doc))
            {
                transGroup.Start("Export to IFC (except surfaces)");

                using (Transaction firstTrans = new Transaction(doc))
                {
                    firstTrans.Start("Hide topographies Transaction");

                    var hideTopoList = new List<ElementId>();

                    foreach (var id in topoCollector)
                    {
                        hideTopoList.Add(id.Id);
                    }

                    if (view != null)
                    {
                        view.HideElements(hideTopoList);
                    }
                    firstTrans.Commit();
                }

                using (Transaction secondTrans = new Transaction(doc))
                {
                    secondTrans.Start("Revit IFC Export Transaction");
                    IFCExportOptions IFCOptions = new IFCExportOptions();
                    IFCOptions.FileVersion = IFCVersion.IFC4;
                    IFCOptions.FilterViewId = view.Id;
                    IFCOptions.AddOption("ActiveViewId", view.Id.ToString());
                    IFCOptions.AddOption("ExportVisibleElementsInView", "true");
                    IFCOptions.AddOption("VisibleElementsOfCurrentView", "true");
                    IFCOptions.AddOption("ExportSchedulesAsPsets", "true");
                    IFCOptions.AddOption("IncludeSiteElevation", "true");
                    IFCOptions.AddOption("ExportPartsAsBuildingElements", "true");
                    IFCOptions.AddOption("ExportInternalRevitPropertySets", "false");
                    IFCOptions.AddOption("ExportIFCCommonPropertySets", "true");
                    IFCOptions.AddOption("ExportRoomsInView", "true");

                    //Export the model to IFC
                    doc.Export(ifc_Location, ifcWithoutTopo, IFCOptions);
                    secondTrans.Commit();
                }
                transGroup.RollBack();
            }

            string original = Path.Combine(ifc_Location, ifcWithoutTopo + ".ifc");
            return original;
        }

        /// <summary>
        /// Edits the standard revit IFC export
        /// </summary>
        /// <param name="model"></param>
        /// <param name="doc"></param>
        public void editRevitExport(IfcStore model, Document doc)
        {
            var transfClass = new Transformation();
            XYZ pbp = transfClass.getProjectBasePoint(doc);
            double angle = transfClass.getAngle(doc);

            using (var txn = model.BeginTransaction("Change Revit Export"))
            {
                var geomRepContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();

                //var ifcSite = ifcProject.Sites.FirstOrDefault();
                var ifcSite = model.Instances.FirstOrDefault<IfcSite>();
                ifcSite.Name = "Revit export default site";
                ifcSite.Description = "Default export by revit software";

                double[] richtung = UTMcalc.AzimuthToLocalVector(angle);
                geomRepContext.TrueNorth = model.Instances.New<IfcDirection>();
                geomRepContext.TrueNorth.SetXY(richtung[0], richtung[1]);

                var projectBasePoint = model.Instances.New<IfcCartesianPoint>();
                //projectBasePoint.SetXYZ(0, 0, 0);
                projectBasePoint.SetXYZ(pbp.X, pbp.Y, pbp.Z);
                var ax3D = model.Instances.New<IfcAxis2Placement3D>();
                ax3D.Location = projectBasePoint;

                geomRepContext.WorldCoordinateSystem = ax3D;

                var mapConversion = model.Instances.New<IfcMapConversion>();
                mapConversion.SourceCRS = geomRepContext;
                mapConversion.Eastings = pbp.X;
                mapConversion.Northings = pbp.Y;
                mapConversion.OrthogonalHeight = pbp.Z;
                mapConversion.XAxisAbscissa = richtung[0];
                mapConversion.XAxisOrdinate = richtung[1];
                mapConversion.Scale = 1.0;

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
                ifcSite.SiteAddress = ifcAddress;

                var projectedCRS = model.Instances.New<IfcProjectedCRS>(p =>
                {
                    p.Description = "CRS fuer Baugrundstueck. ";
                    IfcXBim ifcXBim = new IfcXBim();
                    p.Name = ifcXBim.setCrsValue(doc, "MapProjection") + " " + ifcXBim.setCrsValue(doc, "MapZone");
                    p.GeodeticDatum = ifcXBim.setCrsValue(doc, "GeodeticDatum");
                    p.VerticalDatum = ifcXBim.setCrsValue(doc, "VerticalDatum");
                    p.MapProjection = ifcXBim.setCrsValue(doc, "MapProjection");
                    p.MapZone = ifcXBim.setCrsValue(doc, "MapZone");
                });
                mapConversion.TargetCRS = projectedCRS;

                txn.Commit();
            }
        }

        public void CreateEnumeration(IfcStore model, IfcPropertyEnumeratedValue p)
        {
            using (var txn = model.BeginTransaction("Change Revit Export"))
            {
                p.EnumerationReference = model.Instances.New<IfcPropertyEnumeration>(pe =>
                {
                    pe.Name = "Nutzung";
                    pe.EnumerationValues.Add(new IfcLabel("WOHNEINHEIT_EIGENTUMSWOHNUNG"));
                    pe.EnumerationValues.Add(new IfcLabel("WOHNEINHEIT_MIETWOHNUNG"));
                    pe.EnumerationValues.Add(new IfcLabel("WOHNEINHEIT_SOZIALWOHNUNG"));
                    pe.EnumerationValues.Add(new IfcLabel("WOHNEINHEIT_GEWERBLICH"));
                    pe.EnumerationValues.Add(new IfcLabel("WOHNEINHEIT_FREIBERUFLICH"));
                    pe.EnumerationValues.Add(new IfcLabel("NUTZUNGSEINHEIT_GEWERBE"));
                });

                txn.Commit();
            }
        }

        public void CreateUsage(IfcStore model, IfcSpace room)
        {
            using (var txn = model.BeginTransaction("Set usage"))
            {
                //set a few basic properties
                model.Instances.New<IfcRelDefinesByProperties>(relSpace =>
                {
                    relSpace.RelatedObjects.Add(room);
                    relSpace.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pSetSpace =>
                    {
                        pSetSpace.Name = "BauantragNutzungseinheiten";
                        pSetSpace.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertyEnumeratedValue>(p =>
                                {
                                    p.EnumerationReference = model.Instances.New<IfcPropertyEnumeration>(pe =>
                                    {
                                        pe.Name = "Nutzung " + room.Name;
                                        pe.EnumerationValues.Add(new IfcLabel("WOHNEINHEIT_EIGENTUMSWOHNUNG"));
                                        pe.EnumerationValues.Add(new IfcLabel("WOHNEINHEIT_MIETWOHNUNG"));
                                        pe.EnumerationValues.Add(new IfcLabel("WOHNEINHEIT_SOZIALWOHNUNG"));
                                        pe.EnumerationValues.Add(new IfcLabel("WOHNEINHEIT_GEWERBLICH"));
                                        pe.EnumerationValues.Add(new IfcLabel("WOHNEINHEIT_FREIBERUFLICH"));
                                        pe.EnumerationValues.Add(new IfcLabel("NUTZUNGSEINHEIT_GEWERBE"));
                                    });
                                    p.Name = "Nutzung " + room.Name;
                                    p.EnumerationValues.Add(new IfcLabel("NUTZUNGSEINHEIT_GEWERBE"));
                                }),
                            });
                        pSetSpace.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = "Einheit";
                                    p.NominalValue = new IfcIdentifier("M2");
                                }),
                            });
                    });
                });
                txn.Commit();
            }                
        }

        public void CreateRoomArea(IfcStore model, IfcSpace room)
        {
            using (var txn = model.BeginTransaction("Set Netto-Raumflächen"))
            {
                //set a few basic properties
                model.Instances.New<IfcRelDefinesByProperties>(relSpace =>
                {
                    relSpace.RelatedObjects.Add(room);
                    relSpace.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pSetSpace =>
                    {
                        pSetSpace.Name = "BauantragNettoflächen";
                        pSetSpace.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertyEnumeratedValue>(p =>
                                {
                                    p.EnumerationReference = model.Instances.New<IfcPropertyEnumeration>(pe =>
                                    {
                                        pe.Name = "Art " + room.Name;
                                        pe.EnumerationValues.Add(new IfcLabel("NUF1 (Wohnen und Aufenthalt)"));
                                        pe.EnumerationValues.Add(new IfcLabel("NUF2 (Büroarbeit)"));
                                        pe.EnumerationValues.Add(new IfcLabel("NUF3 (Produktion, Hand- und Maschinenarbeit, Forschung und Entwicklung)"));
                                        pe.EnumerationValues.Add(new IfcLabel("NUF4 (Lagern, Verteilen und Verkaufen)"));
                                        pe.EnumerationValues.Add(new IfcLabel("NUF5 (Bildung, Unterricht und Kultur)"));
                                        pe.EnumerationValues.Add(new IfcLabel("NUF6 (Heilen und Pflegen)"));
                                        pe.EnumerationValues.Add(new IfcLabel("NUF7 (Sonstige Nutzung)"));
                                        pe.EnumerationValues.Add(new IfcLabel("TF (Technikfläche)"));
                                        pe.EnumerationValues.Add(new IfcLabel("VF (Verkehrsfläche)"));
                                    });
                                    p.Name = "Art " + room.Name;
                                    p.EnumerationValues.Add(new IfcLabel("NUF1 (Wohnen und Aufenthalt)"));
                                }),
                            });
                        pSetSpace.HasProperties.AddRange(new[]
                            {
                            model.Instances.New<IfcPropertyEnumeratedValue>(p =>
                                {
                                    p.EnumerationReference = model.Instances.New<IfcPropertyEnumeration>(pe =>
                                    {
                                        pe.Name = "Raumumschließung " + room.Name;
                                        pe.EnumerationValues.Add(new IfcLabel("REGELFALL"));
                                        pe.EnumerationValues.Add(new IfcLabel("SONDERFALL"));
                                    });
                                    p.Name = "Raumumschließung " + room.Name;;
                                    p.EnumerationValues.Add(new IfcLabel("REGELFALL"));
                                }),
                            });
                    });
                });

                try
                {
                    double bbHeight = room.Height.Value;
                    //double bbLength = cpbbMax.X - cpbbMin.X;
                    //double bbWidth = cpbbMax.Y - cpbbMin.Y;
                    double bbFloorArea = room.NetFloorArea.Value;
                    double bbFloorVolume = bbFloorArea * bbHeight;

                    IfcXBim.AreaSpaceQuantity(model, bbFloorArea, "Qto_SpaceBaseQuantites", "GrossFloorArea");
                    IfcXBim.HeightSpaceQuantity(model, bbHeight, "Qto_SpaceBaseQuantites", "Height");
                    IfcXBim.VolumeSpaceQuantity(model, bbFloorVolume, "Qto_SpaceBaseQuantites", "GrossVolume");
                }
                catch
                {
                    IfcXBim.AreaSpaceQuantity(model, 0, "Qto_SpaceBaseQuantites", "GrossFloorArea");
                    IfcXBim.HeightSpaceQuantity(model, 0, "Qto_SpaceBaseQuantites", "Height");
                    IfcXBim.VolumeSpaceQuantity(model, 0, "Qto_SpaceBaseQuantites", "GrossVolume");
                }

                txn.Commit();
            }
        }

        /// <summary>
        /// Creates Project Information for revit export
        /// </summary>
        /// <param name="model"></param>
        /// <param name="ifcProject"></param>
        /// <param name="doc"></param>
        public void createProjectInformation(IfcStore model, IfcProject ifcProject, Document doc)
        {
            List<string> artDerMassnahme = new List<string>();
            artDerMassnahme.Add("ERRICHTUNG");
            artDerMassnahme.Add("AENDERUNG");
            artDerMassnahme.Add("NUTZUNGSAENDERUNG_OHNE_BAULICHE_AENDERUNG");
            artDerMassnahme.Add("NUTZUNGSAENDERUNG_MIT_BAULICHER_AENDERUNG");
            artDerMassnahme.Add("BESEITIGUNG");

            List<string> gebaeudeKlasse = new List<string>();
            gebaeudeKlasse.Add("GEBAEUDEKLASSE_1");
            gebaeudeKlasse.Add("GEBAEUDEKLASSE_2");
            gebaeudeKlasse.Add("GEBAEUDEKLASSE_3");
            gebaeudeKlasse.Add("GEBAEUDEKLASSE_4");
            gebaeudeKlasse.Add("GEBAEUDEKLASSE_5");

            List<string> bauweise = new List<string>();
            bauweise.Add("OFFENE_BAUWEISE_EINZELHAUS");
            bauweise.Add("OFFENE_BAUWEISE_DOPPELHAUS");
            bauweise.Add("OFFENE_BAUWEISE_HAUSGRUPPE");
            bauweise.Add("OFFENE_BAUWEISE_GESCHOSSBAU");
            bauweise.Add("GESCHLOSSENE_BAUWEISE");
            bauweise.Add("ABWEICHENDE_BAUWEISE");

            using (var txn = model.BeginTransaction("Create Project Information"))
            {
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.RelatedObjects.Add(ifcProject);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "BauantragAllgemein";
                        IfcXBim ifcXBim = new IfcXBim();
                        ifcXBim.getProjectProperties(pset, model, doc, "Bezeichnung des Bauvorhabens");
                        ifcXBim.getProjectEnums(pset, model, doc, "Art der Massnahme", artDerMassnahme);
                        ifcXBim.getProjectProperties(pset, model, doc, "Art des Gebaeudes");
                        ifcXBim.getProjectEnums(pset, model, doc, "Gebaeudeklasse", gebaeudeKlasse);
                        ifcXBim.getProjectEnums(pset, model, doc, "Bauweise", bauweise);
                    });
                });
                txn.Commit();
            }
        }
    }
}
