using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

namespace NasImport
{
    class AlkisXmlParser
    {
        public AlkisXmlParser()
        {

        }
    }
    class FlstPoint
    {
        public FlstPoint(double x, double y, int id)
        {
            this.X = x;
            this.Y = y;
            this.Id = id;
        }
        private double X;
        private double Y;
        private double Id;
    }

    class Flurstueck
    {
        public Flurstueck (int p1, int p2)
        {
            this.Point1 = p1;
            this.Point2 = p2;
        }
        private int Point1;
        private int Point2;
    }
    public class ReadData
    {
        public List<string> ReadCoordFromXML()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Bestandsdaten_VERMESSUNGSSTELLEN_Auszug.XML");

            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ns1", "http://www.ibr-bonn.de/ibr");
            nsmgr.AddNamespace("ns2", "http://www.adv-online.de/namespaces/adv/gid/6.0");
            nsmgr.AddNamespace("adv", "http://www.adv-online.de/namespaces/adv/gid/6.0");
            nsmgr.AddNamespace("gco", "http://www.isotc211.org/2005/gco");
            nsmgr.AddNamespace("gmd", "http://www.isotc211.org/2005/gmd");
            nsmgr.AddNamespace("gml", "http://www.opengis.net/gml/3.2");
            nsmgr.AddNamespace("ows", "http://www.opengis.net/ows");
            nsmgr.AddNamespace("wfs", "http://www.adv-online.de/namespaces/adv/gid/wfs");
            nsmgr.AddNamespace("wfsext", "http://www.adv-online.de/namespaces/adv/gid/wfsext");
            nsmgr.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
            nsmgr.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
            nsmgr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            nsmgr.AddNamespace("ogc", "http://www.adv-online.de/namespaces/adv/gid/ogc");

            XmlNodeList fKoord = doc.SelectNodes("//ns2:AX_Flurstueck/ns2:position/gml:Surface/gml:patches/gml:PolygonPatch/gml:exterior/gml:Ring/gml:curveMember/gml:Curve/gml:segments/gml:LineStringSegment", nsmgr);
            List<string> list = new List<string>();
            foreach (XmlNode itemNode2 in fKoord)
            {
                XmlNode flstNode = itemNode2.SelectSingleNode("gml:posList", nsmgr);
                Console.WriteLine(flstNode.InnerText);
                list.Add(flstNode.InnerText);
            }
            /*List<string> flstgrenzen = CreateGeometry(doc);*/
            return list;
        }
        /*private static List<string> CreateGeometry(XmlDocument xmlDoc)
        {

        }*/
              
    }
}
