using System;
using System.Collections.Generic;
using System.Globalization;

//implement xml parser
using System.Xml;
using System.Xml.Linq;

//Transfer class (Result) for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//Include IfcTerrain - Model for unit conversion
using static BIMGISInteropLibs.IfcTerrain.Common;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

using NetTopologySuite.Geometries; //NTS for geometry data exchange

namespace BIMGISInteropLibs.LandXML
{
    public class ReaderTerrain
    {
        /// <summary>
        /// Reading a LandXML file
        /// </summary>
        public static Result readDtmData(Config config)
        {
            var pointList = new List<Point>();
            var triangleList = new List<Polygon>();
            var lines = new List<LineString>();
            var triangleMap = new HashSet<Triangulator.triangleMap>();

            //Invoke XML reader based on location
            using (var reader = XmlReader.Create(config.filePath))
            {
                XElement el;

                double? scale = null; //[TODO] Automate scaling based on input data
                
                reader.MoveToContent();

                bool insideTin = false;

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

                            //case point data
                            case "P":
                                el = XElement.ReadFrom(reader) as XElement;
                                if (el != null)
                                {
                                    var att = el.Attribute("id");

                                    if (att != null)
                                    {
                                        //parse point number
                                        int.TryParse(att.Value, out int pnr);

                                        //parse point values
                                        var pt = el.Value.Replace(',', '.').Split(new[] { ' ' },
                                        StringSplitOptions.RemoveEmptyEntries);

                                        //TODO -> left or right handed?
                                        double.TryParse(pt[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double x);
                                        double.TryParse(pt[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double y);
                                        double.TryParse(pt[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double z);

                                        dynamic p = null;

                                        //create point
                                        if (config.mathematicCRS.GetValueOrDefault())
                                        {
                                            p = new Point(y, x, z);

                                        }
                                        else
                                        {
                                            p = new Point(x, y, z);
                                        }
                                        
                                        //set point number
                                        p.UserData = pnr;

                                        //add point to point list
                                        pointList.Add(p);
                                    }
                                }
                                break;
                            //case face
                            case "F":
                                el = XElement.ReadFrom(reader) as XElement;
                                if (el != null)
                                {
                                    string[] pts = el.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (pts.Length == 3)
                                    {
                                        //parse point index
                                        int.TryParse(pts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int indexP1);
                                        int.TryParse(pts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int indexP2);
                                        int.TryParse(pts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int indexP3);

                                        //find point in pointlist
                                        int p1 = pointList.FindIndex(p => (int)p.UserData == indexP1);
                                        int p2 = pointList.FindIndex(p => (int)p.UserData == indexP2);
                                        int p3 = pointList.FindIndex(p => (int)p.UserData == indexP3);

                                        //add indexed map
                                        triangleMap.Add(new Triangulator.triangleMap()
                                        {
                                            triNumber = triangleMap.Count,
                                            triValues = new int[] { p1, p2, p3 }
                                        });
                                    }
                                }
                                break;


                            //case breaklines
                            case "Breakline":

                                el = XElement.ReadFrom(reader) as XElement;

                                List<CoordinateZ> coords = new List<CoordinateZ>();

                                if (el != null)
                                {
                                    string[] pts = el.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                    int j = 0;
                                    do
                                    {
                                        int i = j;

                                        //parse point index
                                        double.TryParse(pts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double Y);
                                        double.TryParse(pts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double X);
                                        double.TryParse(pts[i + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out double Z);

                                        if (config.mathematicCRS.GetValueOrDefault())
                                        {
                                            coords.Add(new CoordinateZ(Y, X, Z));
                                        }
                                        else
                                        {
                                            coords.Add(new CoordinateZ(X, Y, Z));
                                        }

                                        j = j + 3;
                                    }
                                    while (j < pts.Length);
                                }


                                bool isClosed = false;
                                int v = 0;

                                do
                                {
                                    CoordinateZ[] cs = new CoordinateZ[2];
                                    if (!(coords[v].Equals(coords[0])
                                        && v != 0))
                                    {
                                        cs[0] = coords[v];
                                        cs[1] = coords[v++];
                                        
                                        LineString ls = new LineString(cs);
                                        lines.Add(ls);
                                    }
                                    else
                                    {
                                        isClosed = true;
                                    }
                                } while (!isClosed && v < coords.Count);
                                break; 
                                


                            //
                            default:
                                reader.Read();
                                break;
                        }
                    }
                    else if (insideTin && reader.NodeType == XmlNodeType.EndElement && reader.Name == "Definition")
                    {
                        //Logging
                        LogWriter.Add(LogType.info, "Reading LandXML data successful.");

                        //
                        var landXmlResult = new Result();

                        if(triangleList.Count != 0 && lines.Count == 0)
                        {
                            throw new NotImplementedException();
                            /*
                            landXmlResult.currentConversion = DtmConversionType.faces;
                            landXmlResult.triangleList = triangleList;
                            return landXmlResult;
                            */
                        }
                        else if (triangleList.Count != 0 && lines.Count != 0)
                        {
                            throw new NotImplementedException();
                            /*
                            landXmlResult.currentConversion = DtmConversionType.faces_breaklines;
                            landXmlResult.triangleList = triangleList;
                            landXmlResult.lines = lines;
                            return landXmlResult;
                            */

                        }
                        //case if no triangles are defined
                        else if (pointList.Count != 0 && triangleMap.Count == 0)
                        {
                            landXmlResult.currentConversion = DtmConversionType.points;
                            landXmlResult.pointList = pointList;
                            return landXmlResult;
                        }
                        //if triangles are defined 
                        else if (pointList.Count != 0 && triangleMap.Count != 0 && !config.breakline.GetValueOrDefault())
                        {
                            landXmlResult.currentConversion = DtmConversionType.conversion;
                            landXmlResult.pointList = pointList;
                            landXmlResult.triMap = triangleMap;
                            return landXmlResult;
                        }
                        else if(pointList.Count != 0 && triangleMap.Count != 0 && lines.Count != 0)
                        {
                            landXmlResult.currentConversion = DtmConversionType.points_breaklines;
                            landXmlResult.pointList = pointList;
                            landXmlResult.lines = lines;
                            return landXmlResult;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    { reader.Read(); }
                }
            }
            return null;
        } //End ReadTIN
    }
}
