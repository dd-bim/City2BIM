using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Serilog;
using static City2BIM.Prop;

namespace City2BIM.GetGeometry
{
    public class ReadGeomData
    {
        public ReadGeomData(Dictionary<string, XNamespace> allns)
        {
            this.allns = allns;
        }

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

            //List<XYZ> ptList = new List<XYZ>();

            //Auslesen der Polygone
            //------------------------

            Dictionary<XYZ, string> ptDictPoly = new Dictionary<XYZ, string>();

            foreach(var polygon in boundedBy.Descendants(allns["gml"] + "posList"))
            {
                var polyPts = CollectPoints(polygon, lowerCorner);

                //hier Logik implementieren, welche Fehler innerhalb der Polygon-Geometrie ausgibt und ggf. berichtigt

                //Prüfung - gleicher Start- und Endpunkt
                //-----------------------------------------
                //var checkPoly = SameStartAndEndPt(polyPts);

                //if(!checkPoly)
                //    Log.Error("Start- und Endpunkt sind nicht gleich!");

                //----------------------------------------------------------

                //Prüfung - keine redundanten Punkte (außer Start und End)
                //-----------------------------------------
                var checkRedun = NoRedundantPts(polyPts);

                if(!checkRedun)
                    Log.Error("Gleiche Punkte (außer Start- und Endpunkt) in Polygon vorhanden!");

                //----------------------------------------------------------

                string polyGuid = Guid.NewGuid().ToString();

                foreach(var pt in polyPts)
                {
                    ptDictPoly.Add(pt, polyGuid);       //Speicherung aller Punkte (XYZ) mit ID des zugehörigen Polygons
                }
            }

            Dictionary<XYZ, string> ptDictPolyF = new Dictionary<XYZ, string>();

            //Ermitteln der Punkte, welche innerhalb der Tolerant (siehe Prop-Klasse) weniger als 3 äquivalente Punkte besitzen
            //Ebenenschnitt benötigt mindestens 3 Ebenen, welche zum Vertex gehören
            //bei weniger als 3 Punkten werden Fehler in der CityGML-Geometrie, da weniger als 3 Ebenen für Solid-erstellung keinen Sinn ergeben (?!)
            //es werden die Punkte aller Polygone des Buildings zum Vergleich herangezogen

            foreach(var pt in ptDictPoly)
            {
                var redunCt = 0; //Zähler für redundante Punkte (innerhalb Toleranz)

                var ptList = ptDictPoly.Keys;

                foreach(var pt2 in ptList)
                {
                    if(pt.Key == pt2)       //selber Punkt wird nicht mitgezählt
                        continue;

                    double ptDist = XYZ.DistanceSq(pt.Key, pt2);

                    if(ptDist < Distolsq)
                    {
                        redunCt++;
                    }
                }

                if(redunCt < 2)
                {
                    //Log.Error("Fehler wohl bei " + pt.Key.X + " , " + pt.Key.Y + " , " + pt.Key.Z);

                    ptDictPolyF.Add(pt.Key, pt.Value);
                }
            }

            var testCt = ptDictPoly.Count;

            foreach(var pt in ptDictPolyF)
            {
                ptDictPoly.Remove(pt.Key);
            }

            if (ptDictPolyF.Count > 0)
                Log.Warning(ptDictPolyF.Count + " Punkte gelöscht. (In < 3 Ebenen vorhanden)");

            Solid solid = new Solid();

            var polyList = ptDictPoly.Values.Distinct();  //alle Guids der Polygone

            foreach(var polyID in polyList)
            {
                var points = from p in ptDictPoly
                             where p.Value.Equals(polyID)
                             select p.Key;

                //Log.Information("pointsPoly " + points.Count());

                solid.AddPlane(polyID, points.ToList());
            }

            //foreach(var polygon in boundedBy.Descendants(allns["gml"] + "posList"))
            ////foreach(var polygon in building.Descendants(allns["gml"] + "posList"))
            //{
            //    //Log.Information("Auslesen der Polygone innerhalb boundedBy");
            //    //Log.Information("Info: je nach CityGML-Datensatz können ein oder mehrere Flächen pro boundedBy definiert sein");

            //    List<XYZ> vertexList = new List<XYZ>();
            //    vertexList.AddRange(CollectPoints(polygon, lowerCorner));
            //    string guid = Guid.NewGuid().ToString();

            //    //Log.Debug("Anzahl Eckpunkte dieser Fläche = " + vertexList.Count);
            //    //Log.Debug("Fläche erhält intern die ID = " + guid);

            //    //if(vertexList.Count < 4)
            //    //{
            //    //    Log.Error("Zu wenig Eckpunkte!");
            //    //    Log.Error("Polygon falsch generiert. Anzahl Eckpunkte = " + vertexList.Count );

            //    //    //throw new Exception("Zu wenig Eckpunkte!");
            //    //}

            //    // Log.Information("Start Auflösen redundanter Punkte");

            //    solid.AddPlane(guid, vertexList);

            //    //Log.Information("Ebene wurde Solid hinzugefügt!");
            //}

            //foreach Polygon

            // 3.2. Erzeuge Semantik für jedes Gebäud

            solid.CalculatePositions();

            //solid.RemoveWrongVertices();

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

            polygonVertices.Remove(polygonVertices.Last());

            return polygonVertices;
        }

        private bool SameStartAndEndPt(List<XYZ> polygon)
        {
            bool decision = true;

            var start = polygon.First();
            var end = polygon.Last();

            if(start.X != end.X || start.Y != end.Y || start.Z != end.Z)
                decision = false;

            return decision;
        }

        private bool NoRedundantPts(List<XYZ> polygon)
        {
            foreach(var pt in polygon)
            {
                var samePts = from p in polygon
                              where (pt != p && pt.X == p.X && pt.Y == p.Y && pt.Z == p.Z)
                              select p;

                //Log.Warning("SamePoints " + samePts.Count());

                if(pt == polygon.First() && samePts.Count() > 1)
                    return false;

                if(pt == polygon.Last() && samePts.Count() > 1)
                    return false;

                if(pt != polygon.First() && pt != polygon.Last() && samePts.Count() > 0)
                    return false;
            }

            return true;
        }
    }
}