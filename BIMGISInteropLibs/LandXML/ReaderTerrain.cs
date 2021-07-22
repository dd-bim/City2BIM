using System;
using System.Collections.Generic;

//implement xml parser
using System.Xml;
using System.Xml.Linq;

//Integrate the BimGisCad
using BimGisCad.Representation.Geometry.Elementary;     //for points, lines, etc.
using BimGisCad.Representation.Geometry.Composed;       //for TIN processing

//Transfer class (Result) for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//Include IfcTerrain - Model for unit conversion
using static BIMGISInteropLibs.IfcTerrain.Common;

//embed for error handling
using System.Windows; //error handling (message box)

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace BIMGISInteropLibs.LandXML
{
    public class ReaderTerrain
    {
        /// <summary>
        /// Reading a XML file and processing it as a TIN
        /// </summary>
        /// <param name="fileName">Location of the LandXML file</param>
        /// <returns>TIN - for further processing in IFCTerrain (and Revit)</returns>
        public static Result ReadTin(JsonSettings jSettings)
        {
            string filePath = jSettings.filePath;

            //create a new result for passing the TIN
            var result = new Result();

            //[TODO]: Adding the error trapping
            try
            {
                //Invoke XML reader based on location
                using (var reader = XmlReader.Create(filePath))
                {
                    XElement el;
                    double? scale = null; //[TODO] Automate scaling based on input data
                    var pntIds = new Dictionary<string, int>();

                    //TIN-Builder erzeugen
                    var tinB = Tin.CreateBuilder(true);
                    LogWriter.Add(LogType.verbose, "[LandXML] TIN builder created.");

                    //Create PNR "artificially" & used for Indexing in TIN
                    int pnr = 0;

                    bool insideTin = false;

                    //TODO- What do this?
                    reader.MoveToContent();
                    
                    // Parse the file and display each of the nodes.
                    while (!reader.EOF)
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.LocalName)
                            {
                                case "Metric": //TODO WHY IS METRIC MISSING?
                                case "Imperial":
                                    el = XElement.ReadFrom(reader) as XElement;
                                    if (el != null)
                                    {
                                        var att = el.Attribute("linearUnit");
                                        if (att != null && ToMeter.TryGetValue(att.Value.ToLower(), out double tscale))
                                        { scale = tscale; }
                                    }
                                    break;
                                case "Definition":
                                    if (reader.MoveToFirstAttribute()
                                        && reader.LocalName == "surfType"
                                        && reader.Value.ToUpper() == "TIN")
                                    { insideTin = true; }
                                    break;
                                case "P":
                                    el = XElement.ReadFrom(reader) as XElement;
                                    if (el != null)
                                    {
                                        var att = el.Attribute("id");
                                        if (att != null && Point3.Create(
                                            el.Value.Replace(',', '.').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), out var pt))
                                        {
                                            //create point via BIMGISCADLib
                                            Point3 point = Point3.Create(pt.Y, pt.X, pt.Z);
                                            //add point to tin builder
                                            tinB.AddPoint(pnr, point);
                                            //logging
                                            LogWriter.Add(LogType.verbose, "[LandXML] Point (" + (pnr) + ") set (x= " + point.X + "; y= " + point.Y + "; z= " + point.Z + ")");
                                            //add to point indicies
                                            pntIds.Add(att.Value, pnr++);
                                        }
                                    }
                                    break;
                                case "F":
                                    el = XElement.ReadFrom(reader) as XElement;
                                    if (el != null)
                                    {
                                        string[] pts = el.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                        if (pts.Length == 3
                                            && pntIds.TryGetValue(pts[0], out int p1)
                                            && pntIds.TryGetValue(pts[1], out int p2)
                                            && pntIds.TryGetValue(pts[2], out int p3))
                                        {
                                            //Add the triangle over indexing the point numbers based on the IF loop above.
                                            tinB.AddTriangle(p1, p2, p3);

                                            //logging
                                            LogWriter.Add(LogType.verbose, "[LandXML] Triangle set (P1= " + (p1) + "; P2= " + (p2) + "; P3= " + (p3) + ")");
                                        }
                                    }
                                    break;
                                default:
                                    reader.Read();
                                    break;
                            }
                        }
                        else if (insideTin && reader.NodeType == XmlNodeType.EndElement && reader.Name == "Definition")
                        {
                            //Generate TIN from TIN Builder
                            Tin tin = tinB.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
                            
                            LogWriter.Add(LogType.debug, "[LandXML] TIN created via TIN builder.");

                            //return tin to result
                            result.Tin = tin;

                            //add to results (logging stats)
                            result.rPoints = tin.Points.Count;
                            result.rFaces = tin.NumTriangles;

                            //Logging
                            LogWriter.Add(LogType.info, "Reading LandXML data successful.");
                            LogWriter.Add(LogType.debug, "Points: " + result.Tin.Points.Count + "; Triangles: " + result.Tin.NumTriangles + " processed");

                            return result;
                        }
                        else
                        { reader.Read(); }
                    }
                }
            }
            catch (Exception ex)
            {
                //logging
                LogWriter.Add(LogType.error, "[LandXML] file could not be read (" + jSettings.fileName + ")");
                LogWriter.Add(LogType.error, "[LandXML]: " + ex.Message);
                
                //write to console
                Console.WriteLine("LandXML file could not be read: "+ Environment.NewLine + ex.Message);

                //return null result --> can not be processed
                return null;
            }
            return result;
        } //End ReadTIN
    }
}
