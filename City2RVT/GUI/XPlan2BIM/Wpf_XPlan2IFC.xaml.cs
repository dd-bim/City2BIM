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

                tx.Commit();
            }
            return r;
        }

<<<<<<< HEAD
        private void Button_Click_IfcExport(object sender, RoutedEventArgs e)
=======
        private void Button_Click_StartIfcExport(object sender, RoutedEventArgs e)
>>>>>>> 372cb7d4fa3223977a04f077749853b8c1ef220d
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

            FilteredElementCollector topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);
            var view = commandData.Application.ActiveUIDocument.ActiveView as View3D;


            string folder;
            if (string.IsNullOrWhiteSpace(ifc_Location.Text))
            {                
                folder = @"D:\Daten";
            }
            else
            {
                folder = ifc_Location.Text;
            }
            string name = "ifc_export_without_topography";


            using (TransactionGroup transGroup = new TransactionGroup(doc))
            {
                transGroup.Start("Transaction Group");

                using (Transaction firstTrans = new Transaction(doc))
                {
                    firstTrans.Start("First Transaction");

                    var preHide = new List<ElementId>();

                    foreach (var id in topoCollector)
                    {
                        preHide.Add(id.Id);
                    }

                    if (view != null)
                    {
                        view.HideElements(preHide);
                    }
                    firstTrans.Commit();
                }

                using (Transaction secondTrans = new Transaction(doc))
                {
                    secondTrans.Start("Second Transaction");
                    IFCExportOptions IFCOptions = new IFCExportOptions();
                    IFCOptions.FileVersion = IFCVersion.IFC4;
                    IFCOptions.FilterViewId = view.Id;
                    IFCOptions.AddOption("ActiveViewId", view.Id.ToString());
                    IFCOptions.AddOption("ExportVisibleElementsInView", "true");
                    IFCOptions.AddOption("VisibleElementsOfCurrentView", "true");
                    IFCOptions.AddOption("ExportSchedulesAsPsets", "true");
                    IFCOptions.AddOption("IncludeSiteElevation", "true");
                    IFCOptions.AddOption("ExportPartsAsBuildingElements", "true");
                    //Export the model to IFC
                    doc.Export(folder, name, IFCOptions);

                    secondTrans.Commit();
                }

                transGroup.RollBack();

                
            }



            string original = folder + "\\" + name + ".ifc";        

            #region standardifcexport
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
            #endregion standardifcexport                    

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

                using (var txn = model.BeginTransaction("Create Project Information"))
                {
                    //var ifcproject = model.Instances.OfType<IfcProject>();
                    var ifcproject = model.Instances.FirstOrDefault<IfcProject>();

                    //set a few basic properties
                    model.Instances.New<IfcRelDefinesByProperties>(rel =>
                    {
                        rel.RelatedObjects.Add(ifcproject);
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
                                model.Instances.New<IfcPropertySingleValue>(p =>
                                    {
                                        Parameter projInfoParam = doc.ProjectInformation.LookupParameter("Art der Maßnahme");
                                        string projInfoParamValue = projInfoParam.AsString();
                                        string rfaNameAttri = "Art der Maßnahme";
                                        p.Name = rfaNameAttri;
                                        p.NominalValue = new IfcLabel(projInfoParamValue);
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
                                model.Instances.New<IfcPropertySingleValue>(p =>
                                    {
                                        Parameter projInfoParam = doc.ProjectInformation.LookupParameter("Gebäudeklasse");
                                        string projInfoParamValue = projInfoParam.AsString();
                                        string rfaNameAttri = "Gebäudeklasse";
                                        p.Name = rfaNameAttri;
                                        p.NominalValue = new IfcLabel(projInfoParamValue);
                                    }),
                            });

                            pset.HasProperties.AddRange(new[]
                            {
                                model.Instances.New<IfcPropertySingleValue>(p =>
                                    {
                                        Parameter projInfoParam = doc.ProjectInformation.LookupParameter("Bauweise");
                                        string projInfoParamValue = projInfoParam.AsString();
                                        string rfaNameAttri = "Bauweise";
                                        p.Name = rfaNameAttri;
                                        p.NominalValue = new IfcLabel(projInfoParamValue);
                                    }),
                            });

                        });
                    });
                    txn.Commit();
                }

                    string save;

                if (string.IsNullOrWhiteSpace(ifc_name_textbox.Text))
                {                    
                    save = @"D:\Daten\revit2ifc.ifc";
                }
                else
                {
                    save = @"" + ifc_Location.Text + "\\" + ifc_name_textbox.Text + ".ifc";
                    //System.Windows.Forms.MessageBox.Show(save);
                }

                model.SaveAs(@save);
            }  
        }

        //string locationFolder;
<<<<<<< HEAD
        private void Button_Click_FileLocation(object sender, RoutedEventArgs e)
=======
        private void Button_Click_SetIfcLocation(object sender, RoutedEventArgs e)
>>>>>>> 372cb7d4fa3223977a04f077749853b8c1ef220d
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

<<<<<<< HEAD
=======
        private void Button_Click_SetIfcFile(object sender, RoutedEventArgs e)
        {
            Reader.FileDialog winexp = new Reader.FileDialog();
            ifc_file_textbox.Text = winexp.ImportPath(Reader.FileDialog.Data.IFC);
        }
>>>>>>> 372cb7d4fa3223977a04f077749853b8c1ef220d

        private void ifc_name_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click_OhneTopo(object sender, RoutedEventArgs e)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            var view = commandData.Application.ActiveUIDocument.ActiveView as View3D;


            using (TransactionGroup transGroup = new TransactionGroup(doc))
            {
                transGroup.Start("Transaction Group");

                using (Transaction firstTrans = new Transaction(doc))
                {
                    firstTrans.Start("First Transaction");

                    FilteredElementCollector topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);

                    var preHide = new List<ElementId>();

                    foreach (var id in topoCollector)
                    {
                        preHide.Add(id.Id);
                    }

                    if (view != null)
                    {
                        view.HideElements(preHide);
                    }

                    firstTrans.Commit();
                }

                using (Transaction secondTrans = new Transaction(doc))
                {
                    secondTrans.Start("Second Transaction");

                    string folder = @"D:\Daten";
                    string name = "defaultExport_IFC4";

                    //Create an instance of IFCExportOptions
                    IFCExportOptions IFCOptions = new IFCExportOptions();
                    IFCOptions.FileVersion = IFCVersion.IFC4;
                    IFCOptions.FilterViewId = view.Id;
                    IFCOptions.AddOption("ActiveViedId", view.Id.ToString());
                    IFCOptions.AddOption("ExportVisibleElementsInView", "true");
                    IFCOptions.AddOption("VisibleElementsOfCurrentView", "true");
                    IFCOptions.AddOption("ExportSchedulesAsPsets", "true");
                    IFCOptions.AddOption("IncludeSiteElevation", "true");
                    IFCOptions.AddOption("ExportPartsAsBuildingElements", "true");

                    //Export the model to IFC
                    doc.Export(folder, name, IFCOptions);

                    secondTrans.Commit();

                }

                //transGroup.Assimilate();
                transGroup.RollBack();
            }            
        }
    }
}
