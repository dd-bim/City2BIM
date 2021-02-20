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

namespace BIMGISInteropLibs.CityGML
{
    class CityGMLReaderTerrain
    {
        /// <summary>
        /// Reads a TIN from a CityGML file
        /// </summary>
        /// <param name="fileName">Location of the CityGML file</param>
        /// <returns>TIN (in the form of result.tin)</returns>
        public static Result ReadTIN(string fileName)
        {
            //var logger = LogManager.GetCurrentClassLogger(); NLog removed

            //TIN-Builder
            var tinB = Tin.CreateBuilder(true);
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
                                            tinB.AddPoint(pnr++, pt2);
                                            tinB.AddPoint(pnr++, pt3);

                                            //adding Triangle to TIN-Builder (Referencing to point numbers just used)
                                            tinB.AddTriangle(pnr - 3, pnr - 2, pnr - 1, true);
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

                                    //logging [TODO]
                                    //logger.Info("Reading GML-data successful");
                                    //logger.Info(result.Tin.Points.Count() + " points; " + result.Tin.NumTriangles + " triangels processed");

                                    //Generate TIN from TIN Builder
                                    Tin tin = tinB.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
                                    result.Tin = tin;

                                    //Result handed over
                                    return result;
                                }
                            }
                        }
                    }
                    //result.Error = string.Format(Properties.Resources.errNoTIN, Path.GetFileName(fileName));
                    //logger.Error("No TIN-data found");
                    return result;
                }
            }
            //[TODO]: Pass error message and "Error" and add logging
            catch
            {
                //result.Error = string.Format(Properties.Resources.errFileNotReadable, Path.GetFileName(fileName));
                //logger.Error("File not readable");
                return result;
            }
        } //End ReadTIN
    }
}
