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
using Autodesk.Revit.DB.ExtensibleStorage;

using NLog;
using NLog.Targets;
using NLog.Config;
using City2RVT.Reader;
using City2RVT.Builder;
using City2RVT.Calc;

namespace City2RVT.GUI.XPlan2BIM
{
    /// <summary>
    /// Interaktionslogik für Wpf_XPlan.xaml
    /// </summary>
    public partial class Wpf_XPlan : Window
    {
        ExternalCommandData commandData;
        private readonly Document doc;
        GUI_helper guiLogic = new GUI_helper();


        public Wpf_XPlan(Document doc,ExternalCommandData cData)
        {
            commandData = cData;
            this.doc = doc;

            InitializeComponent();

            Uri iconUri = new Uri("pack://application:,,,/City2RVT;component/img/XPlan_32px.ico", UriKind.RelativeOrAbsolute);
            this.Icon = BitmapFrame.Create(iconUri);

            xplan_file.Text = Prop_XPLAN_settings.GmlUrl;
        }

        public class AxesFailure : IFailuresPreprocessor
        {
            //Eventhandler, der eine ignorierbare Warnung, die nur auf einzelnen Geräten auftrat, überspringt.
            public FailureProcessingResult PreprocessFailures(FailuresAccessor a)
            {
                // inside event handler, get all warnings
                IList<FailureMessageAccessor> failures = a.GetFailureMessages();

                foreach (FailureMessageAccessor f in failures)
                {
                    // check failure definition ids 
                    // against ones to dismiss:
                    FailureDefinitionId id = f.GetFailureDefinitionId();

                    if (BuiltInFailures.InaccurateFailures.InaccurateSketchLine == id)
                    {
                        a.DeleteWarning(f);
                    }
                    if (id == BuiltInFailures.InaccurateFailures.InaccurateLine)
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

        public void StoreFilepathInStorage(string gmlPath, Document doc)
        {
            using (Transaction tStoreFilepath = new Transaction(doc, "tCreateAndStore"))
            {
                tStoreFilepath.Start();

                // create a new Data storage.
                DataStorage createdInfoStorage = DataStorage.Create(doc);
                createdInfoStorage.Name = "DS_XPlanung";

                // new Schema builder for editing schema
                SchemaBuilder schemaBuilder = new SchemaBuilder(Guid.NewGuid());
                schemaBuilder.SetSchemaName("Filepaths");
                // allow anyone to read the object
                schemaBuilder.SetReadAccessLevel(AccessLevel.Public);                

                // add new field "gmlPath"
                FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField("gmlPath", typeof(string));
                fieldBuilder.SetDocumentation("Path to gml file of XPlanung.");

                Schema schema = schemaBuilder.Finish(); // register the Schema object

                // create an entity (object) for this schema (class)
                Entity entity = new Entity(schema);

                // get the field from the schema
                Field fieldSpliceLocation = schema.GetField("gmlPath");

                entity.Set<string>(fieldSpliceLocation, gmlPath); // set the value for this entity

                createdInfoStorage.SetEntity(entity); // store the entity in the element (data storage)

                tStoreFilepath.Commit();
            }            
        }

        private void Button_Click_StartXPlanImport(object sender, RoutedEventArgs e)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            // NLog
            #region logging
            string tempPath = Path.GetTempPath();
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

            var materialBuilder = new RevitXPlanBuilder(doc, app);
            Dictionary<string,ElementId> colorDict = materialBuilder.CreateMaterial();

            //loads the selected GML file
            string xPlanGmlPath = xplan_file.Text;
            StoreFilepathInStorage(xPlanGmlPath, doc);
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
            XYZ normal = new XYZ(0, 0, 1);

            Transformation transformation = new Transformation();
            Plane geomPlane = transformation.getGeomPlane(normal, origin);

            // Namespacemanager for used namespaces, e.g. in XPlanung GML or ALKIS XML files
            var XmlNsmgr = new Revit_Semantic(doc);
            XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);

            #region parameter

            Category category = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Topography);
            Category projCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_ProjectInformation);
            CategorySet categorySet = app.Create.NewCategorySet();
            CategorySet projCategorySet = app.Create.NewCategorySet();
            _ = categorySet.Insert(category);
            _ = projCategorySet.Insert(projCategory);

            //GUI.Properties.Wf_showProperties wf_ShowProperties = new Properties.Wf_showProperties(commandData);
            //wf_ShowProperties.updateGml(doc, uidoc);

            #endregion parameter  

            // Creates Project Information for revit project like general data or postal address
            var projInformation = new Revit_Semantic(doc);
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
                bool drape_checked = false;
                if (check_drape.IsChecked == true)
                {
                    pickedId = GetTerrain(uidoc);                    
                    drape_checked = true;
                }

