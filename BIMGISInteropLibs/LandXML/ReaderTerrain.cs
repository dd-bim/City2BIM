using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//implement xml parser
using System.Xml;
using System.Xml.Linq;

//Integrate the BimGisCad
using BimGisCad.Representation.Geometry.Elementary;     //for points, lines, etc.
using BimGisCad.Representation.Geometry.Composed;       //for TIN processing

//Transfer class (Result) for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IFCTerrain;

//Include IfcTerrain - Model for unit conversion
using static BIMGISInteropLibs.IFCTerrain.Common;

namespace BIMGISInteropLibs.LandXML
{
    class ReaderTerrain
    {
        /// <summary>
        /// Reading a XML file and processing it as a TIN
        /// </summary>
        /// <param name="fileName">Location of the LandXML file</param>
        /// <returns>TIN - for further processing in IFCTerrain (and Revit)</returns>
        public static Result ReadTIN(string fileName)
        {
            //create a new result for passing the TIN
            var result = new Result();

            //var logger = LogManager.GetCurrentClassLogger(); removed - is to be replaced by Serilog

            //[TODO]: Adding the error trapping
            try
            {
                //Invoke XML reader based on location
                using (var reader = XmlReader.Create(fileName))
                {
                    XElement el;
                    double? scale = null; //[TODO] Automate scaling based on input data
                    var pntIds = new Dictionary<string, int>();

                    //TIN-Builder erzeugen
                    var tinB = Tin.CreateBuilder(true);

                    //Create PNR "artificially" & used for Indexing in TIN
                    int pnr = 0;

                    bool insideTin = false;
                    reader.MoveToContent();
                    
                    // Parse the file and display each of the nodes.
                    while (!reader.EOF)
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.LocalName)
                            {
                                case "Metric": //WHY IS METRIC MISSING?
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
                                            Point3 point = Point3.Create(pt.Y, pt.X, pt.Z);
                                            tinB.AddPoint(pnr, point);
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
                            /* Adding a check: whether TIN is damaged, if any. [TODO]
                            if(!mesh.Points.Any() || !mesh.FaceEdges.Any())
                            {
                                result.Error = string.Format(Properties.Resources.errNoTINData, Path.GetFileName(fileName));
                                logger.Error("No TIN-data found");
                                return result;
                            }
                            */

                            //Logging [TODO]
                            //logger.Info("Reading LandXML-Data successful");
                            //logger.Info(result.Tin.Points.Count() + " points; " + result.Tin.NumTriangles + " triangels processed");

                            //Generate TIN from TIN Builder
                            Tin tin = tinB.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
                            result.Tin = tin;

                            return result;
                        }
                        else
                        { reader.Read(); } //[TODO]
                    }
                }
            }
            catch
            {
                //result.Error = string.Format(Properties.Resources.errFileNotReadable, Path.GetFileName(fileName));
                //logger.Error("File not readable");
                return result;
            }
            //result.Error = string.Format(Properties.Resources.errNoTIN, Path.GetFileName(fileName));
            //logger.Error("No TIN-data found");
            return result;
        } //End ReadTIN

    }
}
