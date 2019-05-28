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


        public Solid CollectBuilding(XElement building, XYZ lowerCorner)
        {

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

            //Log.Information("Anzahl der boundedBy-Tags innerhalb Building " + boundedBy.Count());

            //var polys = from bound in bounds
            //            select bounds.Descendants(gml + "posList");

            //IEnumerable < XElement > polygons = xmlBuildingNode.Descendants(gml + "posList");

            Solid solid = new Solid();

            foreach(var polygon in boundedBy.Descendants(allns["gml"] + "posList"))
                //foreach(var polygon in building.Descendants(allns["gml"] + "posList"))
                {
                //Log.Information("Auslesen der Polygone innerhalb boundedBy");
                //Log.Information("Info: je nach CityGML-Datensatz können ein oder mehrere Flächen pro boundedBy definiert sein");

                List<XYZ> vertexList = new List<XYZ>();
                vertexList.AddRange(CollectPoints(polygon, lowerCorner));
                string guid = Guid.NewGuid().ToString();

                //Log.Debug("Anzahl Eckpunkte dieser Fläche = " + vertexList.Count);
                //Log.Debug("Fläche erhält intern die ID = " + guid);

                if(vertexList.Count < 4)
                {
                    Log.Error("Zu wenig Eckpunkte!");
                    throw new Exception("Zu wenig Eckpunkte!");
                }

                solid.AddPlane(guid, vertexList);

                //Log.Information("Ebene wurde Solid hinzugefügt!");
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

                //Log.Debug("Koordinate : " + x + "," + y + "," + z);
            }

            return polygonVertices;
        }
    }
}