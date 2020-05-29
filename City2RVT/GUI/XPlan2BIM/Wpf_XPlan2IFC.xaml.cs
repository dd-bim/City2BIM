using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using System.Linq;


using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

using Xbim.Ifc;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using System.IO;
using City2RVT.Calc;

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
                    IFCOptions.AddOption("ExportInternalRevitPropertySets", "false");
                    IFCOptions.AddOption("ExportIFCCommonPropertySets", "true");


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
                    var ifcproject = model.Instances.FirstOrDefault<IfcProject>();

                    var geomRepContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();

                    double[] richtung = UTMcalc.AzimuthToLocalVector(angle);


                    geomRepContext.TrueNorth = model.Instances.New<IfcDirection>();
                    geomRepContext.TrueNorth.SetXY(richtung[0], richtung[1]);


                    ifcproject.RepresentationContexts.Add(geomRepContext);

                    //set a few basic properties
                    model.Instances.New<IfcRelDefinesByProperties>(rel =>
                    {
                        rel.RelatedObjects.Add(ifcproject);
                        rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                        {
                            pset.Name = "BauantragAllgemein";
                            IfcXBim ifcXBim = new IfcXBim();
                            ifcXBim.getProjectProperties(pset, model, doc, "Bezeichnung des Bauvorhabens");
                            ifcXBim.getProjectEnums(pset, model, doc, "Art der Maßnahme",artDerMassnahme);
                            ifcXBim.getProjectProperties(pset, model, doc, "Art des Gebäudes");
                            ifcXBim.getProjectEnums(pset, model, doc, "Gebäudeklasse", gebaeudeKlasse);
                            ifcXBim.getProjectEnums(pset, model, doc, "Bauweise", bauweise);
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
