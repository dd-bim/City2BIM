using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//XML - Reader
using System.Xml;
using System.Xml.Linq;

//BimGisCad - Bibliothek einbinden
using BimGisCad.Representation.Geometry.Composed;       //TIN
using BimGisCad.Representation.Geometry.Elementary;     //Points, Lines, etc.

//Transfer class for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace BIMGISInteropLibs.CityGML
{
    class CityGMLReaderTerrain
    {
        /// <summary>
        /// Reads a TIN from a CityGML file
        /// </summary>
        /// <param name="fileName">Location of the CityGML file</param>
        /// <returns>TIN (in the form of result.tin)</returns>
        public static Result ReadTin(string fileName)
        {
            //var logger = LogManager.GetCurrentClassLogger(); NLog removed

            //TIN-Builder
            var tinB = Tin.CreateBuilder(true);
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "Create TIN builder"));
            int pnr = 0;

            //create new result to be able to transfer later
            var result = new Result();

            try
            {
                using (var reader = XmlReader.Create(fileName))
                {
                    bool isRelief = false;
                    reader.MoveToContent();
                    while (!reader.EOF && (reader.NodeType != XmlNodeType.Element || !(isRelief = reader.LocalName == "ReliefFeature")))
                    { reader.Read(); }
                    if (isRelief)
                    {
                        string id = reader.MoveToFirstAttribute() && reader.LocalName == "id" ? reader.Value : null;
                        bool insideTin = false;
                        while (!reader.EOF && (reader.NodeType != XmlNodeType.Element || !(insideTin = reader.LocalName == "tin")))
                        { reader.Read(); }
                        if (insideTin)
                        {
                            bool insideTri = false;
                            while (!reader.EOF && (reader.NodeType != XmlNodeType.Element || !(insideTri = reader.LocalName == "trianglePatches")))
                            { reader.Read(); }
                            if (insideTri)
                            {
                                while (!reader.EOF && (reader.NodeType != XmlNodeType.Element || !(insideTri = reader.LocalName == "Triangle")))
                                { reader.Read(); }
                                if (insideTri)
                                {
                                    while (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Triangle"
                                        && XElement.ReadFrom(reader) is XElement el)
                                    {
                                        var posList = el.Descendants().Where(d => d.Name.LocalName == "posList" && !d.IsEmpty);
                                        string[] pl;
                                        if (posList.Any()
                                            && (pl = posList.First().Value.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)).Length == 12
                                            && pl[0] == pl[9] && pl[1] == pl[10] && pl[2] == pl[11]
                                            && Point3.Create(pl, out var pt1)
                                            && Point3.Create(pl, out var pt2, 3)
                                            && Point3.Create(pl, out var pt3, 6))
                                        {
                                            //first Add points to tin with point index (pnr)
                                            tinB.AddPoint(pnr++, pt1);
                                            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[CityGML] Point (" + (pnr - 1) + ") set (x= " + pt1.X + "; y= " + pt1.Y + "; z= " + pt1.Z + ")"));
                                            tinB.AddPoint(pnr++, pt2);
                                            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[CityGML] Point (" + (pnr - 1) + ") set (x= " + pt2.X + "; y= " + pt2.Y + "; z= " + pt2.Z + ")"));
                                            tinB.AddPoint(pnr++, pt3);
                                            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[CityGML] Point (" + (pnr - 1) + ") set (x= " + pt3.X + "; y= " + pt3.Y + "; z= " + pt3.Z + ")"));
                                            //adding Triangle to TIN-Builder (Referencing to point numbers just used)
                                            tinB.AddTriangle(pnr - 3, pnr - 2, pnr - 1, true);
                                            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[CityGML] Triangle set (P1= " + (pnr-3) + "; P2= " + (pnr - 2) + "; P3= " + (pnr - 1) + ")"));
                                        }
                                        reader.Read(); // <-- check! is this necessary here? [TODO]
                                    }
                                    /* Check if TIN is "error free" Add a new TIN
                                    if(!tin.Points.Any() || !tin.FaceEdges.Any())
                                    {
                                        result.Error = string.Format(Properties.Resources.errNoTINData, Path.GetFileName(fileName));
                                        logger.Error("No TIN-data found");
                                        return result;
                                    }
                                    */

                                    //Generate TIN from TIN Builder
                                    Tin tin = tinB.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
                                    //logging
                                    LogWriter.Entries.Add(new LogPair(LogType.verbose, "[CityGML] Create TIN via TIN builder."));
                                    //handover tin to result
                                    result.Tin = tin;
                                    //logging
                                    LogWriter.Entries.Add(new LogPair(LogType.info, "Reading CityGML data successful."));
                                    LogWriter.Entries.Add(new LogPair(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed"));
                                    //Result handed over
                                    return result;
                                }
                            }
                        }
                    }

                    //result.Error = string.Format(Properties.Resources.errNoTIN, Path.GetFileName(fileName));
                    LogWriter.Entries.Add(new LogPair(LogType.error, "[CityGML] No TIN data found"));
                    return result;
                }
            }
            //[TODO]: Pass error message and "Error"
            catch
            {
                //result.Error = string.Format(Properties.Resources.errFileNotReadable, Path.GetFileName(fileName));
                
                LogWriter.Entries.Add(new LogPair(LogType.error, "[CityGML] File not readable"));

                return result;
            }
        } //End ReadTIN
    }
}
