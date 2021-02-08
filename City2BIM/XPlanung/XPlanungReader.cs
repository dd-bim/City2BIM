using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Newtonsoft.Json;

using City2BIM.Geometry;
using City2BIM.Properties;


namespace City2BIM.XPlanung
{
    public class XPlanungReader
    {
        private static Dictionary<string, XNamespace> allns;
        private List<XPlanungObject> xplanungobjects;
        public List<XPlanungObject> XPlanungObjects { get => xplanungobjects; }

        private XDocument xDoc { get; set; }
        private Dictionary<string, List<string>> XPlanSchemaDict;

        public XPlanungReader(XDocument xDoc)
        {
            xplanungobjects = new List<XPlanungObject>();
            this.xDoc = xDoc;
            allns = xDoc.Root.Attributes().
                    Where(a => a.IsNamespaceDeclaration).
                    GroupBy(a => a.Name.Namespace == XNamespace.None ? String.Empty : a.Name.LocalName, a => XNamespace.Get(a.Value)).
                    ToDictionary(g => g.Key, g => g.First());
            this.XPlanSchemaDict = getXPlanSchemaDict();
        }

        public void readData()
        {
            this.readBPBereich();
            this.readBPPlan();
            this.readOtherObjects();
        }

        private void readBPBereich()
        {
            var xmlObjType = xDoc.Descendants(allns["xplan"] + "BP_Bereich");

            foreach(XElement BPElement in xmlObjType)
            {
                XPlanungObject xObj = new XPlanungObject();
                xObj.Gmlid = BPElement.Attribute(allns["gml"] + "id").Value;
                xObj.UsageType = "BP_Bereich";
                xObj.Group = XPlanungObject.XGruop.BP_Bereich;
                xObj.Geom = XPlanungObject.geomType.Polygon;
                xObj.Attributes = readAttributes(BPElement, this.XPlanSchemaDict);

                XElement extSeg = BPElement.Descendants(allns["gml"] + "exterior").SingleOrDefault();
                xObj.Segments = readSegments(extSeg);

                List<XElement> intSeg = BPElement.Descendants(allns["gml"] + "interior").ToList();
                if (intSeg.Any())
                    xObj.InnerSegments = ReadInnerSegments(intSeg);

                this.XPlanungObjects.Add(xObj);
            }
        }

        private void readBPPlan()
        {
            var xmlObjType = xDoc.Descendants(allns["xplan"] + "BP_Plan");

            foreach (XElement BPElement in xmlObjType)
            {
                XPlanungObject xObj = new XPlanungObject();
                xObj.Gmlid = BPElement.Attribute(allns["gml"] + "id").Value;
                xObj.UsageType = "BP_Plan";
                xObj.Group = XPlanungObject.XGruop.BP_Plan;
                xObj.Geom = XPlanungObject.geomType.Polygon;
                xObj.Attributes = readAttributes(BPElement, this.XPlanSchemaDict);

                XElement extSeg = BPElement.Descendants(allns["gml"] + "exterior").SingleOrDefault();
                xObj.Segments = readSegments(extSeg);

                List<XElement> intSeg = BPElement.Descendants(allns["gml"] + "interior").ToList();
                if (intSeg.Any())
                    xObj.InnerSegments = ReadInnerSegments(intSeg);

                this.XPlanungObjects.Add(xObj);
            }
        }

