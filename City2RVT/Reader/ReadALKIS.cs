using Autodesk.Revit.DB;
using City2BIM.Alkis;
using City2BIM.Geometry;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using City2BIM;

namespace City2RVT.Reader
{
    public class ReadALKIS
    {
        public Dictionary<string, XNamespace> allns;

        public ReadALKIS(Document doc)
        {
            //local file path
            string path = GUI.Prop_NAS_settings.FileUrl;

            //read all parcelTypes objects
            var alkisRep = new AlkisReader(path);


            var semBuilder = new Builder.Revit_Semantic(doc);
            semBuilder.CreateParameters(Alkis_Semantic.GetParcelAttributes());

            var geomBuilder = new Builder.RevitAlkisBuilder(doc);
            geomBuilder.CreateTopo(alkisRep.AlkisObjects);
        }

        private List<C2BPoint[]> ReadSegments(XElement surfaceExt)
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

        private List<List<C2BPoint[]>> ReadInnerSegments(List<XElement> surfaceInt)
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

        private List<C2BPoint[]> ReadLineString(XElement posList)
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

        public List<string> parcelTypes = new List<string>
        {
            "AX_Flurstueck"
        };

        //may enhance with "AX_Bauteil" --> check before which kind of geometry is transfered (possible overlapping with buildings?)
        public List<string> buildingTypes = new List<string>
        {
            "AX_Gebaeude"
        };

        public List<string> usageTypes = new List<string>
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
