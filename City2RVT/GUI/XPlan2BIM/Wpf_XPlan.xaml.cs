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

using NLog;
using NLog.Targets;
using NLog.Config;
using City2RVT.Reader;
using City2RVT.Builder;

namespace City2RVT.GUI.XPlan2BIM
{
    /// <summary>
    /// Interaktionslogik für Wpf_XPlan.xaml
    /// </summary>
    public partial class Wpf_XPlan : Window
    {
        ExternalCommandData commandData;
        private readonly Document doc;
        double feetToMeter = 1.0 / 0.3048;

        public Wpf_XPlan(Document doc,ExternalCommandData cData)
        {
            commandData = cData;
            this.doc = doc;

            InitializeComponent();

            Uri iconUri = new Uri("pack://application:,,,/City2RVT;component/img/XPlan_32px.ico", UriKind.RelativeOrAbsolute);
            this.Icon = BitmapFrame.Create(iconUri);

            xplan_file.Text = Prop_XPLAN_settings.FileUrl;
        }

        public class AxesFailure : IFailuresPreprocessor
        {
            //Eventhandler, der eine ignorierbare Warnung, die nur auf einzelnen Geräten auftrat, überspringt.
            public FailureProcessingResult PreprocessFailures(
              FailuresAccessor a)
            {
                // inside event handler, get all warnings
                IList<FailureMessageAccessor> failures
                  = a.GetFailureMessages();

                foreach (FailureMessageAccessor f in failures)
                {
                    // check failure definition ids 
                    // against ones to dismiss:
                    FailureDefinitionId id
                      = f.GetFailureDefinitionId();

                    if (BuiltInFailures.InaccurateFailures.InaccurateSketchLine
                      == id)
                    {
                        a.DeleteWarning(f);
                    }
                }
                return FailureProcessingResult.Continue;
            }
        }

        private void Button_Click_SetXPlanFile(object sender, RoutedEventArgs e)
        {
            Reader.FileDialog winexp = new Reader.FileDialog();
            xplan_file.Text = winexp.ImportPath(Reader.FileDialog.Data.XPlanGML);
        }


        private void Button_Click_StartXPlanImport(object sender, RoutedEventArgs e)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            // NLog
            #region logging
            String tempPath = Path.GetTempPath();
            var configNLog = new LoggingConfiguration();

            var fileTarget = new FileTarget("target2")
            {
                FileName = Path.GetFullPath(Path.Combine(tempPath, @"XPlan2Revit_${shortdate}.log")),
                Layout = "${longdate} ${level} ${message}  ${exception}"
            };
            configNLog.AddTarget(fileTarget);
            configNLog.AddRuleForAllLevels(fileTarget);

            LogManager.Configuration = configNLog;
            Logger logger = LogManager.GetCurrentClassLogger();

            #endregion logging

            var materialBuilder = new Builder.RevitXPlanBuilder(doc, app);
            Dictionary<string,ElementId> colorDict = materialBuilder.CreateMaterial();

            // Transforms local coordinates to relative coordinates due to the fact that revit has a 20 miles limit for presentation of geometry. 
            var transfClass = new City2RVT.Calc.Transformation();
            Transform transf = transfClass.transform(doc);

            // No UTM reduction at the moment, factor R = 1
            double R = 1;

            //loads the selected GML file
            string xPlanGmlPath = xplan_file.Text;
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.IgnoreComments = true;
            XmlDocument xmlDoc = new XmlDocument();

            ReadXPlan xPlanReader = new ReadXPlan();

            using (XmlReader reader = XmlReader.Create(xPlanGmlPath, readerSettings))
            {
                xmlDoc.Load(reader);
                xmlDoc.Load(xPlanGmlPath);
            }

            XYZ origin = new XYZ(0, 0, 0);
            XYZ normal = new XYZ(0, 0, feetToMeter);
            Plane geomPlane = Plane.CreateByNormalAndOrigin(normal, origin);

            // Namespacemanager for used namespaces, e.g. in XPlanung GML or ALKIS XML files
            var XmlNsmgr = new Builder.Revit_Semantic(doc);
            XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);

            #region parameter

