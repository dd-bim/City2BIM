using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Xml;
using netDxf;
using netDxf.Entities;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

using Excel = Microsoft.Office.Interop.Excel;
using lageplanImport.Lines;

namespace lageplanImport
{
    public class Inserts
    {
        public List<Insert> importInserts(string filePath)
        {
            //MessageBox.Show("dxf: " + filePath.ToString());
            DxfDocument dxfLoad = DxfDocument.Load(filePath);
            List<Insert> inserts = new List<Insert>();

            string excelFile = @"D:\Daten\ZAFT\Außenbereich für BIM\Ausgangsdaten\Zuordnungstabelle\Zuordnungstabelle.xlsx";
            string rfaNameInserts = null;
            Excel.Application excelApp = null;
            excelApp = new Excel.Application();
            excelApp.Visible = true;
            Excel.Workbook wkb = null;
            wkb = excelApp.Workbooks.Open(excelFile);
            //Family fam = null;

            #region insert
            foreach (Insert insertEntity in dxfLoad.Inserts)
            {
                string dxfLayerName = insertEntity.Layer.Name;
                try
                {
                    Excel.Range rngFind = excelApp.Cells.Find(dxfLayerName);
                    rfaNameInserts = (excelApp.Cells[rngFind.Row, 3] as Excel.Range).Value;

                    // inserts wird nur gefüllt, wenn der Layername in der Excel-Tabelle gefunden wird, sonst übersprungen (catch)
                    inserts.Add(insertEntity);
                }
                catch
                {
                    if (dxfLayerName != "0")
                    {
                        MessageBox.Show("Für den Insert-Layer " + dxfLayerName + " existiert (noch) keine Zuordnung. ");
                    }
                    else
                    {

                    }
                }
            }
            #endregion insert
            return inserts;
        }

        public List<netDxf.Entities.Line> importLines(string filePath)
        {
            //MessageBox.Show("dxf: " + filePath.ToString());
            DxfDocument dxfLoad = DxfDocument.Load(filePath);
            List<netDxf.Entities.Line> lines = new List<netDxf.Entities.Line>();

            string excelFile = @"D:\Daten\ZAFT\Außenbereich für BIM\Ausgangsdaten\Zuordnungstabelle\Zuordnungstabelle.xlsx";
            string rfaNameInserts = null;
            Excel.Application excelApp = null;
            excelApp = new Excel.Application();
            excelApp.Visible = true;
            Excel.Workbook wkb = null;
            wkb = excelApp.Workbooks.Open(excelFile);
            //Family fam = null;

            #region lines
            #region Familie laden
            foreach (netDxf.Entities.Line lineEntity in dxfLoad.Lines)
            {
                string layerNameLines = lineEntity.Layer.Name;
                try
                {
                    lines.Add(lineEntity);

                    Excel.Range rngFind = excelApp.Cells.Find(layerNameLines);
                    rfaNameInserts = (excelApp.Cells[rngFind.Row, 3] as Excel.Range).Value;

                }
                catch
                {
                    //MessageBox.Show("Für den Linien-Layer " + layerNameLines + " existiert (noch) keine Zuordnung. ");
                }
            }
            #endregion Familie laden     
            //MessageBox.Show(lines.Count().ToString());


            #endregion lines
            return lines;
        }
    }
}
