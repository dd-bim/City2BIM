using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

using Autodesk.Revit;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace NasImport
{

    public enum ImportFormat
    {


        /// <summary>
        /// XML format
        /// </summary>
        XML

    }


    public class MainData
    {
        Autodesk.Revit.UI.UIApplication m_revit; // Store the reference of the application in revit
        Autodesk.Revit.Creation.Application m_createApp;// Store the create Application reference
        Autodesk.Revit.Creation.Document m_createDoc;   // Store the create Document reference

        ModelCurveArray m_lineArray;        // Store the ModelLine references



        // Revit command data
        private ExternalCommandData m_commandData;

        // Whether current view is a 3D view
        private bool m_is3DView;

        /// <summary>
        /// Revit command data
        /// </summary>
        public ExternalCommandData CommandData
        {
            get
            {
                return m_commandData;
            }
        }

        /// <summary>
        /// Whether current view is a 3D view
        /// </summary>
        public bool Is3DView
        {
            get
            {
                return m_is3DView;
            }
            set
            {
                m_is3DView = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="commandData">Revit command data</param>
        public MainData(ExternalCommandData commandData)
        {
            m_commandData = commandData;

            //Whether current active view is 3D view
            if (commandData.Application.ActiveUIDocument.Document.ActiveView.ViewType == Autodesk.Revit.DB.ViewType.ThreeD)
            {
                m_is3DView = true;
            }
            else
            {
                m_is3DView = false;
            }
        }

        /// <summary>
        /// Get the format to import
        /// </summary>
        /// <param name="selectedFormat">Selected format in format selecting dialog</param>
        /// <returns>The format to import</returns>
        private static ImportFormat GetSelectedImportFormat(string selectedFormat)
        {
            ImportFormat format = ImportFormat.XML;
            switch (selectedFormat)
            {

                case "XML":
                    format = ImportFormat.XML;
                    break;

                default:
                    break;
            }

            return format;
        }

        /// <summary>
        /// Export according to selected format
        /// </summary>
        /// <param name="selectedFormat">Selected format</param>
        /// <returns></returns>

        public DialogResult Import(string selectedFormat)
        {
            DialogResult dialogResult = DialogResult.OK;
            ImportFormat format = GetSelectedImportFormat(selectedFormat);

            try
            {
                switch (format)
                {
                    case ImportFormat.XML:
                        ImportXMLData importXMLData = new ImportXMLData(m_commandData, format);
                        dialogResult = Import(importXMLData);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
                String errorMessage = "Failed to import " + format + " format";
                TaskDialog.Show("Error", errorMessage, TaskDialogCommonButtons.Ok);
            }

            return dialogResult;
        }


        /// <summary>
        /// Import
        /// </summary>
        /// <param name="data"></param>
        private static DialogResult Import(ImportData data)
        {
            String returnFilename = String.Empty;
            DialogResult result = ShowOpenDialog(data, ref returnFilename);
            if (result != DialogResult.Cancel)
            {
                data.ImportFileFullName = returnFilename;
                if (!data.Import())
                {
                    TaskDialog.Show("Import", "Cannot import " + Path.GetFileName(data.ImportFileFullName) +
                        " in current settings.", TaskDialogCommonButtons.Ok);
                }
            }

            return result;
        }

        /// <summary>
        /// Show Open File dialog
        /// </summary>
        /// <param name="importData">Data to import</param>
        /// <param name="returnFileName">File name will be returned</param>
        /// <returns>Dialog result</returns>
        public static DialogResult ShowOpenDialog(ImportData importData, ref String returnFileName)
        {
            using (OpenFileDialog importDialog = new OpenFileDialog())
            {
                importDialog.Title = importData.Title;
                importDialog.InitialDirectory = importData.ImportFolder;
                importDialog.Filter = importData.Filter;
                importDialog.RestoreDirectory = true;

                DialogResult result = importDialog.ShowDialog();
                if (result != DialogResult.Cancel)
                {
                    returnFileName = importDialog.FileName;
                }

                return result;
            }
        }

        public MainData(Autodesk.Revit.UI.UIApplication revit)
        {
            ModelCurveArray m_lineArray;        // Store the ModelLine references
            List<SketchPlane> m_sketchArray;    // Store the SketchPlane references
            //List<ModelCurveCounter> m_informationMap;   // Store the number of each model line type
            // Store the reference of the application for further use.
            m_revit = revit;
            // Get the create references
            m_createApp = m_revit.Application.Create;       // Creation.Application
            m_createDoc = m_revit.ActiveUIDocument.Document.Create;// Creation.Document

            // Construct all the ModelCurveArray instances for model lines
            m_lineArray = new ModelCurveArray();

            // Construct the sketch plane list data
            m_sketchArray = new List<SketchPlane>();

            // Construct the information list data
            //m_informationMap = new List<ModelCurveCounter>();



        }
        public void CreateLine(int sketchId, Autodesk.Revit.DB.XYZ startPoint, Autodesk.Revit.DB.XYZ endPoint)
        {
            try
            {
                // First get the sketch plane by the giving element id.
                SketchPlane workPlane = GetSketchPlaneById(sketchId);
                // Additional check: start point should not equal end point
                if (startPoint.Equals(endPoint))
                {
                    throw new ArgumentException("Two points should not be the same.");
                }

                // create geometry line
                Line geometryLine = Line.CreateBound(startPoint, endPoint);
                if (null == geometryLine)       // assert the creation is successful
                {
                    throw new Exception("Create the geometry line failed.");
                }
                // create the ModelLine
                ModelLine line = m_createDoc.NewModelCurve(geometryLine, workPlane) as ModelLine;
                if (null == line)               // assert the creation is successful
                {
                    throw new Exception("Create the ModelLine failed.");
                }
                // Add the created ModelLine into the line array
                m_lineArray.Append(line);

                // Finally refresh information map.
                //RefreshInformationMap();

            }
            catch (Exception ex)
            {
                throw new Exception("Can not create the ModelLine, message: " + ex.Message);
            }

        }
        Autodesk.Revit.DB.Element GetElementById(int id)
        {
            // Create a Autodesk.Revit.DB.ElementId data
            Autodesk.Revit.DB.ElementId elementId = new Autodesk.Revit.DB.ElementId(id);

            // Get the corresponding element
            return m_revit.ActiveUIDocument.Document.GetElement(elementId);
        }
        SketchPlane GetSketchPlaneById(int id)
        {
            // First get the sketch plane by the giving element id.
            SketchPlane workPlane = GetElementById(id) as SketchPlane;
            if (null == workPlane)
            {
                throw new Exception("Don't have the work plane you select.");
            }
            return workPlane;
        }

        class Creator
        {
            static SketchPlane NewSketchPlanePassLine(UIApplication pUiapp, Line line)
            {
                Document doc = pUiapp.ActiveUIDocument.Document;
                XYZ p = line.GetEndPoint(0);
                XYZ q = line.GetEndPoint(1);
                XYZ norm;
                if (p.X == q.X)
                {
                    norm = XYZ.BasisX;
                }
                else if (p.Y == q.Y)
                {
                    norm = XYZ.BasisY;
                }
                else
                {
                    norm = XYZ.BasisZ;
                }
                Plane plane = Plane.CreateByNormalAndOrigin(norm, p);
                SketchPlane skPlane = SketchPlane.Create(doc, plane);
                return skPlane;
            }

            public static Result Create3DModelLine(UIApplication pUiapp, XYZ p, XYZ q, string pstrLineStyle, bool pblnIsFamily)
            {
                Document doc = pUiapp.ActiveUIDocument.Document;
                Result lresResult = Result.Failed;
                using (Transaction tr = new Transaction(doc, "Create3DModelLine"))
                {
                    tr.Start();
                    try
                    {
                        if (p.IsAlmostEqualTo(q))
                        {
                            throw new System.ArgumentException("Expected two different points.");
                        }
                        Line line = Line.CreateBound(p, q);
                        if (null == line)
                        {
                            throw new Exception("Geometry line creation failed.");
                        }
                        ModelCurve mCurve = null;
                        if (pblnIsFamily)
                        {
                            mCurve = doc.FamilyCreate.NewModelCurve(line, NewSketchPlanePassLine(pUiapp, line)); // not needed , pblnIsFamily));
                        }
                        else
                        {
                            mCurve = doc.Create.NewModelCurve(line, NewSketchPlanePassLine(pUiapp, line)); // not needed , pblnIsFamily));
                        }
                        // set linestyle
                        ICollection<ElementId> styles = mCurve.GetLineStyleIds();
                        foreach (ElementId eid in styles)
                        {
                            Element e = doc.GetElement(eid);
                            if (e.Name == pstrLineStyle)
                            {
                                mCurve.LineStyle = e;
                                break;
                            }
                        }
                        tr.Commit();
                        lresResult = Result.Succeeded;
                    }
                    catch (Autodesk.Revit.Exceptions.ExternalApplicationException ex)
                    {
                        MessageBox.Show(ex.Source + Environment.NewLine + ex.StackTrace + Environment.NewLine + ex.Message);
                        // tr.RollBack();
                    }
                }
                return lresResult;
            }

            /*
            // usage
            // all in feet remember
            // line 1
            XYZ StartPt = new XYZ(0, 0, 0);
            XYZ EndPt = new XYZ(10, 0, 10);
            string lstrLineStyle = "<Hidden>"; // system linestyle guaranteed to exist
            lresResult = Creator.Create3DModelLine(pUiapp, StartPt, EndPt, lstrLineStyle, true);
        // line 2
        StartPt = EndPt;
        EndPt = new XYZ(10, 10, 20);
            lresResult = Creator.Create3DModelLine(pUiapp, StartPt, EndPt, lstrLineStyle, true);*/
        }
    }
}