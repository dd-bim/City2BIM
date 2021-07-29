using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using System.IO;

using Microsoft.Win32; //file dialog handling
using System.Windows.Input; //mouse coursor
using System.ComponentModel; //background worker

using IxMilia.Dxf;

namespace City2RVT.GUI.Surveyorsplan2BIM
{
    /// <summary>
    /// util collection to support import for surveyorsplan
    /// </summary>
    public class utils
    {
        /// <summary>
        /// import family
        /// </summary>
        /// <param name="absFilePath"></param>
        /// <param name="doc"></param>
        public static Family importFamily(string storagePath, string familyName, Document doc)
        {
            //without *.rfa
            string famName = Path.GetFileNameWithoutExtension(familyName);

            //check if family is allready in document
            Family family = utils.findFamilyViaName(doc, famName);
            
            //if family has been found this will be used
            if(family != null)
            {
                return family;
            }
            //import family to document
            else
            {
                //read family as transaction (otherwise: famliy = null)
                using (Transaction t = new Transaction(doc))
                {
                    //start transaction
                    t.Start("Load family: " + familyName);

                    //get absoult path
                    var absPath = Path.Combine(storagePath, familyName);

                    try
                    {
                        //load via absoult path
                        doc.LoadFamily(absPath, out family);

                        //commit transaction (family will be loaded)
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        //return error message to user
                        TaskDialog.Show("Family loading", ex.Message, TaskDialogCommonButtons.Ok);

                        //rollback transaction family loading failed
                        t.RollBack();
                    }
                }
                return family;
            }
        }

        /// <summary>
        /// find family via family name <para/>
        /// Hint: does not need to be in a transaction!
        /// </summary>
        /// <param name="doc">current revit doc</param>
        /// <param name="familyName">name of the family (for selection)</param>
        /// <returns></returns>
        public static Family findFamilyViaName(Document doc, string familyName)
        {
            //init collector
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            //set filter via input (targetType
            collector = collector.OfClass(typeof(Family));

            //get family 
            Family family = collector.FirstOrDefault<Element>(el => el.Name.Equals(familyName)) as Family;

            if(family != null)
            {
                //return family
                return family;
            }
            else
            {
                //no family found (can be used for request)
                return null;
            }
        }

        /// <summary>
        /// constructor class for 
        /// </summary>
        public class familyParameterData
        {
            public string Family { get; set; }
            public string BuiltinParameter { get; set; }
            public string ParameterType { get; set; }
            public string ParameterName { get; set; }
            public string ParameterGroup { get; set; }
            public string BuiltinGroup { get; set; }

            public static familyParameterData GetParameterData(FamilyParameter familyparam, Document doc)
            {
                familyParameterData parameterdata = new familyParameterData
                {
                    Family = Path.GetFileNameWithoutExtension(doc.Title.ToString()),
                    ParameterName = familyparam.Definition.Name,
                    BuiltinParameter = ((InternalDefinition)familyparam.Definition).BuiltInParameter.ToString(),
                    ParameterGroup = LabelUtils.GetLabelFor(familyparam.Definition.ParameterGroup),
                    BuiltinGroup = familyparam.Definition.ParameterGroup.ToString(),
                    ParameterType = familyparam.Definition.ParameterType.ToString(),
                };

                return parameterdata;
            }
        }

        /// <summary>
        /// read family parameters from rfa file (TODO: error handling)
        /// </summary>
        public static List<familyParameterData> readFamilyParameterInfo(Application app, string absPath)
        {
            //open revit family file in a separate document
            var doc = app.OpenDocumentFile(absPath);

            /// Get the familyManager instance from the open document
            var familyManager = doc.FamilyManager;
            
            //int totalParams = familyManager.Parameters.Size;

            List<familyParameterData> ParametersData = new List<familyParameterData>();

            foreach (FamilyParameter familyParameter in familyManager.Parameters)
            {
                /// Add Parameter Data into a list
                ParametersData.Add(familyParameterData.GetParameterData(familyParameter, doc));
            }

            return ParametersData;
        }

