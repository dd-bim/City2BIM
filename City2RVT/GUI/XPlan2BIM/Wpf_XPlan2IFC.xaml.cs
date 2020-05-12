using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;


using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

using Xbim.Ifc;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;
using System.IO;

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

            // Set an icon using code
            Uri iconUri = new Uri("pack://application:,,,/City2RVT;component/img/IFC_32px_96dpi.ico", UriKind.RelativeOrAbsolute);
            this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);
        }

        private void Button_Click_IfcExport(object sender, RoutedEventArgs e)
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
            string ifcWithoutTopo = "ifc_export_without_topography";

            using (TransactionGroup transGroup = new TransactionGroup(doc))
            {
                transGroup.Start("Transaction Group");

                using (Transaction firstTrans = new Transaction(doc))
                {
                    firstTrans.Start("First Transaction");

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
                    doc.Export(folder, ifcWithoutTopo, IFCOptions);

                    secondTrans.Commit();
                }
                transGroup.RollBack();                
            }

            string original = Path.Combine(folder,ifcWithoutTopo + ".ifc"); 

            using (var model = IfcStore.Open(original))
            {
                foreach (var t in topoCollector)
                {
                    TopographySurface topoSurf = doc.GetElement(t.UniqueId.ToString()) as TopographySurface;
                    IList<XYZ> topoPoints = topoSurf.GetPoints();
                    ParameterSet topoParams = topoSurf.Parameters;

                    string bezeichnung = topoSurf.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
                    if (bezeichnung == null)
                    {
                        bezeichnung = "-";
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
                    var ifcproject = model.Instances.FirstOrDefault<IfcProject>();

                    //set a few basic properties
                    model.Instances.New<IfcRelDefinesByProperties>(rel =>
                    {
                        rel.RelatedObjects.Add(ifcproject);
                        rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                        {
                            pset.Name = "BauantragAllgemein";
                            IfcXBim ifcXBim = new IfcXBim();
                            ifcXBim.getProjectProperties(pset, model, doc, "Bezeichnung des Bauvorhabens");
                            ifcXBim.getProjectProperties(pset, model, doc, "Art der Maßnahme");
                            ifcXBim.getProjectProperties(pset, model, doc, "Art des Gebäudes");
                            ifcXBim.getProjectProperties(pset, model, doc, "Gebäudeklasse");
                            ifcXBim.getProjectProperties(pset, model, doc, "Bauweise");
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
                    save = Path.Combine(ifc_Location.Text, ifc_name_textbox.Text + ".ifc");
                }

                model.SaveAs(@save);
            }  
        }

        private void ifc_Location_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }


        private void ifc_name_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click_SetIfcLocation(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            DialogResult folder = dlg.ShowDialog();
            string locationFolder = dlg.SelectedPath;
            ifc_Location.Text = locationFolder;
        }
    }
}
