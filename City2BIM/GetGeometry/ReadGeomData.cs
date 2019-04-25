using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Serilog;

namespace City2BIM.GetGeometry
{
    public class ReadGeomData
    {
        public ReadGeomData(Dictionary<string, XNamespace> allns) { this.allns = allns; }
        
        
        //private static XNamespace core = "http://www.opengis.net/citygml/1.0";
        //private static XNamespace bldg = "http://www.opengis.net/citygml/building/1.0";
        //private static XNamespace gml = "http://www.opengis.net/gml";

        private Dictionary<string, XNamespace> allns;

        public PlugIn PlugIn
        {
            get => default(PlugIn);
            set
            {
            }
        }                                                             //nötig? evtl. Verknüpfung zu "ReadCityGML.cs"

        //public List<Solid> ReadGeometryFromXML()
        //{
        //    var path = "D:\\1_CityBIM\\1_Programmierung\\City2BIM\\CityGML_Data\\CityGML_Data\\Berlin\\input_clean_3.gml";
        //    //var path = "D:\\1_CityBIM\\1_Programmierung\\City2BIM\\CityGML_Data\\CityGML_Data\\Erfurt\\LoD2_642_5648_2_TH\\LoD2_642_5648_2_TH.gml";
        //    //var path = "D:\\1_CityBIM\\1_Programmierung\\City2BIM\\CityGML_Data\\CityGML_Data\\Dresden\\Gebaeude_LOD1_citygml.gml";
        //    //var path = "D:\\1_CityBIM\\1_Programmierung\\City2BIM\\CityGML_Data\\CityGML_Data\\Dresden\\Gebaeude_LOD2_citygml.gml";
        //    //var path = "D:\\1_CityBIM\\1_Programmierung\\City2BIM\\CityGML_Data\\CityGML_Data\\Dresden\\Gebaeude_LOD3_citygml.gml";
        //    //var path = "D:\\1_CityBIM\\1_Programmierung\\City2BIM\\CityGML_Data\\CityGML_Data\\Dresden\\Vegetation.gml";
        //    //var path = "D:\\1_CityBIM\\1_Programmierung\\City2BIM\\CityGML_Data\\CityGML_Data\\Einzelhaus.gml";

        //    XDocument doc = XDocument.Load(path);

        //    Log.Information("file: " + path);

        //    Log.Information("ReadGeometryFromXml Methode... (speichert bldgs in Solid-List");

        //    this.allns = doc.Root.Attributes().
        //            Where(a => a.IsNamespaceDeclaration).
        //            GroupBy(a => a.Name.Namespace == XNamespace.None ? String.Empty : a.Name.LocalName,
        //            a => XNamespace.Get(a.Value)).
        //            ToDictionary(g => g.Key,
        //                 g => g.First());

        //    List<Solid> buildings = CreateGeometry(doc);

        //    Log.Debug("Buildings, Anzahl = " + buildings.Count);

        //    return buildings;
        //}

        //private void readOffset(ref Double x, ref Double y, ref Double z, XElement e)
        //{
        //    var pointSeperated = e.Value.Split(new[] { (' ') }, StringSplitOptions.RemoveEmptyEntries);
        //    x = Double.Parse(pointSeperated[0], CultureInfo.InvariantCulture);
        //    y = Double.Parse(pointSeperated[1], CultureInfo.InvariantCulture);
        //    z = Double.Parse(pointSeperated[2], CultureInfo.InvariantCulture);
        //}

        //private List<Solid> CreateGeometry(XDocument xmlDoc)
        //{
            //Log.Information("CreateGeometry Methode... (erzeugt Solid-List für bldgs");

            ////in Datensatz Berlin problematisch (mehrere LowerCorner in verschachtelten Objekten --> es darf nur die Projekt corner gelesen werden)

            ////Später als LocalPlacement in Building-Class
            //double offsetX = 0.0;
            //double offsetY = 0.0;
            //double offsetZ = 0.0;

            //var gml = this.allns["gml"];

            //// 1. Find LowerCorner (Global oder Lokal?)
            //IEnumerable<XElement> pointLowerCorner = xmlDoc.Descendants(gml + "lowerCorner");
            //int clc = pointLowerCorner.Count();
            //switch(clc)
            //{
            //    case 0:
            //        Log.Information("keine lower Corner");
            //        break;

            //    case 1:
            //        readOffset(ref offsetX, ref offsetY, ref offsetZ, pointLowerCorner.First());
            //        Log.Information(String.Format("Eine lower Corner: {0,5:N3} {1,5:N3} {2,5:N3}  ", offsetX, offsetY, offsetZ));
            //        break;

            //    default:
            //        Log.Information("Anzahl lower Corner: " + clc);
            //        break;
            //}

            //XYZ lowerCorner = new XYZ(offsetX, offsetY, offsetZ);