                foreach (string xPlanObject in xPlanObjectList)
                {
                    if (selectedLayers.Contains(xPlanObject))
                    {
                        //_______________________________________________________________________________________________
                        // creates referenceplanes (if picked DTM, else creating new referenceplane) for the surfaces
                        //***********************************************************************************************
                        XmlNodeList xPlanExterior = xmlDoc.SelectNodes("//gml:featureMember//xplan:position//gml:exterior", nsmgr);
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
                        XmlNodeList xpSurface = xmlDoc.SelectNodes("//gml:featureMember/" + xPlanObject + "//gml:exterior", nsmgr);                        
                        xPlanBuilder.createSurface(xPlanObject, xpSurface, zOffset, xmlDoc, categorySet, geomPlane, logger, colorDict, refplaneId);

                        //_______________________________________________________________________________________________
                        // creates line segments
                        //***********************************************************************************************
                        //bool drape_checked = (bool)check_drape.IsChecked;
                        xPlanBuilder.createLineSegments(xPlanObject, xmlDoc, nsmgr, zOffset, pickedId, drape_checked);                        


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
            rb_selectAll.IsChecked = false;
        }

        private void Button_Click_ApplyXPlanFile(object sender, RoutedEventArgs e)
        {
            string xPlanGmlPath = xplan_file.Text;
            Prop_NAS_settings.FileUrl = xplan_file.Text;
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.IgnoreComments = true;
            XmlDocument xmlDoc = new XmlDocument();

            using (XmlReader reader = XmlReader.Create(xPlanGmlPath, readerSettings))
            {
                xmlDoc.Load(reader);
                xmlDoc.Load(xPlanGmlPath);
            }

            var XmlNsmgr = new Revit_Semantic(doc);
            XmlNamespaceManager nsmgr = XmlNsmgr.GetNamespaces(xmlDoc);

            XmlNodeList allXPlanObjects = xmlDoc.SelectNodes("//gml:featureMember", nsmgr);

            ReadXPlan xPlanReader = new ReadXPlan();
            List<string> xPlanObjectList = xPlanReader.getXPlanFeatureMembers(allXPlanObjects, nsmgr);
            xPlanObjectList.Sort();

            int ix = 0;
            foreach (string item in xPlanObjectList)
            {
                _ = categoryListbox_selectedLayer.Items.Add(xPlanObjectList[ix]);
                ix++;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            guiLogic.uncheckRbWhenSelected(rb_selectAll, categoryListbox_selectedLayer);
        }

        private void Button_Click_CloseXplanForm(object sender, RoutedEventArgs e)
        {
            Prop_XPLAN_settings.GmlUrl = xplan_file.Text;
            this.Close();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            guiLogic.setRadiobutton(rb_selectAll, categoryListbox_selectedLayer);
        }

        private void checkBoxZOffset_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_ModifyParams(object sender, RoutedEventArgs e)
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

            var selectedLayers = categoryListbox_selectedLayer.SelectedItems;

            ReadXPlan xPlanReader = new ReadXPlan();

            List<string> allParamList = new List<string>();

            int il = 0;
            foreach (var layer in selectedLayers)
            {
                List<string> thisParams = xPlanReader.getXPlanParameter(layer.ToString(), xmlDoc, nsmgr);

                foreach (var p in thisParams)
                {
                    allParamList.Add(p + " (" + layer.ToString() + ")");
                }

                il++;
            }
            
            allParamList.Sort();
            GUI.Prop_NAS_settings.ParamList = allParamList;

            Modify.ModifyParameterForm f1 = new Modify.ModifyParameterForm();
            _ = f1.ShowDialog();
        }

        private void drape_Checked(object sender, RoutedEventArgs e)
        {

        }

        public ElementId GetTerrain(UIDocument uidoc)
        {
            FilteredElementCollector topoCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Topography);
            ElementId pickedId = Prop_Revit.PickedId;

            var topoList = new List<ElementId>();
            foreach (var id in topoCollector)
            {
                topoList.Add(id.Id);
            }

            // if there is only one terrain in the revit project, this one will be used as Hostelement. Otherwise user has to pick one. 
            if (topoList.Count() == 1)
            {
                pickedId = topoList.FirstOrDefault();
            }
            else if (topoList.Count() == 0)
            {
                TaskDialog.Show("No terrain", "There is no terrain. Please import a terrain first via function 'DTM2BIM - Get terrain data '. " +
                    "Alternative switch off the 'drape-checkbox' when importing your data. ");
            }
            else if (topoList.Count() > 0)
            {
                this.Hide();
                Selection choices = uidoc.Selection;
                Reference hasPickOne = choices.PickObject(ObjectType.Element, "Please select the terrain where the surfaces got draped to. ");

                pickedId = hasPickOne.ElementId;
                this.Show();
            }
            return pickedId;
        }
    }
}
