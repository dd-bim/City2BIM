using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime;
using System.Configuration;
using System.Configuration.Assemblies;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using netDxf;
using netDxf.Entities;
using Excel = Microsoft.Office.Interop.Excel;
using lageplanImport.Lines;
using NLog;
using NLog.Targets;
using NLog.Config;

namespace lageplanImport
{
    [TransactionAttribute(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public partial class mainForm : System.Windows.Forms.Form
    {
        ExternalCommandData commandData;
        double feetToMeter = 1.0 / 0.3048;

        public mainForm(ExternalCommandData cData)
        {
            commandData = cData;
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            InitializeComponent();
        }
        
        // this function is needed for getting the key value from the config file
        // this is only needed for libraries (DLLs)
        // further information: https://stackoverflow.com/questions/5190539/equivalent-to-app-config-for-a-library-dll
        string GetAppSetting(Configuration config, string key)
        {
            KeyValueConfigurationElement element = config.AppSettings.Settings[key];
            if (element != null)
            {
                string value = element.Value;
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return string.Empty;
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }
        OpenFileDialog ofdDxf = new OpenFileDialog();

        public void button1_Click(object sender, EventArgs e)
        {
            ofdDxf.Filter = "DXF|*.dxf|All files| *.*";
            if (ofdDxf.ShowDialog() == DialogResult.OK && ofdDxf.FileName.Length > 0)
            {
                textBox1.Text = ofdDxf.FileName;
            }
        }

        public string dxfFileText
        {
            get
            {
                return textBox1.Text;
            }
        }

        public void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            // Log File location
            String tempPath = Path.GetTempPath();
            string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string subFolder = assemblyPath.Substring(0, assemblyPath.LastIndexOf("SurveyorsplanToRevit")+ 20);
            string logLocation = Path.GetFullPath(Path.Combine(tempPath, @"SurveyorsplanToRevit_LOG.txt"));

            // NLog
            var configNLog = new LoggingConfiguration();

            var fileTarget = new FileTarget("target2")
            {
                FileName = Path.GetFullPath(Path.Combine(tempPath, @"SurveyorsplanToRevit_${shortdate}.log")),
                Layout = "${longdate} ${level} ${message}  ${exception}"
            };
            configNLog.AddTarget(fileTarget);
            configNLog.AddRuleForAllLevels(fileTarget); // all to console

            LogManager.Configuration = configNLog;
            Logger log = LogManager.GetLogger("Example");

            //    Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.File(logLocation, rollingInterval: RollingInterval.Day).CreateLogger();
            //    Log.Information("Log file created at " + logLocation + ". ");

            helpclasses hc = new helpclasses();

            // load config file
            // config file is currently not used
            string exeConfigPath = this.GetType().Assembly.Location;
            //string exeConfigPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "App.config");
            Configuration config = ConfigurationManager.OpenExeConfiguration(exeConfigPath);
            // load dxf file
            DxfDocument dxfLoad = DxfDocument.Load(textBox1.Text);
            log.Info("Dxf file loaded");

            double utmOffset;
            if (string.IsNullOrWhiteSpace(textBox4.Text))
            {
                utmOffset = 0;
            }
            else
            {
                utmOffset = Convert.ToDouble(textBox4.Text);
            }

            // gets the procject location 
            ProjectLocation projloc = doc.ActiveProjectLocation;
            ProjectPosition positionData = projloc.GetProjectPosition(XYZ.Zero);
            double angle = positionData.Angle;
            double elevation = positionData.Elevation;
            //eastring value is reduced by zone number if the user typed in an utm offset
            double easting = positionData.EastWest;
            easting = hc.splitUtm(easting/feetToMeter, utmOffset);
            easting *= feetToMeter;
            double northing = positionData.NorthSouth;
            // calls the methode to calculate the transformation
            Transform transf = hc.transform(positionData,angle,easting,northing,elevation);
            double R = hc.reduction(positionData, angle, easting, northing, elevation);

            // An Excel-File sets the assignment of dxf-Layernames to the family-names, that comes from surveying regulation "bfr-Verm".
            // Chosing the excel filer. Path ist given by either config file or by textBox3.Text, selected by the user
            string excelFile = null;
            if (textBox2.Text == "")
            {
                //excelFile = Path.GetFullPath(Path.Combine(subFolder, @"Zuordnungstabelle\Zuordnungstabelle.xlsx"));
                excelFile = @"D:\Daten\ZAFT\SurveyorsplanToRevit\Ausgangsdaten\Zuordnungstabelle\Zuordnungstabelle.xlsx";
                log.Info("No excel file chosen. The default path " + excelFile + " is used. ");
            }
            else
            {
                excelFile = textBox3.Text;
                log.Info("Excel file: " + excelFile + ". ");
            }

            Excel.Application excelApp = null;
            excelApp = new Excel.Application();
            excelApp.Visible = false;
            Excel.Workbook wkb = null;
            wkb = excelApp.Workbooks.Open(excelFile);
            // opens the worksheet for either Attribute or layer definition and checks only column A for dxf-layer name
            Excel.Worksheet workSheetAttribute = wkb.Worksheets["Attribute"];
            Excel.Worksheet workSheetLayer = wkb.Worksheets["dxf layer"];
            Excel.Worksheet workSheetName = wkb.Worksheets["Blockname"];
            Excel.Range columnRangeN = workSheetName.Columns["C:C"];
            log.Info("Excel file loaded");

            DataTable dtLayer = hc.createDataTable(workSheetLayer);
            DataTable dtAttribute = hc.createDataTable(workSheetAttribute);
            DataTable dtName = hc.createDataTable(workSheetName);

            int lastRowDtLayerBefore = dtLayer.Rows.Count;

            string famName = null;
            // Chosing the family folder. Path ist given by either config file or by textBox2.Text, selected by the user
            string famFolder = null;
            if (textBox2.Text =="")
            {
                famFolder = Path.GetFullPath(Path.Combine(subFolder, @"Bauteilbibliothek"));
                //famFolder = GetAppSetting(config, "famFolderPath");
                log.Info("No family folder path chosen. The default path " + famFolder + " is used. ");
            }
            else
            {
                famFolder = textBox2.Text;
                log.Info("Family folder path: " + famFolder + ". ");
            }

            #region inserts

            #region loadFamilies
            // for each entity of the dxf-File the families get loaded into the project. 
            string rfaNameInserts = null;
            Family fam = null;
            string famExtension = "rfa";
            string FamilyPath = null;
            List<Insert> inserts = new List<Insert>();
            List<string> revitFam = new List<string>();

            string rfaNameInsertDT = null;
            string rfaNameAttriDT = null;

            foreach (Insert insertEntity in dxfLoad.Inserts)
            {
                string dxfLayerName = insertEntity.Layer.Name;

                rfaNameInsertDT = hc.getRfaName(dtLayer, dxfLayerName);

                if (dxfLayerName != "0")
                {
                    try
                    {
                        if (rfaNameInsertDT == "Blockname")
                        {
                            rfaNameInsertDT = hc.getRfaName(dtName, insertEntity.Block.Name);

                            if (rfaNameInsertDT != null)
                            {
                                famName = rfaNameInsertDT;
                                revitFam.Add(famName);
                                inserts.Add(insertEntity);
                            }                            
                        }

                        else
                        {
                            if (rfaNameInsertDT == null || rfaNameInsertDT == "-")
                            {
                                string concat = string.Concat("leer_", dxfLayerName);
                                revitFam.Add(concat);
                            }
                            else
                            {
                                famName = rfaNameInsertDT;
                                revitFam.Add(famName);
                                inserts.Add(insertEntity);
                            }
                        }
                    }
                    catch
                    {
                        string rangeFind = hc.getRfaName(dtLayer, dxfLayerName);
                        int lastRow = dtLayer.Rows.Count;
                        if (rangeFind == null)
                        {
                            dtLayer.Rows.Add("-", "", dxfLayerName, "Please fill out column 'rfa name' with an existing revit family name you want to use for '" + dxfLayerName + "'. ");
                        }
                        //wkb.Save();
                        //TaskDialog.Show("Error", "No entry found in excel sheet for the insert '" + dxfLayerName + "'. ");
                        //log.Warn("No entry found in excel sheet for the insert '" + dxfLayerName + "'. ");
                    }
                }                    
            }
            
            List<string> revitFamDistinct = new List<string>();

            revitFamDistinct = revitFam
                .GroupBy(n => n.ToString())
                .Select(grp => grp.First()).ToList();

            //revitFamDistinct.Remove("Blockname");

            // using the distinct list containing distinct inserts. This speeds up the process since every family name has only be checked once 
            // and it cleans up logging file and error task dialogs
            foreach (string insertEntity in revitFamDistinct)
            {
                // the families only get loaded into the project if the layername is found in the excel-file. otherwise an error-message appears
                try
                {
                    famName = insertEntity;
                    FamilyPath = Path.Combine(famFolder, famName);
                    FamilyPath = Path.ChangeExtension(FamilyPath, famExtension);

                    #endregion Familypath

                    fam = Funktionen.FindElementByName(doc, typeof(Family), famName) as Family;
                    if (null == fam && famName != "-" && famName != "Blockname")
                    {
                        if (!File.Exists(FamilyPath))
                        {
                            if (insertEntity.Substring(0,5)=="leer_")
                            {
                                //TaskDialog.Show("Error", "No family associated to '" + insertEntity.Substring(5) + "'." +
                                //    " Make sure that there is an entry in the excel sheet for an rfa name for '" + insertEntity.Substring(5) + "'. " );
                                TaskDialog.Show("Error", "Entry for layername and/or rfa name missing in excel sheet for '" + insertEntity.Substring(5) + "'." +
                                    " Make sure that there is an entry in the excel sheet and associate a rfa name for '" + insertEntity.Substring(5) + "'. ");
                                log.Warn("Entry for layername and/or rfa name missing in excel sheet for '" + insertEntity.Substring(5) + "'. ");
                            }
                            else
                            {
                                TaskDialog.Show("Error", string.Format("Make sure that insert family '{0}' exists in '{1}'.", famName, famFolder));
                                log.Warn("Could not find '" + famName + "' in '" + famFolder + "'");
                            }
                        }
                        // Load family from file:
                        using (Transaction tx = new Transaction(doc))
                        {
                            tx.Start("Load Family");
                            doc.LoadFamily(FamilyPath, out fam);
                            log.Info("Loaded insert family '" + famName + "' into project");
                            tx.Commit();
                        }
                    }
                }
                catch
                {

                }
            }

            // In the following families get created and attributes added
            int ip = 0;
            String layerName = null;
            foreach (Insert insertEntity in inserts)
            {
                layerName = insertEntity.Layer.Name;
                // the family-name for each dxf-layername is taken from the excel-file
                rfaNameInsertDT = hc.getRfaName(dtLayer, layerName);

                if (rfaNameInsertDT == "Blockname")
                {
                    try
                    {
                        rfaNameInsertDT = hc.getRfaName(dtName, insertEntity.Block.Name);
                    }
                    catch
                    {
                        // Standardausgabe Nadelbaum: nur um was zu sehen!
                        rfaNameInsertDT = "UP_Baum(Nadelbaum)";
                    }
                }
                else
                {
                    
                }

                //MessageBox.Show(rfaNameInsertDT);
                if (rfaNameInsertDT != null || rfaNameInsertDT != "-" )
                {
                    using (Transaction transInserts = new Transaction(doc))
                    {
                        transInserts.Start("Create insert-families");

                        FamilySymbol famSymbol = new FilteredElementCollector(doc).OfClass(typeof(Family)).OfType<Family>().FirstOrDefault
                            (f => f.Name.Equals(rfaNameInsertDT))?.GetFamilySymbolIds().Select(id => doc.GetElement(id))
                            .OfType<FamilySymbol>().FirstOrDefault(symbol => symbol.Name.Equals(rfaNameInsertDT));

                        FamilyInstance famInstance = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, famSymbol);

                        IList<ElementId> placePointIds = new List<ElementId>();
                        placePointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(famInstance);

                        // the placement-points for the adaptive families are the coordinates of the inserts from the dxf-file. 
                        // That means, the adaptive families are placed at the position of the inserts. 
                        foreach (ElementId id in placePointIds)
                        {
                            ReferencePoint placementPoint = doc.GetElement(id) as ReferencePoint;
                            placementPoint.Position = (transf.OfPoint(new XYZ(hc.splitUtm(inserts[ip].Position.X,utmOffset) * feetToMeter, 
                                inserts[ip].Position.Y * feetToMeter, inserts[ip].Position.Z * feetToMeter)))/R;
                        }
                        log.Info("Family '" + rfaNameInsertDT + "' created.");

                        #region attributes
                        try
                        {
                            // the variable "attribute" contains the attributes for each insert from the dxf-file
                            var attribute = insertEntity.Attributes;
                            foreach (var s in attribute)
                            {
                                rfaNameAttriDT = hc.getRfaName(dtAttribute, s.Tag);

                                if (famInstance.LookupParameter(rfaNameAttriDT) != null)
                                {
                                    foreach (Parameter p in famInstance.Parameters)
                                    {
                                        if (p.Definition.Name == rfaNameAttriDT)
                                        {
                                            p.Set(s.Value.ToString());
                                        }
                                    }
                                }
                                else
                                {
                                    //TaskDialog.Show("Error", "Attribute '" + rfaNameAttri + "' for '" + rfaNameInserts + "' not found.");
                                    log.Warn("Attribute '" + rfaNameAttriDT + "' for '" + rfaNameInsertDT + "' not found.");
                                }
                            }
                        }
                        catch
                        {

                        }
                        #endregion attributes
                        transInserts.Commit();
                    }
                }
                else
                {
                    TaskDialog.Show("Error", "No family available for '" + layerName + "'. ");
                    log.Error("No family available for '" + layerName + "'. ");
                }
                ip++;
                rfaNameInserts = String.Empty;
            }

            #endregion inserts

            #region lines
            #region load families
            List<netDxf.Entities.Line> lines = new List<netDxf.Entities.Line>();
            List<netDxf.Entities.Line> distinctLines = new List<netDxf.Entities.Line>();
            lines = dxfLoad.Lines.ToList();
            distinctLines = lines.GroupBy(g => g.Layer.Name).Select(grp => grp.First()).ToList();
            string rfaNameLines=null;

            // loading families for distinct lines to speed up the process and keep log file and error messages clean
            foreach (netDxf.Entities.Line lineEntity in distinctLines)
            {
                string layerNameLines = lineEntity.Layer.Name;
                try
                {
                    rfaNameLines = hc.getRfaName(dtLayer, layerNameLines);

                    #region familypath
                    famName = rfaNameLines;
                    FamilyPath = Path.Combine(famFolder, famName);
                    FamilyPath = Path.ChangeExtension(FamilyPath, famExtension);
                    #endregion familypath

                    fam = Funktionen.FindElementByName(doc, typeof(Family), famName) as Family;
                    if (null == fam && famName != "-" && famName != "Blockname")
                    {
                        if (!File.Exists(FamilyPath))
                        {
                            TaskDialog.Show("Error", string.Format("Make sure that line family '{0}' exists in '{1}'.", famName, famFolder));
                        }
                        // Load family from file:
                        using (Transaction tx = new Transaction(doc))
                        {
                            tx.Start("Load Family");
                            doc.LoadFamily(FamilyPath, out fam);
                            log.Info("Loaded line family '" + famName + "' into project");
                            tx.Commit();
                        }
                    }
                }
                catch
                {
                        TaskDialog.Show("Error", "No entry found in excel sheet for line '" + layerNameLines + "'. Proxy model line is created. ");
                        log.Warn("No entry found in excel sheet for line '" + layerNameLines + "'. Proxy model line is created. ");

                    string rangeFind = hc.getRfaName(dtLayer, layerNameLines);
                    int lastRow = dtLayer.Rows.Count;
                    if (rangeFind == null)
                    {
                        dtLayer.Rows.Add("-", "", layerNameLines, "Please fill out column 'rfa name' with an existing revit family name you want to use for '" + layerNameLines + "'. ");
                    }
                    //wkb.Save();
                }
            }

            #endregion load families     

            foreach (netDxf.Entities.Line lineEntity in lines)
            {
                String layerTerm = null;
                XYZ startPunkt = (transf.OfPoint(new XYZ(hc.splitUtm(lineEntity.StartPoint.X, utmOffset) * feetToMeter, lineEntity.StartPoint.Y * feetToMeter, lineEntity.StartPoint.Z * feetToMeter)))/R;
                XYZ endPunkt = (transf.OfPoint(new XYZ(hc.splitUtm(lineEntity.EndPoint.X, utmOffset) * feetToMeter, lineEntity.EndPoint.Y * feetToMeter, lineEntity.EndPoint.Z * feetToMeter)))/R;
                XYZ norm = startPunkt.CrossProduct(endPunkt);

                // line-families get created if layername is found in the excel-sheet. Otherwise the lines will be represented as modellines. 
                try
                {
                    layerTerm = lineEntity.Layer.Name;

                    rfaNameLines = hc.getRfaName(dtLayer, layerTerm);

                    using (Transaction transLines = new Transaction(doc))
                    {
                        transLines.Start("Create line-families");

                        FamilySymbol famSymbol = new FilteredElementCollector(doc).OfClass(typeof(Family)).OfType<Family>().FirstOrDefault
                            (f => f.Name.Equals(rfaNameLines))?.GetFamilySymbolIds().Select(id => doc.GetElement(id))
                            .OfType<FamilySymbol>().FirstOrDefault(symbol => symbol.Name.Equals(rfaNameLines));

                        FamilyInstance famInstance = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, famSymbol);

                        IList<ElementId> placePointIds = new List<ElementId>();
                        placePointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(famInstance);

                        var pID1 = placePointIds[0];
                        ReferencePoint point1 = doc.GetElement(pID1) as ReferencePoint;
                        point1.Position = startPunkt;
                        var pID2 = placePointIds[1];
                        ReferencePoint point2 = doc.GetElement(pID2) as ReferencePoint;
                        point2.Position = endPunkt;
                        log.Info("Line family '" + rfaNameLines + "' created.");

                        foreach (Parameter p in famInstance.Parameters)
                        {
                            if (p.Definition.Name == "Layer")
                            {
                                p.Set(rfaNameLines);
                            }
                        }
                        transLines.Commit();
                    }
                }
                catch
                {
                    Autodesk.Revit.DB.Line tl = Autodesk.Revit.DB.Line.CreateBound(startPunkt, endPunkt);
                    Autodesk.Revit.DB.Plane geomPlane = Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(norm, endPunkt);
                    Transaction drawLines = new Transaction(doc, "draw lines");
                    {
                        drawLines.Start();
                        SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                        ModelLine ml = doc.Create.NewModelCurve(tl, sketch) as ModelLine;
                    }
                    drawLines.Commit();
                }
            }
            #endregion lines

            #region 3dPolylines
            #region Familie laden
            List<Polyline> polylines = new List<Polyline>();
            List<Polyline> allPolylines = new List<Polyline>();

            allPolylines = dxfLoad.Polylines.ToList();
            var distinctPolylines = allPolylines.GroupBy(g => g.Layer.Name).Select(grp => grp.First()).ToList();
            string rfaNamePolylines = null;
            foreach (Polyline polylineEntity in distinctPolylines)
            {
                string layerNamePolylines = polylineEntity.Layer.Name;
                try
                {
                    rfaNamePolylines = hc.getRfaName(dtLayer, layerNamePolylines);
                    //MessageBox.Show(rfaNamePolylines);

                    polylines.Add(polylineEntity);

                    #region Familiepfad
                    famName = rfaNamePolylines;
                    FamilyPath = Path.Combine(famFolder, famName);
                    FamilyPath = Path.ChangeExtension(FamilyPath, famExtension);
                    #endregion Familiepfad

                    fam = Funktionen.FindElementByName(doc, typeof(Family), famName) as Family;
                    if (null == fam && famName != "-" && famName != "Blockname")
                    {
                        if (!File.Exists(FamilyPath))
                        {
                            TaskDialog.Show("Error", string.Format("Make sure that polyline family '{0}' exists in '{1}'.", famName, famFolder));
                        }
                        // Load family from file:
                        using (Transaction tx = new Transaction(doc))
                        {
                            tx.Start("Load Family");
                            doc.LoadFamily(FamilyPath, out fam);
                            tx.Commit();
                        }
                    }
                }
                catch
                {
                    TaskDialog.Show("Error", "No entry found in excel sheet for polyline '" + layerNamePolylines + "'. Proxy model line is created. ");
                    log.Warn("No entry found in excel sheet for polyline '" + layerNamePolylines + "'. Proxy model line is created. ");
                }
            }
            #endregion Familie laden   

            int iPl = 0;
            List<XYZ> verticesXYZ = new List<XYZ>();
            foreach (Polyline pz in polylines)
            {
                int iv = 0;
                var vertices = polylines[iPl].Vertexes;
                foreach (PolylineVertex pv in vertices)
                {
                    verticesXYZ.Add(transf.OfPoint(new XYZ(hc.splitUtm(vertices[iv].Position.X, utmOffset) * feetToMeter, vertices[iv].Position.Y * feetToMeter, vertices[iv].Position.Z * feetToMeter)));
                    iv++;
                }

                var distinctVertices = verticesXYZ.GroupBy(m => new { m.X, m.Y }).Select(x => x.First()).ToList();
                int numberVertices = distinctVertices.Count;

                String layerBezeichnung = null;

                if (rfaNamePolylines != null )
                {
                    layerBezeichnung = pz.Layer.Name;
                    rfaNamePolylines = hc.getRfaName(dtLayer, layerBezeichnung);

                    for (int g = 1; g < numberVertices; g++)
                    {
                        XYZ a = distinctVertices[g-1];
                        XYZ b = distinctVertices[g];
                        using (Transaction tPolylines = new Transaction(doc))
                        {
                            tPolylines.Start("Transaction Polylines");

                            FamilySymbol famSymbol = new FilteredElementCollector(doc).OfClass(typeof(Family)).OfType<Family>().FirstOrDefault
                            (f => f.Name.Equals(rfaNamePolylines))?.GetFamilySymbolIds().Select(id => doc.GetElement(id))
                            .OfType<FamilySymbol>().FirstOrDefault(symbol => symbol.Name.Equals(rfaNamePolylines));

                            if (famSymbol!=null)
                            {
                                FamilyInstance famInstance = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, famSymbol);

                                IList<ElementId> placePointIds = new List<ElementId>();
                                placePointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(famInstance);

                                var pID1 = placePointIds[0];
                                ReferencePoint point1 = doc.GetElement(pID1) as ReferencePoint;
                                point1.Position = a;
                                var pID2 = placePointIds[1];
                                ReferencePoint point2 = doc.GetElement(pID2) as ReferencePoint;
                                point2.Position = b;
                                log.Info("Polyline family '" + rfaNamePolylines + "' created.");

                                // placeholder for line Attributes
                                foreach (Parameter p in famInstance.Parameters)
                                {
                                    if (p.Definition.Name == "Layer")
                                    {
                                        p.Set(rfaNamePolylines);
                                    }
                                }
                                // placeholder for line Attributes

                            }


                            tPolylines.Commit();
                        }
                    }
                    verticesXYZ.Clear();
                }
                else
                {
                    for (int g = 1; g < numberVertices; g++)
                    {
                        XYZ a = distinctVertices[g - 1];
                        XYZ b = distinctVertices[g];
                        double len = a.DistanceTo(b);
                        if (len > 0.005)
                        {
                            using (Transaction proxyPolylines = new Transaction(doc))
                            {
                                proxyPolylines.Start("Draw proxy polylines");
                                XYZ norm = a.CrossProduct(b);
                                if (norm.IsZeroLength())
                                    norm = XYZ.BasisZ;
                                Autodesk.Revit.DB.Plane plane = Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(norm, b);
                                SketchPlane skplane = SketchPlane.Create(doc, plane);
                                Autodesk.Revit.DB.Line linie = Autodesk.Revit.DB.Line.CreateBound(a, b);
                                ModelCurve mcurve = doc.Create.NewModelCurve(linie, skplane);
                                proxyPolylines.Commit();
                            }
                        }
                    }
                    //MessageBox.Show("Für den Linien-Layer " + layerBezeichnung + " ist keine Bauteilfamilie vorhanden");
                }
                iPl++;
            }
            #endregion 3dPolylines

            #region points
            #region load families
            List<netDxf.Entities.Point> points = new List<netDxf.Entities.Point>();
            List<netDxf.Entities.Point> distinctPoints = new List<netDxf.Entities.Point>();
            points = dxfLoad.Points.ToList();
            distinctPoints = points.GroupBy(p => p.Layer.Name).Select(grp => grp.First()).ToList();
            string rfaNamePoints = null;

            // loading families for distinct points to speed up the process and keep log file and error messages clean
            foreach (netDxf.Entities.Point pointEntity in distinctPoints)
            {
                string layerNamePoints = pointEntity.Layer.Name;
                try
                {
                    rfaNamePoints = hc.getRfaName(dtLayer, layerNamePoints);

                    #region familypath
                    famName = rfaNamePoints;
                    FamilyPath = Path.Combine(famFolder, famName);
                    FamilyPath = Path.ChangeExtension(FamilyPath, famExtension);
                    #endregion familypath

                    fam = Funktionen.FindElementByName(doc, typeof(Family), famName) as Family;
                    if (null == fam && famName != "-" && famName != "Blockname")
                    {
                        if (!File.Exists(FamilyPath))
                        {
                            TaskDialog.Show("Error", string.Format("Make sure that point family '{0}' exists in '{1}'.", famName, famFolder));
                        }
                        // Load family from file:
                        using (Transaction tx = new Transaction(doc))
                        {
                            tx.Start("Load Family");
                            doc.LoadFamily(FamilyPath, out fam);
                            log.Info("Loaded point family '" + famName + "' into project");
                            tx.Commit();
                        }
                    }
                }
                catch
                {
                    TaskDialog.Show("Error", "No entry found in excel sheet for line '" + layerNamePoints + "'. Proxy model line is created. ");
                    log.Warn("No entry found in excel sheet for line '" + layerNamePoints + "'. Proxy model line is created. ");

                    string rangeFind = hc.getRfaName(dtLayer, layerNamePoints);
                    int lastRow = dtLayer.Rows.Count;
                    if (rangeFind == null)
                    {
                        dtLayer.Rows.Add("-", "", layerNamePoints, "Please fill out column 'rfa name' with an existing revit family name you want to use for '" + layerNamePoints + "'. ");
                    }
                    //wkb.Save();
                }
            }
            #endregion load families

            int ipoi = 0;
            foreach (netDxf.Entities.Point pointEntity in points)
            {
                String layerTerm = null;

                // line-families get created if layername is found in the excel-sheet. Otherwise the lines will be represented as modellines. 
                try
                {
                    layerTerm = pointEntity.Layer.Name;

                    rfaNamePoints = hc.getRfaName(dtLayer, layerTerm);

                    using (Transaction transPoints = new Transaction(doc))
                    {
                        transPoints.Start("Create point-families");

                        FamilySymbol famSymbol = new FilteredElementCollector(doc).OfClass(typeof(Family)).OfType<Family>().FirstOrDefault
                            (f => f.Name.Equals(rfaNamePoints))?.GetFamilySymbolIds().Select(id => doc.GetElement(id))
                            .OfType<FamilySymbol>().FirstOrDefault(symbol => symbol.Name.Equals(rfaNamePoints));

                        FamilyInstance famInstance = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, famSymbol);

                        IList<ElementId> placePointIds = new List<ElementId>();
                        placePointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(famInstance);

                        foreach (ElementId id in placePointIds)
                        {
                            ReferencePoint placementPoint = doc.GetElement(id) as ReferencePoint;
                            placementPoint.Position = (transf.OfPoint(new XYZ(hc.splitUtm(points[ipoi].Position.X, utmOffset) * feetToMeter,
                                points[ipoi].Position.Y * feetToMeter, points[ipoi].Position.Z * feetToMeter))) / R;
                        }
                        log.Info("Family '" + rfaNamePoints + "' created.");

                        transPoints.Commit();
                    }
                }
                catch
                {

                }
                ipoi++;
            }