            //foreach(XElement lc in pointLowerCorner)
            //{
            //    string lowercorner = lc.Value;
            //    var pointSeperated = lowercorner.Split(new[] { (' ') }, StringSplitOptions.RemoveEmptyEntries);
            //    double x = Double.Parse(pointSeperated[0], CultureInfo.InvariantCulture);
            //    double y = Double.Parse(pointSeperated[1], CultureInfo.InvariantCulture);
            //    double z = Double.Parse(pointSeperated[2], CultureInfo.InvariantCulture);
            //    lowerCorner.X = x;
            //    lowerCorner.Y = y;
            //    lowerCorner.Z = z;

            //}

            //Log.Debug("Detected LowerCorner = " + lowerCorner.X + " " + lowerCorner.Y + " " + lowerCorner.Z);

            ////XNamespace nsbldg = this.allns["bldg"];
            //Log.Information("Namespace Building: " + this.allns["bldg"].NamespaceName);

            //var xmlBuildings = xmlDoc.Descendants(this.allns["bldg"] + "Building");
            //int cb = xmlBuildings.Count();
            //Log.Information(@"Anzahl Gebäude: " + cb);

            //GetBldgSemantics(xmlBuildings.First());

            //List<Solid> solidList = new List<Solid>();

            //foreach(XElement xmlBuildingNode in xmlBuildings)
            //{
            //    solidList.Add(CollectBuilding(xmlBuildingNode, lowerCorner));
            //} //foreach building

            //Log.Debug("Start der bldg suche..");

            //foreach(XElement building in buildings)
            //{
            //    Log.Debug("new bldg calculation...Übergabe bldg, lowercorner");

            //    solidList.Add(CollectBuilding(building, lowerCorner));
            //}

        //    return solidList;
        //}



        public Solid CollectBuilding(XElement building, XYZ lowerCorner)
        {
            Log.Debug("collectbuilding-methode...");

            // 3. Erzeuge Volumenkörüer für jedes Gebäude
            // Objekt, offset, id für jedes Gebäude

            //var gml = this.allns["gml"];

            //string gmlid = building.Attribute(gml + "id").Value;

            //var xmlLc = building.Descendants(gml + "lowerCorner");

            //if(xmlLc.Count() > 0)
            //{
            //    readOffset(ref lowerCorner.X, ref lowerCorner.Y, ref lowerCorner.Z, xmlLc.First());
            //}

            //Log.Information(String.Format("LR Corner Gebäude {0,17}: {1,5:N3} {2,5:N3} {3,5:N3}  ", gmlid, lowerCorner.X, lowerCorner.Y, lowerCorner.Z));

            // 3.1 Sammle alle Polygone // Falscher Ansatz. Neheme bldg.bounded

            var boundedBy = building.Descendants(this.allns["bldg"] + "boundedBy");

            //var polygons = building.Descendants(this.allns["bldg"] + "boundedBy").Select(b => b.Descendants(gml + "posList"));

            Log.Information("Anzahl der boundedBy-Tags innerhalb Building " + boundedBy.Count());

            //var polys = from bound in bounds
            //            select bounds.Descendants(gml + "posList");

            //IEnumerable < XElement > polygons = xmlBuildingNode.Descendants(gml + "posList");

            Solid solid = new Solid();

            foreach(var polygon in boundedBy.Descendants(allns["gml"] + "posList"))
            {
                Log.Information("Auslesen der Polygone innerhalb boundedBy");
                Log.Information("Info: je nach CityGML-Datensatz können ein oder mehrere Flächen pro boundedBy definiert sein");

                List<XYZ> vertexList = new List<XYZ>();
                vertexList.AddRange(CollectPoints(polygon, lowerCorner));
                string guid = Guid.NewGuid().ToString();

                Log.Debug("Anzahl Eckpunkte dieser Fläche = " + vertexList.Count);
                Log.Debug("Fläche erhält intern die ID = " + guid);

                if(vertexList.Count < 4)
                {
                    Log.Error("Zu wenig Eckpunkte!");
                    throw new Exception("Zu wenig Eckpunkte!");
                }

                solid.AddPlane(guid, vertexList);

                Log.Information("Ebene wurde Solid hinzugefügt!");
            }

            //foreach Polygon

            // 3.2. Erzeuge Semantik für jedes Gebäud

            solid.CalculatePositions();

            foreach(Vertex v in solid.Vertices)
            {
                v.Position = v.Position + lowerCorner;
            }

            return solid;
        }

        private List<XYZ> CollectPoints(XElement points, XYZ lowerCorner)
        {
            string pointString = points.Value;
            var pointsSeperated = pointString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var polygonVertices = new List<XYZ>();

            for(int i = 0; i < pointsSeperated.Length; i = i + 3)
            {
                double x = Double.Parse(pointsSeperated[i], CultureInfo.InvariantCulture) - lowerCorner.X;
                double y = Double.Parse(pointsSeperated[i + 1], CultureInfo.InvariantCulture) - lowerCorner.Y;
                double z = Double.Parse(pointsSeperated[i + 2], CultureInfo.InvariantCulture) - lowerCorner.Z;
                polygonVertices.Add(new XYZ(x, y, z));

                Log.Debug("Koordinate : " + x + "," + y + "," + z);
            }

            return polygonVertices;
        }
    }
}