        /// <summary>
        /// mappingList class
        /// </summary>
        public class mappingEntry
        {
            /// <summary>
            /// enumeration of the specific type
            /// </summary>
            public survObjType survObjType { get; set; }

            /// <summary>
            /// file name of the used dxf file
            /// </summary>
            public string dxfName { get; set; } = null;

            /// <summary>
            /// family name
            /// </summary>
            public string familyName { get; set; } = null;

            /// <summary>
            /// mapped parameters
            /// </summary>
            public Dictionary<string, string> parameterMap { get; set; }
        }

        /// <summary>
        /// different object type for a CAD2BIM conversion
        /// </summary>
        public enum survObjType
        {
            Point,
            MultiPoint,
            Line,
            Polyline,
            Surface
        }

        /// <summary>
        /// TODO: find a better name
        /// </summary>
        public class mapping
        {
            /// <summary>
            /// check if needed
            /// </summary>
            public Guid mappingId { get; set; }

            /// <summary>
            /// file path to be used in conversion
            /// </summary>
            public string dxfFileName { get; set; }

            /// <summary>
            /// directory of all family files to be used in converison
            /// </summary>
            public string familyDir { get; set; }

            /// <summary>
            /// list of mapping entrys
            /// </summary>
            public List<mappingEntry> mappingEntrys { get; set; }

            public static mapping init()
            {
                mapping mapping = new mapping
                {
                    mappingId = Guid.NewGuid(),
                    mappingEntrys = new List<mappingEntry>(),
                };

                return mapping;
            }
        }

        /// <summary>
        /// init background worker
        /// </summary>
        private static readonly BackgroundWorker backgroundWorker = new BackgroundWorker();

        private static AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        /// <summary>
        /// init do and done tasks
        /// </summary>
        public static void initBackgroundWorkerDxf()
        {
            //init do task
            backgroundWorker.DoWork += backgroundWorker_DoWork;

            //task when "do" is completed
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        }

        private static bool dxfSuccess { get; set; }
        public static string dxfFileName { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        private static void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //file name
            string fileName = (string)e.Result;

            //if file path is empty (file does not exist)
            if (string.IsNullOrEmpty(fileName))
            {
                dxfSuccess = false;
            }
            else
            {
                dxfFileName = fileName;
                dxfSuccess = true;
            }

            //change mouse cursor to default
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// dialog to open dxf file
        /// </summary>
        /// <returns></returns>
        public static void readDxfFileDialog()
        {
            //new file dialog handler
            var ofd = new OpenFileDialog();

            //set file filter
            ofd.Filter = "DXF Files *.dxf, *.dxb|*.dxf;*.dxb";

            //open dialog window
            if (ofd.ShowDialog() == true)
            {
                dxfFileName = ofd.FileName;
            }
        }

        /// <summary>
        /// check if file exsists
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //if file exsists get file name if not: set to null
            e.Result = File.Exists((string)e.Argument) ? (string)e.Argument : null;
        }

        /// <summary>
        /// reader for file name
        /// </summary>
        /// <returns>dxf file (ixMilia conform)</returns>
        public static bool openDxfFile(string fileName, out DxfFile dxfFile)
        {
            //try to open fileName
            try
            {
                using (var fileStream = new FileStream(fileName, FileMode.Open))
                {
                    //will be returned via "out"
                    dxfFile = DxfFile.Load(fileStream);

                    //return true - file could be opend
                    return true;
                }
            }
            catch (Exception ex)
            {
                //return error message
                TaskDialog.Show("DXF file could not be read:" + Environment.NewLine + ex.Message, "DXF file reader");

                //set dxf file to false
                dxfFile = null;

                //return false - file could not be opend
                return false;
            }
        }



    }
}