            #endregion points

            // hand over the new entries from data table to excel worksheets
            int lastRowDtLayerAfter = dtLayer.Rows.Count;

            workSheetLayer.Activate();
            for (int k = lastRowDtLayerBefore; k < lastRowDtLayerAfter; k++)
            {
                workSheetLayer.Cells[k + 1, 1].Value = dtLayer.Rows[k][0];
                workSheetLayer.Cells[k + 1, 3].Value = dtLayer.Rows[k][2];
                workSheetLayer.Cells[k + 1, 4].Value = dtLayer.Rows[k][3];
            }

            wkb.Close(true);
            excelApp.Quit();            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            String selectedFormat = String.Empty;
            DialogResult result = DialogResult.OK;
            this.DialogResult = (result != DialogResult.Cancel ? DialogResult.OK : DialogResult.None);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }
        FolderBrowserDialog fbd = new FolderBrowserDialog();

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void openFileDialog2_FileOk(object sender, CancelEventArgs e)
        {

        }
        OpenFileDialog ofdExcel = new OpenFileDialog();

        private void button6_Click(object sender, EventArgs e)
        {
            ofdExcel.Filter = "XSLX|*.xlsx|All files| *.*";
            if (ofdExcel.ShowDialog() == DialogResult.OK && ofdExcel.FileName.Length > 0)
            {
                textBox3.Text = ofdExcel.FileName;
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        public void textBox4_TextChanged(object sender, EventArgs e)
        {
            
        }
    }
}