        private void readOtherObjects()
        {
            var xmlObjType = xDoc.Descendants(allns["gml"] + "featureMember");

            List<XElement> xelemList = new List<XElement>();

            foreach(var featMemb in xmlObjType)
            {
                var XPlanElem = featMemb.Elements().First();
                
                if (!XPlanElem.Name.LocalName.Equals("BP_Bereich") && !XPlanElem.Name.LocalName.Equals("BP_Plan") && XPlanElem.Descendants(allns["xplan"] + "position").Any())
                {
                    XPlanungObject xObj = new XPlanungObject();
                    xObj.Gmlid = XPlanElem.Attribute(allns["gml"] + "id").Value;
                    xObj.UsageType = XPlanElem.Name.LocalName;
                    xObj.Group = XPlanungObject.XGruop.other;
                    xObj.Attributes = readAttributes(XPlanElem, this.XPlanSchemaDict);

                    var position = XPlanElem.Descendants(allns["xplan"] + "position").First();

                    var geomType = position.Elements().First().Name.LocalName;

                    switch (geomType)
                    {
                        case "Polygon":
                            XElement extSeg = XPlanElem.Descendants(allns["gml"] + "exterior").SingleOrDefault();
                            xObj.Segments = readSegments(extSeg);

                            List<XElement> intSeg = XPlanElem.Descendants(allns["gml"] + "interior").ToList();
                            if (intSeg.Any())
                                xObj.InnerSegments = ReadInnerSegments(intSeg);
                            xObj.Geom = XPlanungObject.geomType.Polygon;
                            break;

                        case "LineString":
                            List<C2BPoint[]> coords = ReadLineString(position.Descendants(allns["gml"] + "posList").First());
                            xObj.Segments = coords;
                            xObj.Geom = XPlanungObject.geomType.LineString;
                            break;

                        case "Curve":
                            var lineStringSegments = XPlanElem.Descendants(allns["gml"] + "posList").ToList();
                            List<C2BPoint[]> coordsCurve = new List<C2BPoint[]>();
                            
                            foreach( var seg in lineStringSegments)
                            {
                                coordsCurve.AddRange(ReadLineString(seg));
                            }
                            
                            xObj.Segments = coordsCurve;
                            xObj.Geom = XPlanungObject.geomType.Curve;
                            break;


                        default:
                            continue;
                    }

                    this.XPlanungObjects.Add(xObj);
                }
            }

        }

        private static void readPosition(XElement position)
        {
            var geomType = position.Elements().First().Name.LocalName;

            

            Console.WriteLine(geomType);
        }

        private static List<C2BPoint[]> readSegments(XElement seg)
        {
            List<C2BPoint[]> segments = new List<C2BPoint[]>();
            var posLists = seg.Descendants(allns["gml"] + "posList");

            foreach (XElement posList in posLists)
            {
                var line = ReadLineString(posList);

                segments.AddRange(line);
            }
            return segments;
        }

        private static List<C2BPoint[]> ReadLineString(XElement posList)
        {
            List<C2BPoint[]> segments = new List<C2BPoint[]>();

            var coords = posList.Value;
            string[] coord = coords.Split(' ');

            for (var c = 0; c < coord.Length - 3; c += 2)
            {
                C2BPoint start = new C2BPoint(double.Parse(coord[c], CultureInfo.InvariantCulture), double.Parse(coord[c + 1], CultureInfo.InvariantCulture), 0.0);
                C2BPoint end = new C2BPoint(double.Parse(coord[c + 2], CultureInfo.InvariantCulture), double.Parse(coord[c + 3], CultureInfo.InvariantCulture), 0.0);

                segments.Add(new C2BPoint[] { start, end });
            }
            return segments;
        }

        private static List<List<C2BPoint[]>> ReadInnerSegments(List<XElement> surfaceInt)
        {
            List<List<C2BPoint[]>> innerSegments = new List<List<C2BPoint[]>>();

            foreach (var interior in surfaceInt)
            {
                List<C2BPoint[]> segments = new List<C2BPoint[]>();

                var posLists = interior.Descendants(allns["gml"] + "posList");

                foreach (XElement posList in posLists)
                {
                    var line = ReadLineString(posList);

                    segments.AddRange(line);
                }
                innerSegments.Add(segments);
            }
            return innerSegments;
        }

        private static Dictionary<string,string> readAttributes(XElement XPlanElem, Dictionary<string, List<string>> XPlanSchemaDict)
        {
            var XPlanObjType = XPlanElem.Name.LocalName;

            List<string> attrList = XPlanSchemaDict[XPlanObjType];

            Dictionary<string, string> attributes = new Dictionary<string, string>();

            foreach(string attrName in attrList)
            {
                var node = XPlanElem.Descendants(allns["xplan"] + attrName).ToList();
                if (node.Count == 1)
                {
                    var value = node.FirstOrDefault().Value;
                    attributes.Add(attrName, value);
                }
            }

            return attributes;
        }
        private static Dictionary<string, List<string>> getXPlanSchemaDict()
        {
            var jsonString = Resources.xplan;
            dynamic result = JsonConvert.DeserializeObject(jsonString);

            var schemaDict = new Dictionary<String, List<String>>();

            foreach (var obj in result.meta)
            {
                var key = obj.name.Value;
                var values = new List<String>();

                foreach (var prop in obj.properties)
                {
                    values.Add(prop.name.Value);
                }

                schemaDict.Add(key, values);
            }

            return schemaDict;
        }
    }
}
