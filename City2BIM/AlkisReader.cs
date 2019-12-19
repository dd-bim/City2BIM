using City2BIM.Geometry;
using City2BIM.Alkis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace City2BIM
{
    public class AlkisReader
    {
        private static Dictionary<string, XNamespace> allns;
        private List<AX_Object> alkisObjects;

        public List<AX_Object> AlkisObjects { get => alkisObjects; }

        public AlkisReader(string path)
        {
            XDocument xDoc = XDocument.Load(path);

            allns = xDoc.Root.Attributes().
                    Where(a => a.IsNamespaceDeclaration).
                    GroupBy(a => a.Name.Namespace == XNamespace.None ? String.Empty : a.Name.LocalName, a => XNamespace.Get(a.Value)).
                    ToDictionary(g => g.Key, g => g.First());

            //read all parcelTypes objects
            List<AX_Object> axObjects = new List<AX_Object>();

            foreach (string axObject in parcelTypes)
            {
                var xmlObjType = xDoc.Descendants(allns[""] + axObject);

                foreach (XElement xmlObj in xmlObjType)
                {
                    AX_Object axObj = new AX_Object
                    {
                        UsageType = axObject
                    };

                    XElement extSeg = xmlObj.Descendants(allns["gml"] + "exterior").SingleOrDefault();
                    axObj.Segments = ReadSegments(extSeg);

                    List<XElement> intSeg = xmlObj.Descendants(allns["gml"] + "interior").ToList();
                    if (intSeg.Any())
                        axObj.InnerSegments = ReadInnerSegments(intSeg);

                    axObj.Group = AX_Object.AXGroup.parcel;
                    axObj.Attributes = new Alkis_Sem_Reader(xDoc, allns).ReadAttributeValuesParcel(xmlObj, Alkis_Semantic.GetParcelAttributes());

                    axObjects.Add(axObj);
                }
            }

            //---------------

            //read all buildingTypes objects

            foreach (string axObject in buildingTypes)
            {
                var xmlObjType = xDoc.Descendants(allns[""] + axObject);

                foreach (XElement xmlObj in xmlObjType)
                {
                    AX_Object axObj = new AX_Object
                    {
                        UsageType = axObject
                    };

                    XElement extSeg = xmlObj.Descendants(allns["gml"] + "exterior").SingleOrDefault();
                    axObj.Segments = ReadSegments(extSeg);

                    List<XElement> intSeg = xmlObj.Descendants(allns["gml"] + "interior").ToList();
                    if (intSeg.Any())
                        axObj.InnerSegments = ReadInnerSegments(intSeg);

                    axObj.Group = AX_Object.AXGroup.building;

                    axObjects.Add(axObj);
                }
            }

            //---------------

            //read all usageTypes objects

            foreach (string axObject in usageTypes)
            {
                var xmlObjType = xDoc.Descendants(allns[""] + axObject);

                foreach (XElement xmlObj in xmlObjType)
                {
                    AX_Object axObj = new AX_Object
                    {
                        UsageType = axObject
                    };

                    XElement extSeg = xmlObj.Descendants(allns["gml"] + "exterior").SingleOrDefault();
                    axObj.Segments = ReadSegments(extSeg);

                    List<XElement> intSeg = xmlObj.Descendants(allns["gml"] + "interior").ToList();
                    if (intSeg.Any())
                        axObj.InnerSegments = ReadInnerSegments(intSeg);

                    axObj.Group = AX_Object.AXGroup.usage;

                    axObjects.Add(axObj);
                }
            }
            this.alkisObjects = axObjects;

        }

        private static List<C2BPoint[]> ReadSegments(XElement surfaceExt)
        {
            List<C2BPoint[]> segments = new List<C2BPoint[]>();

            var posLists = surfaceExt.Descendants(allns["gml"] + "posList");

            foreach (XElement posList in posLists)
            {
                var line = ReadLineString(posList);

                segments.AddRange(line);
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

        private static List<string> parcelTypes = new List<string>
        {
            "AX_Flurstueck"
        };

        //may enhance with "AX_Bauteil" --> check before which kind of geometry is transfered (possible overlapping with buildings?)
        private static List<string> buildingTypes = new List<string>
        {
            "AX_Gebaeude"
        };

        private static List<string> usageTypes = new List<string>
        {
            //group "Siedlung"
            "AX_Wohnbauflaeche",
            "AX_IndustrieUndGewerbeflaeche",
            "AX_Halde",
            "AX_Bergbaubetrieb",
            "AX_TagebauGrubeSteinbruch",
            "AX_FlaecheGemischterNutzung",
            "AX_FlaecheBesondererFunktionalerPraegung",
            "AX_SportFreizeitUndErholungsflaeche",
            "AX_Friedhof",

            //group "Verkehr"
            "AX_Strassenverkehr",
            "AX_Weg",
            "AX_Platz",
            "AX_Bahnverkehr",
            "AX_Flugverkehr",
            "AX_Schiffsverkehr",

            //group "Vegetation"
            "AX_Landwirtschaft",
            "AX_Wald",
            "AX_Gehoelz",
            "AX_Heide",
            "AX_Moor",
            "AX_Sumpf",
            "AX_UnlandVegetationsloseFlaeche",

            //group "Gewaesser"
            "AX_Fliessgewaesser",
            "AX_Hafenbecken",
            "AX_StehendesGewaesser",
            "AX_Meer"
        };
    }
}
