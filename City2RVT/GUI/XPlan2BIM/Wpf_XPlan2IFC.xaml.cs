using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml;

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

using NLog;
using NLog.Targets;
using NLog.Config;

//using Xbim.Ifc;
//https://forums.autodesk.com/t5/revit-api-forum/revit-api-s-integration-with-xbim-geometry-microsoft-extensions/td-p/9270358

namespace City2RVT.GUI.XPlan2BIM
{
    /// <summary>
    /// Interaktionslogik für Wpf_XPlan2IFC.xaml
    /// </summary>
    public partial class Wpf_XPlan2IFC : Window
    {
        ExternalCommandData commandData;
        double feetToMeter = 1.0 / 0.3048;

        public Wpf_XPlan2IFC(ExternalCommandData cData)
        {
            commandData = cData;

            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            InitializeComponent();
        }

        /// <summary>
        /// Export current view to IFC
        /// </summary>
        static Result ExportToIfc(Document doc)
        {
            Result r = Result.Failed;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Export IFC");

                string desktop_path = Environment.GetFolderPath(
                  Environment.SpecialFolder.Desktop);

                IFCExportOptions opt = null;

                doc.Export(desktop_path, doc.Title, opt);

                tx.RollBack();

                r = Result.Succeeded;
            }
            return r;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            #region Transformation und UTM-Reduktion

            //Zuerst wird die Position des Projektbasispunkts bestimmt
            ProjectLocation projloc = doc.ActiveProjectLocation;
            ProjectPosition position_data = projloc.GetProjectPosition(XYZ.Zero);
            double angle = position_data.Angle;
            double elevation = position_data.Elevation;
            double easting = position_data.EastWest;
            double northing = position_data.NorthSouth;

            // Der Ostwert des PBB wird als mittlerer Ostwert für die UTM Reduktion verwendet.
            double xSchwPktFt = easting;
            double xSchwPktKm = (double)((xSchwPktFt / feetToMeter) / 1000);
            double xSchwPkt500 = xSchwPktKm - 500;
            double R = 1;

            Transform trot = Transform.CreateRotation(XYZ.BasisZ, -angle);
            XYZ vector = new XYZ(easting, northing, elevation);
            XYZ vectorRedu = vector / R;
            Transform ttrans = Transform.CreateTranslation(vectorRedu);
            Transform transf = trot.Multiply(ttrans);
            #endregion Transformation und UTM-Reduktion  

            var model = IfcXBim.CreateandInitModel("XPlanungs Flaechen");


            FilteredElementCollector topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);
            System.Windows.Forms.MessageBox.Show(topoCollector.Count().ToString());

            foreach (var t in topoCollector)
            {
                //System.Windows.Forms.MessageBox.Show(t.UniqueId.ToString());
                TopographySurface topoSurf = doc.GetElement(t.UniqueId.ToString()) as TopographySurface;
                var topoPoints = topoSurf.GetPoints();
                var topoParams = topoSurf.Parameters;


                using (var txn = model.BeginTransaction("Create Ifc Export for topography"))
                {
                    var site = model.Instances.New<IfcSite>();
                    site.Name = "Site for " + "Test";
                    site.RefElevation = 0.0;
                    site.Description = "Site fuer Nutzungsflaeche " + "Test";
                    site.RefLatitude = new List<long> { 1, 2, 3, 4 };
                    site.GlobalId = Guid.NewGuid();

                    var curveSet = model.Instances.New<IfcPolyline>();
                    var loop = model.Instances.New<IfcPolyLoop>();
                    int i = 0;
                    foreach (var x in topoPoints)
                    {
                        XYZ transPoint = transf.OfPoint(new XYZ(topoPoints[i].X, topoPoints[i].Y, topoPoints[i].Z));
                        var cartPoint = model.Instances.New<IfcCartesianPoint>();
                        cartPoint.SetXYZ(Convert.ToDouble(topoPoints[i].X, System.Globalization.CultureInfo.InvariantCulture),
                            Convert.ToDouble(topoPoints[i].Y, System.Globalization.CultureInfo.InvariantCulture),
                            Convert.ToDouble(topoPoints[i].Z, System.Globalization.CultureInfo.InvariantCulture));

                        curveSet.Points.Add(cartPoint);
                        loop.Polygon.Add(cartPoint);

                        i++;
                    }

                    var cartPoint1 = model.Instances.New<IfcCartesianPoint>();
                    cartPoint1.SetXYZ(0, 0, 0);

                    var facebound = model.Instances.New<IfcFaceBound>();
                    facebound.Bound = loop;

                    var outerFace = model.Instances.New<IfcFaceOuterBound>();
                    outerFace.Bound = loop;

                    var face = model.Instances.New<IfcFace>();
                    face.Bounds.Add(outerFace);

                    var connFaceSet = model.Instances.New<IfcConnectedFaceSet>();
                    connFaceSet.CfsFaces.Add(face);

                    var curveSetFace = model.Instances.New<IfcFaceBasedSurfaceModel>();
                    curveSetFace.FbsmFaces.Add(connFaceSet);

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
                    ax3D.Location = cartPoint1;
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
                                    //System.Windows.Forms.MessageBox.Show(key.ToString());
                                    //System.Windows.Forms.MessageBox.Show(value.ToString());
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

                    string save = @"D:\Daten\\revit2ifc.ifc";

                    model.SaveAs(@save);
                    txn.Commit();
                }
            }





        }

        //string locationFolder;
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            DialogResult folder = dlg.ShowDialog();
            string locationFolder = dlg.SelectedPath;
            System.Windows.Forms.MessageBox.Show(locationFolder.ToString());
            ifc_Location.Text = locationFolder;
            //Reader.FileDialog ifcLocation = new Reader.FileDialog();
            //ifc_Location.Text = ifcLocation.ImportPath(Reader.FileDialog.Data.XPlanGML);
        }

        private void ifc_Location_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }
    }
}
