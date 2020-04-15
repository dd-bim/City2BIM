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

            XYZ pbp = new XYZ(Math.Round((easting / feetToMeter),4), (Math.Round((northing / feetToMeter),4)), (Math.Round((elevation / feetToMeter),4)));

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

            const string original = @"D:\Daten\\Bauwerksmodell_IFC4.ifc";


            //var model = IfcStore.Open(original);

            //var model = IfcXBim.CreateandInitModel("XPlanungs Flaechen", doc);

            //Parameter myParam = doc.ProjectInformation.LookupParameter("Testproperty");
            //string para = myParam.AsString();
            //System.Windows.Forms.MessageBox.Show(para);



            ////Get an instance of IFCExportConfiguration
            //IFCExportConfiguration selectedConfig = modelSelection.Configuration;

            ////Get the current view Id, or -1 if you want to export the entire model
            //ElementId activeViewId = GenerateActiveViewIdFromDocument(doc);
            //selectedConfig.ActiveViewId =
            //        selectedConfig.UseActiveViewGeometry ? activeViewId.IntegerValue : -1;

            ////Update the IFCExportOptions
            //selectedConfig.UpdateOptions(IFCOptions, activeViewId);

            //////////var new_model = IfcXBim.CreateandInitModel("XPlanungs Flaechen", doc);

            //////////Transaction txnE = new Transaction(doc);
            //////////txnE.Start("Standard Export");

            //////////string folder = @"D:\Daten";
            //////////string name = "defaultExport_IFC4";

            ////////////Create an instance of IFCExportOptions
            //////////IFCExportOptions IFCOptions = new IFCExportOptions();
            //////////IFCOptions.FileVersion = IFCVersion.IFC4;

            ////////////Export the model to IFC
            //////////doc.Export(folder, name, IFCOptions);

            //////////ExportToIfc(doc);

            //////////txnE.Commit();



            //using (var model = IfcXBim.CreateandInitModel("XPlanungs Flaechen", doc))
            //{
            //    Transaction txnE = new Transaction(doc);
            //    txnE.Start("Standard Export");

            //    string folder = @"D:\Daten";
            //    string name = "defaultExport_IFC4";

            //    //Create an instance of IFCExportOptions
            //    IFCExportOptions IFCOptions = new IFCExportOptions();
            //    IFCOptions.FileVersion = IFCVersion.IFC4;

            //    //Export the model to IFC
            //    doc.Export(folder, name, IFCOptions);

            //    ExportToIfc(doc);

            //    txnE.Commit();
            //    //using (var txnE = model.BeginTransaction("Standard Export"))
            //    //{
            //    //    string folder = @"D:\Daten";
            //    //    string name = "defaultExport_IFC4";

            //    //    //Create an instance of IFCExportOptions
            //    //    IFCExportOptions IFCOptions = new IFCExportOptions();
            //    //    IFCOptions.FileVersion = IFCVersion.IFC4;

            //    //    //Export the model to IFC
            //    //    doc.Export(folder, name, IFCOptions);

            //    //    ExportToIfc(doc);

            //    //    txnE.Commit();
            //    //}
            //} 

            


            FilteredElementCollector topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);
            //System.Windows.Forms.MessageBox.Show(topoCollector.Count().ToString());

            //using (var model = IfcXBim.CreateandInitModel("XPlanungs Flaechen", doc))
            using (var model = IfcStore.Open(original))
            {
                foreach (var t in topoCollector)
                {
                    TopographySurface topoSurf = doc.GetElement(t.UniqueId.ToString()) as TopographySurface;
                    IList<XYZ> topoPoints = topoSurf.GetPoints();
                    ParameterSet topoParams = topoSurf.Parameters;

                    string bezeichnung = topoSurf.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
                    //string bezeichnung2 = default;

                    if (bezeichnung == null)
                    {
                        bezeichnung = "-";
                    }
                    else
                    {

                    }

                    // Diese IF-Anweisung bewirkt, dass die Referenzflächen, also die Boundingboxen, die "gedachte Flächen", d.h. keine existierenden Flächen sondern 
                    // nur für die Darstellung in Revit notwendig sind, nicht nach IFC exportiert werden.
                    if (bezeichnung.StartsWith("Reference plane") == false)
                    {
                        IfcXBim.CreateSite(model, topoPoints, /*transf, */topoParams, bezeichnung, topoSurf, pbp, doc);
                    }
                }

                var project = model.Instances.FirstOrDefault<IfcProject>(d => d.GlobalId == "1iMGscZQD1UuWMh2zmdLM4");

                using (var txn = model.BeginTransaction("Project modification"))
                {
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
                        });
                    });
                }


                string save = @"D:\Daten\\revit2ifc.ifc";
                model.SaveAs(@save);
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
