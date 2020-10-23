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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Excel = Microsoft.Office.Interop.Excel;

namespace lageplanImport
{
    public class helpclasses
    {
        double feetToMeter = 1.0 / 0.3048;
        double RE = 6380;       // earth radius in km

        public double splitUtm(double d,double utmOffset)
        {
            // Methode to remove the utm zone number by subtracting the offset given by the user. In this way utm zone number will not be taken for calculations, FeetToMeter convertations etc.
            double utmDouble = d;

            //string utmString = utmDouble.ToString();
            //string[] utmSplit = utmString.Split(',');
            if (utmOffset < d && utmOffset != 0)
            {
                utmDouble -= utmOffset;
                return utmDouble;
            }
            else 
            {
                return utmDouble;
            }
        }

        public double reduction(ProjectPosition projectData, double angle, double easting, double northing, double elevation)
        {
            // checks if there is a utm zone number. cuts it if so. In this way the zone number gets ignored when calculate the utm reduction. 

            // Der Ostwert des PBB wird als mittlerer Ostwert für die UTM Reduktion verwendet.
            double xSchwPktFt = easting;
            double xSchwPktKm = (double)((xSchwPktFt / feetToMeter) / 1000);
            double xSchwPkt500 = xSchwPktKm - 500;
            double R = default(double);
            double mittlHoeheKm = (Convert.ToDouble((elevation / feetToMeter), System.Globalization.CultureInfo.InvariantCulture)) / 1000;
            double kR = (-mittlHoeheKm / RE);
            double kAbb = ((xSchwPkt500) * (xSchwPkt500)) / (2 * RE * RE);
            R = (0.9996 * (1 + kAbb + kR));            
            return R;
        }

        public Transform transform(ProjectPosition projectData,double angle,double easting,double northing,double elevation)
        {
            Transform trot = Transform.CreateRotation(XYZ.BasisZ, -angle);
            XYZ vector = new XYZ(easting, northing, elevation);
            Transform ttrans = Transform.CreateTranslation(-vector);
            Transform transf = trot.Multiply(ttrans);

            return transf;
        }

        public DataTable createDataTable(Excel.Worksheet excelSheet)
        {
            int lastRow = excelSheet.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell, Type.Missing).Row ;
            int lastColumn = excelSheet.Cells.SpecialCells(Excel.XlCellType.xlCellTypeLastCell, Type.Missing).Column  ;

            DataTable dt = new DataTable();

            for (int h=1; h<=lastColumn;h++)
            {
                dt.Columns.Add((string)(excelSheet.Cells[1, h] as Excel.Range).Value);
            }

            for (int r = 1; r<=lastRow;r++)
            {
                DataRow toInsert = dt.NewRow();

                for (int c=1; c<=lastColumn -1 ;c++)
                {
                    toInsert[dt.Columns.Count - dt.Columns.Count + c - 1] = (string)(excelSheet.Cells[r, c] as Excel.Range).Value;
                }

                if (dt.Columns[0]!=null)
                {
                    dt.Rows.Add(toInsert);
                }
            }

            return dt;
        }

        public string getRfaName(DataTable dt, string layerName)
        {
            string rfaName = null;

            int lastRowLayer = dt.Rows.Count;
            for (int p = 0; p < lastRowLayer; p++)
            {
                if (dt.Rows[p][2].ToString() == layerName)
                {
                    if(dt.Rows[p][0].ToString()!=null)
                    {

                        rfaName = dt.Rows[p][0].ToString();
                        return rfaName;
                    }
                    else
                    {

                    }
                    
                }
                else
                {
                    rfaName = null;
                }
            }
            return rfaName;
        }
    }
}
