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

//embed for error handling
using System.Windows; //error handling (message box)

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
        public static Result ReadTin(JsonSettings jSettings)
        {
            //read file name from settings
            string fileName = jSettings.filePath;

            //TIN-Builder
            var tinB = Tin.CreateBuilder(true);
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "Create TIN builder"));
            int pnr = 0;

            //init hash set
            var pList = new HashSet<Geometry.uPoint3>();

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
                                            //add points to point list
                                            int pnrP1 = Geometry.terrain.addToList(pList, pt1);
                                            int pnrP2 = Geometry.terrain.addToList(pList, pt2);
                                            int pnrP3 = Geometry.terrain.addToList(pList, pt3);
                                            
                                            //add triangle via indicies
                                            tinB.AddTriangle(pnrP1, pnrP2, pnrP3);
                                            LogWriter.Entries.Add(new LogPair(LogType.verbose, "[CityGML] Triangle set (P1= " + (pnr-3) + "; P2= " + (pnr - 2) + "; P3= " + (pnr - 1) + ")"));
                                        }
                                        reader.Read();
                                    }

                                    //loop through point list 
                                    foreach (Geometry.uPoint3 pt in pList)
                                    {
                                        //add point to tin builder
                                        tinB.AddPoint(pt.pnr, pt.point3);
                                    }

                                    //Generate TIN from TIN Builder
                                    Tin tin = tinB.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
                                    
                                    //logging
                                    LogWriter.Entries.Add(new LogPair(LogType.verbose, "[CityGML] Create TIN via TIN builder."));
                                    
                                    //handover tin to result
                                    result.Tin = tin;

                                    //add to results (stats)
                                    result.rPoints = tin.Points.Count;
                                    result.rFaces = tin.NumTriangles;

                                    //logging
                                    LogWriter.Entries.Add(new LogPair(LogType.info, "Reading CityGML data successful."));
                                    LogWriter.Entries.Add(new LogPair(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed"));
                                    //Result handed over
                                    return result;
                                }
                            }
                        }
                    }
                    else
                    {
                        //error logging
                        LogWriter.Entries.Add(new LogPair(LogType.error, "[CityGML] file (" + jSettings.fileName + ") no TIN data found!"));
                        MessageBox.Show("CityGML file contains no TIN data!", "CityGML file reader", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    

                    return result;
                }
            }
            //[TODO]: Pass error message and "Error"
            catch (Exception ex)
            {
                //logging
                LogWriter.Entries.Add(new LogPair(LogType.error, "[CityGML] file could not be read (" + jSettings.fileName + ")"));
                MessageBox.Show("CityGML file could not be read: \n" + ex.Message, "LandXML file reader", MessageBoxButton.OK, MessageBoxImage.Error);
                return result;
            }
        } //End ReadTIN
    }
}