            Category category = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Topography);
            Category projCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_ProjectInformation);
            CategorySet categorySet = app.Create.NewCategorySet();
            CategorySet projCategorySet = app.Create.NewCategorySet();
            categorySet.Insert(category);
            projCategorySet.Insert(projCategory);

            #endregion parameter  

            // Creates Project Information for revit project like general data or postal address
            //City2RVT.GUI.XPlan2BIM.XPlan_Parameter parameter = new XPlan_Parameter();
            //DefinitionFile defFile = default(DefinitionFile);
            var projInformation = new City2RVT.Builder.Revit_Semantic(doc);
            projInformation.CreateProjectInformation(app, doc, projCategorySet);   

            // Selected Layer for beeing shown in revit view
            var selectedLayers = categoryListbox_selectedLayer.SelectedItems;

            // Selected Parameter for beeing shown in revit view
            System.Collections.IList selectedParams = GUI.Prop_NAS_settings.SelectedParams;

            // The big surfaces of BP_Bereich and BP_Plan getting brought to the bottom so they do not overlap other surfaces in Revit
            // Alternatively they could be represented as borders instead of areas 
            XmlNodeList allXPlanObjects = xmlDoc.SelectNodes("//gml:featureMember", nsmgr);
            List<string> xPlanObjectList = xPlanReader.getXPlanFeatureMembers(allXPlanObjects, nsmgr);

            RevitXPlanBuilder xPlanBuilder = new RevitXPlanBuilder(doc, app);

            ElementId refplaneId = Prop_Revit.TerrainId;
            ElementId pickedId = Prop_Revit.PickedId;
            double zOffset = 0.0;
            if (selectedLayers.Count != 0)
            {
                if (check_drape.IsChecked == true)
                {
                    this.Hide();
                    Selection choices = uidoc.Selection;
                    Reference hasPickOne = choices.PickObject(ObjectType.Element, "Please select the terrain where the surfaces got draped to. ");

                    pickedId = hasPickOne.ElementId;
                    this.Show();
                }

                foreach (string xPlanObject in xPlanObjectList)
                {
                    if (selectedLayers.Contains(xPlanObject))
                    {
                        //_______________________________________________________________________________________________
                        // creates referenceplanes (if picked DTM, else new creating new referenceplane) for the surfaces
                        //***********************************************************************************************
                        XmlNodeList xPlanExterior = xmlDoc.SelectNodes("//gml:featureMember//xplan:position", nsmgr);
                        if (xPlanExterior.Count > 0)
                        { 
                            if (check_drape.IsChecked == false)
                            {
                                refplaneId = xPlanBuilder.createRefPlane(xPlanExterior, xPlanObject, zOffset, geomPlane, colorDict, logger);                                
                            }
                            else
                            {
                                refplaneId = xPlanBuilder.copyDtm(xPlanObject, pickedId, zOffset, colorDict);
                            }
                        }

                        //_______________________________________________________________________________________________
                        // creates surfaces with parameters and values
                        //***********************************************************************************************
                        XmlNodeList bpSurface = xmlDoc.SelectNodes("//gml:featureMember/" + xPlanObject + "//gml:exterior", nsmgr);
                        xPlanBuilder.createSurface(xPlanObject, bpSurface, zOffset, xmlDoc, categorySet, geomPlane, logger, colorDict, refplaneId);
                       
                        #region lines

                        XmlNodeList bpLines = xmlDoc.SelectNodes("//gml:featureMember/" + xPlanObject + "/xplan:position/gml:LineString", nsmgr);

                        List<string> lineList = new List<String>();
                        int il = 0;
                        foreach (XmlNode nodeLine in bpLines)
                        {
                            lineList.Add(nodeLine.InnerText);
                            string[] koordWerteLine = lineList[il].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            int ia = 0;

                            using (Transaction createLine = new Transaction(doc, "Create Line for " + xPlanObject.Substring(xPlanObject.LastIndexOf(':') + 1)))
                            {
                                createLine.Start();
                                foreach (string split in koordWerteLine)
                                {
                                    var geomBuilder = new Builder.RevitXPlanBuilder(doc, app);

                                    double xStart = Convert.ToDouble(koordWerteLine[ia], System.Globalization.CultureInfo.InvariantCulture);
                                    double xStartMeter = xStart * feetToMeter;
                                    double xStartMeterRedu = xStartMeter / R;
                                    double yStart = Convert.ToDouble(koordWerteLine[ia + 1], System.Globalization.CultureInfo.InvariantCulture);
                                    double yStartMeter = yStart * feetToMeter;
                                    double yStartMeterRedu = yStartMeter / R;
                                    double zStart = 0;
                                    double zStartMeter = zStart * feetToMeter;

                                    double xEnd = Convert.ToDouble(koordWerteLine[ia + 2], System.Globalization.CultureInfo.InvariantCulture);
                                    double xEndMeter = xEnd * feetToMeter;
                                    double xEndMeterRedu = xEndMeter / R;
                                    double yEnd = Convert.ToDouble(koordWerteLine[ia + 3], System.Globalization.CultureInfo.InvariantCulture);
                                    double yEndMeter = yEnd * feetToMeter;
                                    double yEndMeterRedu = yEndMeter / R;
                                    double zEnd = 0;
                                    double zEndMeter = zEnd * feetToMeter;

                                    XYZ startPoint = new XYZ(xStartMeterRedu, yStartMeterRedu, zStartMeter);
                                    XYZ endPoint = new XYZ(xEndMeterRedu, yEndMeterRedu, zEndMeter);

                                    XYZ tStartPoint = transf.OfPoint(startPoint);
                                    XYZ tEndPoint = transf.OfPoint(endPoint);

                                    Line lineString = Line.CreateBound(tStartPoint, tEndPoint);
                                    {
                                        FailureHandlingOptions optionsExterior = createLine.GetFailureHandlingOptions();
                                        optionsExterior.SetFailuresPreprocessor(new AxesFailure());
                                        createLine.SetFailureHandlingOptions(optionsExterior);

                                        SketchPlane sketch = SketchPlane.Create(doc, geomPlane);

                                        ModelLine line = doc.Create.NewModelCurve(lineString, sketch) as ModelLine;

                                        GraphicsStyle gs = line.LineStyle as GraphicsStyle;

                                        gs.GraphicsStyleCategory.LineColor = new Color(250, 10, 10);
                                        gs.GraphicsStyleCategory.SetLineWeight(10, GraphicsStyleType.Projection);

                                    }

                                    if ((ia + 3) == (koordWerteLine.Count() - 1))
                                    {
                                        break;
                                    }
                                    ia += 2;
                                }
                                createLine.Commit();
                            }
                                
                            il++;
                        }

                        #endregion lines

                        if (checkBoxZOffset.IsChecked == true)
                        {
                            zOffset += 10.0;
                        }
                        else
                        {
                            zOffset += 0;
                        }
                    }
                }
            }
            else
            {
                TaskDialog.Show("No layer selected", "You have to select at least one layer to start the import. ");
            }
        }

        private void Xplan_file_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click_ClearSelection(object sender, RoutedEventArgs e)
        {
            categoryListbox_selectedLayer.UnselectAll();
            radioButton1.IsChecked = false;
        }

        private void Button_Click_ApplyXPlanFile(object sender, RoutedEventArgs e)
        {
            string xPlanGmlPath = xplan_file.Text;
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.IgnoreComments = true;
            XmlDocument xmlDoc = new XmlDocument();

            using (XmlReader reader = XmlReader.Create(xPlanGmlPath, readerSettings))
            {
                xmlDoc.Load(reader);
                xmlDoc.Load(xPlanGmlPath);
            }

            var XmlNsmgr = new Builder.Revit_Semantic(doc);
            XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);

            XmlNodeList allXPlanObjects = xmlDoc.SelectNodes("//gml:featureMember", nsmgr);

            ReadXPlan xPlanReader = new ReadXPlan();
            List<string> xPlanObjectList = xPlanReader.getXPlanFeatureMembers(allXPlanObjects, nsmgr);
            xPlanObjectList.Sort();

            int ix = 0;
            foreach (string item in xPlanObjectList)
            {
                categoryListbox_selectedLayer.Items.Add(xPlanObjectList[ix]);
                ix++;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (categoryListbox_selectedLayer.SelectedItems.Count < categoryListbox_selectedLayer.Items.Count)
            {
                radioButton1.IsChecked = false;
            }
        }

        private void Button_Click_CloseXplanForm(object sender, RoutedEventArgs e)
        {
            Prop_XPLAN_settings.FileUrl = xplan_file.Text;
            this.Close();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (radioButton1.IsChecked == true)
            {
                categoryListbox_selectedLayer.SelectAll();
            }
            else
            {
                categoryListbox_selectedLayer.UnselectAll();

            }
        }

        private void checkBoxZOffset_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_ModifyParams(object sender, RoutedEventArgs e)
        {
            //UIApplication uiapp = commandData.Application;
            //UIDocument uidoc = uiapp.ActiveUIDocument;
            //Document doc = uidoc.Document;

            string xPlanGmlPath = xplan_file.Text;
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.IgnoreComments = true;
            XmlDocument xmlDoc = new XmlDocument();

            using (XmlReader reader = XmlReader.Create(xPlanGmlPath, readerSettings))
            {
                xmlDoc.Load(reader);
                xmlDoc.Load(xPlanGmlPath);
            }

            var XmlNsmgr = new Builder.Revit_Semantic(doc);
            XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);

            var selectedLayers = categoryListbox_selectedLayer.SelectedItems;

            ReadXPlan xPlanReader = new ReadXPlan();

            List<string> allParamList = new List<string>();

            int il = 0;
            foreach (var layer in selectedLayers)
            {
                List<string> thisParams = xPlanReader.getXPlanParameter(layer.ToString(), xmlDoc, nsmgr);

                foreach (var p in thisParams)
                {
                    //allParamList.Add(p + " (" + layer.ToString().Substring(layer.ToString().LastIndexOf(':')+1) + ")");
                    allParamList.Add(p + " (" + layer.ToString() + ")");
                }

                il++;
            }
            
            allParamList.Sort();
            GUI.Prop_NAS_settings.ParamList = allParamList;

            Modify.ModifyParameterForm f1 = new Modify.ModifyParameterForm();
            f1.ShowDialog();
        }

        private void drape_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
