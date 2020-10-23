using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using netDxf;
using netDxf.Entities;
using Excel = Microsoft.Office.Interop.Excel;
using NLog;
using NLog.Targets;
using NLog.Config;

namespace lageplanImport
{
    [TransactionAttribute(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public partial class AddAttributes : System.Windows.Forms.Form
    {
        ExternalCommandData commandData;

        public AddAttributes(ExternalCommandData cData)
        {
            commandData = cData;
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            InitializeComponent();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        public void button1_Click(object sender, EventArgs e)
        {
            // "Add"-Button for import of the family parameter
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            String tempPath = Path.GetTempPath();
            string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string logLocation = Path.GetFullPath(Path.Combine(tempPath, @"SurveyorsplanToRevit_LOG.txt"));

            // Backgroundworker / progress bar
            progressBar1.Value = 0;
            progressBar1.Maximum = 10;

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

            // The attributes from the listbox get written in a list.
            List<string> attrList = new List<string>();
            int ik = 0; 
            foreach (var ka in listBox2.Items)
            {
                attrList.Add(listBox2.Items[ik].ToString());
                ik++;
            }

            // The list with attributes is used for the following transaction. The attribute-entries of this list are added as new parameter to the family object. 
            FamilyManager familyMgr = doc.FamilyManager;
            using (Transaction tP = new Transaction(doc))
            {
                tP.Start("Parameter");

                int im = 0;
                foreach (var xAttr in attrList)
                {
                    try
                    {
                        FamilyParameter fp = doc.FamilyManager.AddParameter(attrList[im], BuiltInParameterGroup.PG_DATA, ParameterType.Text, true);
                        log.Info("Parameter '" + attrList[im] + "' added.");
                    }

                    catch
                    {
                        TaskDialog.Show("Revit", "The parameter '" + attrList[im] + "' already exists.");
                        log.Warn("The parameter '" + attrList[im] + "' already exists.");
                    }
                    im++;
                }

                tP.Commit();
                backgroundWorker1.RunWorkerAsync();
                this.Refresh();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //  button for closing the dialoge
            String selectedFormat = String.Empty;
            DialogResult result = DialogResult.OK;
            this.DialogResult = (result != DialogResult.Cancel ? DialogResult.OK : DialogResult.None);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // button for adding the attributes of the chosen layername to the listbox
            listBox2.Items.Clear();
            var chosen = listBox1.SelectedItem;

            DxfDocument dxfLoad = DxfDocument.Load(textBox1.Text);
            String tempPath = Path.GetTempPath();

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

            // An Excel-File sets the assignment of dxf-Layernames to the family-names, that comes from surveying regulation "bfr-Verm".
            string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string excelFile = null;
            if (textBox3.Text == "")
            {
                //excelFile = Path.GetFullPath(Path.Combine(assemblyPath, @"..\Zuordnungstabelle\Zuordnungstabelle.xlsx"));
                excelFile = @"D:\Daten\ZAFT\SurveyorsplanToRevit\Zuordnungstabelle\Zuordnungstabelle.xlsx";
                log.Info("No excel file chosen. The default path " + excelFile + " is used. ");
            }
            else
            {
                excelFile = textBox3.Text;
                log.Info("Excel file: " + excelFile + ". ");
            }

            string rfaNameAttribute = null;
            Excel.Application excelApp = null;
            excelApp = new Excel.Application();
            excelApp.Visible = false;
            Excel.Workbook wkb = null;
            wkb = excelApp.Workbooks.Open(excelFile);
            // opens the worksheet for either Attribute or layer definition and checks only column A for dxf-layer name
            Excel.Worksheet workSheetAttribute = wkb.Worksheets["Attribute"];
            Excel.Range columnRangeA = workSheetAttribute.Columns["C:C"];
            log.Info("Excel file loaded");

            List<string> tagList = new List<string>();
            List<string> tagListRfa = new List<string>();

            foreach (Insert ins in dxfLoad.Inserts)
            {
                var listString = ins.Attributes;

                if (ins.Layer.Name == chosen.ToString())
                {
                    foreach (var s in listString)
                    {
                        tagList.Add(s.Tag.ToString());
                    }
                }
            }
            var tagListDistinct = tagList.Distinct().ToList();
            tagListDistinct.Sort();

            if (tagListDistinct.Count()>0)
            {

                workSheetAttribute.Activate();
                foreach (var x in tagListDistinct)
                {
                    try
                    {
                        Excel.Range rngFindAttribute = columnRangeA.Find(x.ToString());
                        rfaNameAttribute = (excelApp.Cells[rngFindAttribute.Row, 1] as Excel.Range).Value;
                        tagListRfa.Add(rfaNameAttribute);
                    }
                    catch
                    {
                        TaskDialog.Show("Error","No entry found for '" + x + "'. ");
                        tagListRfa.Add(x);
                        log.Warn("No entry found for '" + x + "'. ");
                        //workSheetAttribute.Cells[17, 3] = x;

                        int lastRow = workSheetAttribute.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell, Type.Missing).Row;
                        Excel.Range rangeFind = columnRangeA.Find(x);

                        if (rangeFind == null)
                        {
                            workSheetAttribute.Cells[lastRow + 1, 3].Value = x;
                            workSheetAttribute.Cells[lastRow + 1, 4].Value = "Please fill out column 'rfa name' with an existing revit family name you want to use for '" + x + "'. ";
                            wkb.Save();
                        }

                    }
                }
                int ii = 0;
                foreach (string item in tagListRfa)
                {
                    if (item != null)
                    {
                        listBox2.Items.Add(tagListRfa[ii]);
                    }
                    else
                    {
                        log.Warn("There are entries that are not associated to an rfa-parameter. ");
                        TaskDialog.Show("Error", "There are entries that are not associated to an rfa-parameter. ");
                    }
                    ii++;
                }
            }
            else
            {
                log.Warn("There are no attributes for'" + chosen + "' in the dxf-file");
                TaskDialog.Show("Error", "No attributes found in dxf-file for '" + chosen + "'. ");
            }

            if (tagListRfa.Count()>0)
            {

            }
            wkb.Close();
            excelApp.Quit();
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            // this button is used to define new user defined attributes for the later import as family parameters
            string newItem = textBox2.Text;
            if (textBox2.Text.Trim() != string.Empty)
            {
                listBox2.Items.Add(newItem);
                textBox2.Clear();
            }
            else
            {
                TaskDialog.Show("Error", "Please enter an attribute name in the textbox.");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // this button removes an attribute so it will not get added as parameter
            listBox2.Items.Remove(listBox2.SelectedItem);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // button for opening a chosen dxf-file. Adds the layernames to a listbox the user can chose from. 
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            DxfDocument dxfLoad = DxfDocument.Load(textBox1.Text);

            List<string> layers = new List<string>();
            foreach (Insert insertEntity in dxfLoad.Inserts)
            {
                layers.Add(insertEntity.Layer.Name);
            }

            foreach (netDxf.Entities.Line lineEntity in dxfLoad.Lines)
            {
                layers.Add(lineEntity.Layer.Name);
            }

            foreach (Polyline polylineEntity in dxfLoad.Polylines)
            {
                layers.Add(polylineEntity.Layer.Name);
            }

            List<string> layersDistinct = new List<string>();
            layersDistinct = layers.Distinct().ToList();

            layersDistinct.Sort();
            layersDistinct.Remove("0");

            int ix = 0;
            foreach (string item in layersDistinct)
            {
                listBox1.Items.Add(layersDistinct[ix]);
                ix++;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            // this button opens the windows explorer to select a dxf file
            ofdDxf.Filter = "DXF|*.dxf|All files| *.*";
            if (ofdDxf.ShowDialog() == DialogResult.OK && ofdDxf.FileName.Length > 0)
            {
                textBox1.Text = ofdDxf.FileName;
            }
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }
        OpenFileDialog ofdDxf = new OpenFileDialog();

        private void button8_Click(object sender, EventArgs e)
        {
            ofdExcel.Filter = "XSLX|*.xlsx|All files| *.*";
            if (ofdExcel.ShowDialog() == DialogResult.OK && ofdExcel.FileName.Length > 0)
            {
                textBox3.Text = ofdExcel.FileName;
            }
        }

        private void openFileDialog2_FileOk(object sender, CancelEventArgs e)
        {

        }
        OpenFileDialog ofdExcel = new OpenFileDialog();

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 1; i <= 10; i++)
            {
                Thread.Sleep(10);
                backgroundWorker1.ReportProgress(0);
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value += 1;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Attributes added");
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }
}
