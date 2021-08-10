using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

//XML - Reader
using System.Xml;
using System.Xml.Linq;

//Transfer class for the reader (IFCTerrain + Revit)
using BIMGISInteropLibs.IfcTerrain;

//Logging
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

using NetTopologySuite.Geometries; //geometries for further processing 

namespace BIMGISInteropLibs.CityGML
{
    class CityGMLReaderTerrain
    {
        /// <summary>
        /// Reads DTM from a CityGML file<para/>
        /// [TODO] CityGML - Features review and expand ("gml::MultiCurve", "gml::Multipoint", ...)
        /// </summary>
        public static Result ReadTin(Config config)
        {
            var cityGmlResult = new Result();
            var triangleList = new List<Polygon>();

            try
            {
                using (var reader = XmlReader.Create(config.filePath))
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
                                            && pl[0] == pl[9] && pl[1] == pl[10] && pl[2] == pl[11])
                                        {
                                            double.TryParse(pl[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double p1X);
                                            double.TryParse(pl[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double p1Y);
                                            double.TryParse(pl[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double p1Z);
                                            Coordinate c1 = new CoordinateZ(p1X, p1Y, p1Z);
                                            
                                            double.TryParse(pl[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double p2X);
                                            double.TryParse(pl[4], NumberStyles.Any, CultureInfo.InvariantCulture, out double p2Y);
                                            double.TryParse(pl[5], NumberStyles.Any, CultureInfo.InvariantCulture, out double p2Z);
                                            Coordinate c2 = new CoordinateZ(p1X, p1Y, p1Z);

                                            double.TryParse(pl[6], NumberStyles.Any, CultureInfo.InvariantCulture, out double p3X);
                                            double.TryParse(pl[7], NumberStyles.Any, CultureInfo.InvariantCulture, out double p3Y);
                                            double.TryParse(pl[8], NumberStyles.Any, CultureInfo.InvariantCulture, out double p3Z);
                                            Coordinate c3 = new CoordinateZ(p1X, p1Y, p1Z);

                                            LinearRing shell = new LinearRing(new Coordinate[] { c1, c2, c3, c1 });
                                            Polygon triangle = new Polygon(shell);

                                            triangleList.Add(triangle);
                                        }
                                        reader.Read();
                                    }

                                    
                                    //logging
                                    LogWriter.Add(LogType.info, "Reading CityGML data successful.");

                                    cityGmlResult.currentConversion = DtmConversionType.faces;

                                    cityGmlResult.triangleList = triangleList;

                                    //Result handed over
                                    return cityGmlResult;
                                }
                            }
                        }
                    }
                    else
                    {
                        //error logging
                        LogWriter.Add(LogType.error, "[CityGML] file (" + config.fileName + ") no TIN data found!");
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                //logging
                LogWriter.Add(LogType.error, "[CityGML] Error: " + ex.Message);
                return null;
            }
        } //End ReadTIN
    }
}
