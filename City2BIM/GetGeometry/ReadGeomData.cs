using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Serilog;
using static City2BIM.Prop;

namespace City2BIM.GetGeometry
{
    /// <summary>
    ///
    /// </summary>
    public class ReadGeomData
    {
        public ReadGeomData(Dictionary<string, XNamespace> allns, DxfVisualizer dxf)
        {
            this.allns = allns;
            this.dxf = dxf;
        }

        //private static XNamespace core = "http://www.opengis.net/citygml/1.0";
        //private static XNamespace bldg = "http://www.opengis.net/citygml/building/1.0";
        //private static XNamespace gml = "http://www.opengis.net/gml";

        private Dictionary<string, XNamespace> allns;
        private DxfVisualizer dxf;

        public PlugIn PlugIn
        {
            get => default(PlugIn);
            set
            {
            }
        }                                                             //nötig? evtl. Verknüpfung zu "ReadCityGML.cs"

        /// <summary>
        /// Main method for Solid calculation
        /// </summary>
        /// <param name="building">CityGML element for Building</param>
        /// <param name="lowerCorner">XYZ coordinate of lowerCorner</param>
        /// <returns>Solid representation for Building(part)</returns>
        public Solid CollectBuilding(XElement building, XYZ lowerCorner)
        {
            //alle bldg:boundedBy-Elemente pro Building(part)
            //bei LOD1: lod1Solid bzw lod1Surfaces
            //bldg:boundedBy enthält in der Regel eine Polygonfläche (zB. TH), kann aber auch mehrere Flächen pro Wandtyp enthalten (zB. Berlin)
            var boundedBy = building.Elements(this.allns["bldg"] + "boundedBy").ToList();   //nur LOD2

            if(boundedBy.Count == 0)        //für LOD1-Fälle
            {
                boundedBy = building.Elements(this.allns["bldg"] + "lod1Solid").ToList();       //LOD1 mit Solids (AdV-Standard)

                if(boundedBy.Count == 0)
                {
                    boundedBy = building.Elements(this.allns["bldg"] + "lod1MultiSurface").ToList();       //LOD1 mit MultiSurfaces (nicht empfohlen, aber zB in DD)
                }
            }

            var polyList = new Dictionary<string[], List<XYZ>>(); //Dictionary for all pos in polygon and FaceType (Wall, Roof, Ground, Closure)

            foreach(var bound in boundedBy)
            {
                var polyType = "";          //leer initialisiert, falls keine Flächenart gespeichert (wie bspw bei LOD1)
                var i = 0;

                polyType = bound.Elements().First().Name.LocalName; //erstes Kindelement von boundedBy enthält FaceType

                var posLists = bound.Descendants(allns["gml"] + "posList").ToList();  //alle posLists innerhalb boundedBy

                foreach(var posL in posLists)
                {
                    i++;

                    var polyPts = CollectPoints(posL, lowerCorner); //Speichern der Polygonpunkte, reduziert um lowerCorner in PointList

                    string ringType = "exterior";

                    if((posL.Ancestors(allns["gml"] + "interior").Count() > 0))
                        ringType = "interior";

                    var polygonID = posL.AncestorsAndSelf().Attributes(allns["gml"] + "id").First();

                    string[] polyIdType = new string[] { polygonID.ToString(), polyType, ringType };

                    if(polyList.ContainsKey(polyIdType))                //Fall tritt ein, wenn Polygone nirgends ID besitzen, sondern nur umfassendes Bldg
                        polyIdType = new string[] { polygonID.ToString() + "_" + i, polyType, ringType };

                    //jedes Polygon wird als Punktliste zusammen mit ID und verkettetem faceType in Dictionary geschrieben
                    polyList.Add(polyIdType, polyPts);
                }

                if(posLists.Count == 0)            //wenn Punkte nicht in posList gespeichert sind, dann wahrscheinlich als einzelne pos tags mit XYZ
                {
                    var rings = bound.Descendants(allns["gml"] + "LinearRing");  //alle posLists innerhalb boundedBy

                    var j = 0; //RingCounter: für ID, wenn Polygone keine eigene ID besitzen (zB LOD1)

                    foreach(var ring in rings)
                    {
                        j++;

                        var positions = ring.Descendants(allns["gml"] + "pos");  //alle posLists innerhalb boundedBy

                        var posList = new List<XYZ>();

                        foreach(var pos in positions)
                        {
                            var polyPt = CollectPoint(pos, lowerCorner);     //Speichern der Polygonpunkte, reduziert um lowerCorner in PointList
                            posList.Add(polyPt);
                        }

                        var polygonID = ring.AncestorsAndSelf().Attributes(allns["gml"] + "id").First();

                        string ringType = "exterior";

                        if((ring.Ancestors(allns["gml"] + "interior").Count() > 0))
                            ringType = "interior";

                        string[] polyIdType = new string[] { polygonID.ToString(), polyType, ringType };

                        if(polyList.ContainsKey(polyIdType))                //Fall tritt ein, wenn Polygone nirgends ID besitzen, sondern nur umfassendes Bldg
                            polyIdType = new string[] { polygonID.ToString() + "_" + j, polyType, ringType };    //ID besteht aus Bldg_ID + Zähler pro Ring (Polygon)

                        polyList.Add(polyIdType, posList);
                    }
                }
            }

            //Auslesen der Polygone
            //------------------------

            Dictionary<XYZ, string[]> ptDictPoly = new Dictionary<XYZ, string[]>(); //Dictionary für Polygonpunkt und generierte PolygonID

            foreach(var polyPts in polyList)        //Auslesen aller Polygone in Schleife
            {
                //Prüfung - gleicher Start - und Endpunkt
                // ---------------------------------------- -
                var checkPoly = SameStartAndEndPt(polyPts.Value);

                if(!checkPoly)
                    Log.Error("Start- und Endpunkt sind nicht gleich!");
                else
                    polyPts.Value.Remove(polyPts.Value.Last());     //Entfernen des letzten Punktes (=Start)

                //----------------------------------------------------------

                //Prüfung - keine redundanten Punkte (außer Start und End)
                //-----------------------------------------
                var checkRedun = NoRedundantPts(polyPts.Value);

                if(!checkRedun)
                    Log.Error("Gleiche Punkte (außer Start- und Endpunkt) in Polygon vorhanden!");
                //----------------------------------------------------------

                foreach(var pt in polyPts.Value)
                {
                    ptDictPoly.Add(pt, polyPts.Key);       //Speicherung jedes Punktes (XYZ) mit ID des zugehörigen Polygons
                }
            }

            //Ermitteln der Punkte, welche innerhalb der Toleranz (siehe Prop-Klasse) weniger als 3 äquivalente Punkte besitzen
            //Ebenenschnitt benötigt mindestens 3 Ebenen, welche zum Vertex gehören
            //bei weniger als 3 Punkten werden Fehler in der CityGML-Geometrie, da weniger als 3 Ebenen für Solid-erstellung keinen Sinn ergeben (?!)
            //es werden die Punkte aller Polygone des Buildings zum Vergleich herangezogen

            //alle XYZ-Koordinaten der zum Gebäude gehörenden Polygone, geordnet nach X,Y,Z (zur schnelleren Suche in for-Schleife)
            var bldgXYZ = ptDictPoly.Keys.OrderBy(x => x.X).OrderBy(y => y.Y).OrderBy(z => z.Z).ToList();

            var bldgXYZF = new List<XYZ>();    //temporäre Liste für vermutlich falsche Punkte (1 oder 2 Polygone)

            var redCt = 0;  //Schleifenvariable (=0 für Start)

            for(int i = 0; i < bldgXYZ.Count; i += redCt)   //Schleife zur Suche identischer Punkte (innerhalb Toleranz, Hochzählen um Anzahl ident. Pkt.)
            {
                //var polyID = (from p in ptDictPoly
                //              where p.Key == bldgXYZ[i]
                //              select p.Value).Single();

                int locRedCt = 0;                   //interner Zähler für redundante Punkte
                var redPts = new List<XYZ>();       //Initialsierung für Liste ident. Punkte pro Durchlauf

                redCt = 1;                          //Punkt i zählt mit

                for(int j = i + 1; j < bldgXYZ.Count; j++)      //interne Schleife (j jeweils i+1, nächster Punkt in Liste)
                //foreach(var xyzOther in bldgXYZ)
                {
                    if(bldgXYZ[i] == bldgXYZ[j])        //zur Sicherheit
                        continue;

                    double ptDist = XYZ.DistanceSq(bldgXYZ[i], bldgXYZ[j]);     //Distanzberechnung

                    if(ptDist < Distolsq)
                    {
                        //wenn kleiner als Toleranz (siehe Prop-Klasse) ist der Punkt redundant
                        locRedCt++;
                        redCt++;

                        redPts.Add(bldgXYZ[j]);
                    }
                    else
                    {
                        break;          //Abbruch, wenn kein "identischer" Punkt mehr gefunden wird (durch geordnete Liste wird keiner vergessen)
                    }
                }

                if(locRedCt < 2)        //gewünschter Fall: pro Vertex sind 3 Punkte in CityGML enthalten
                {
                    bldgXYZF.Add(bldgXYZ[i]);      //bei weniger als 3 (kein Ebenenschnitt möglich) -> Speichern in temp. Liste
                    bldgXYZF.AddRange(redPts);
                }
            }

            foreach(var ptF in bldgXYZF)
            {
                ptDictPoly.Remove(ptF);  //jeder vermutlich falsche Punkt wird aus Punktliste gelöscht

                ptDictPoly.TryGetValue(ptF, out string[] id);

                var ptX = ptF.X + lowerCorner.X;
                var ptY = ptF.Y + lowerCorner.Y;
                var ptZ = ptF.Z + lowerCorner.Z;

                Log.Information("Deleted Pt: " + ptX + " , " + ptY + " , " + ptZ + " at " + id);

                dxf.DrawPoint(ptF.X + lowerCorner.X, ptF.Y + lowerCorner.Y, ptF.Z + lowerCorner.Z, "cityGMLremovedPts", new int[] { 255, 0, 0 });
            }

            if(bldgXYZF.Count > 0)
                Log.Warning(bldgXYZF.Count + " Punkte gelöscht. (In < 3 Ebenen vorhanden)");

            Solid solid = new Solid();      //Anlegen eines Solids pro Gebäude(teil)

            var polyL = ptDictPoly.Values.Distinct();  //Distinct: Zusammenfassen aller gleichen PolygonIDs zu einer ID-Liste

            foreach(var polyID in polyL)
            {
                var points = from p in ptDictPoly
                             where p.Value.Equals(polyID)
                             select p.Key;                  //Selektieren aller Punkte pro Polygon-ID
                try
                {
                    solid.AddPlane(polyID[0], polyID[1], polyID[2], points.ToList());    //Aufruf der AddPlane-Methode, Übergabe von Polygon-ID und zugehörigen Punkten
                }
                catch(Exception ex)
                {
                    var exs = ex.Message;
                    var esd = ex.StackTrace;
                    continue; }
            }

            //-------------------------------------------------------------------------------------------------------------

            //alte Implementierung (wird evtl. später wieder genutzt, falls obige Suche verlagert wird):

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

            //-------------------------------------------------------------------------------------------------------------

            //Aufruf der Methode zur Bildung der Ebenenschnitte
            //solid enthält bis jetzt Vertex-Liste mit zugehörigen Ebenen ohne neu berechnete Eckpunkte (vertices)
            //nach Methode werden die Koordinaten der Vertices mit neuen Koordinaten aus Ebenenschnitt überschrieben/verbessert/ausgeglichen
            solid.CalculatePositions();

            foreach(Vertex v in solid.Vertices)
            {
                v.Position = v.Position + lowerCorner;          //für jeden Vertex wird lowerCorner wieder addiert
            }

            return solid;
        }

        /// <summary>
        /// Splitting of PosList for XYZ coordinates
        /// Subtraction of lowerCorner for mathematical calculations
        /// </summary>
        /// <param name="points">posList polygon</param>
        /// <param name="lowerCorner">Coordinate XYZ lower Corner</param>
        /// <returns>List of Polygon Points (XYZ)</returns>
        private List<XYZ> CollectPoints(XElement points, XYZ lowerCorner)
        {
            string pointString = points.Value;
            var pointsSeperated = pointString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var polygonVertices = new List<XYZ>();

            for(int i = 0; i < pointsSeperated.Length; i = i + 3)
            {
                var coord = SplitCoordinate(new string[] { pointsSeperated[i], pointsSeperated[i + 1], pointsSeperated[i + 2] }, lowerCorner);

                polygonVertices.Add(coord);
            }

            for(int i = 0; i < polygonVertices.Count - 1; i++)
            {
                dxf.DrawLine(polygonVertices[i].X + lowerCorner.X, polygonVertices[i].Y + lowerCorner.Y, polygonVertices[i].Z + lowerCorner.Z, polygonVertices[i + 1].X + lowerCorner.X, polygonVertices[i + 1].Y + lowerCorner.Y, polygonVertices[i + 1].Z + lowerCorner.Z, "cityGMLlines", new int[] { 0, 255, 0 });
            }

            return polygonVertices;
        }

        /// <summary>
        /// Splitting of Pos for single XYZ coordinate
        /// Subtraction of lowerCorner for mathematical calculations
        /// </summary>
        /// <param name="position">pos tag</param>
        /// <param name="lowerCorner">Coordinate XYZ lower Corner</param>
        /// <returns>Polygon Point (XYZ)</returns>
        private XYZ CollectPoint(XElement position, XYZ lowerCorner)
        {
            var pointSeperated = position.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return SplitCoordinate(pointSeperated, lowerCorner);
        }

        private XYZ SplitCoordinate(string[] xyzString, XYZ lowerCorner)
        {
            double x = Double.Parse(xyzString[0], CultureInfo.InvariantCulture) - lowerCorner.X;
            double y = Double.Parse(xyzString[1], CultureInfo.InvariantCulture) - lowerCorner.Y;
            double z = Double.Parse(xyzString[2], CultureInfo.InvariantCulture) - lowerCorner.Z;

            dxf.DrawPoint(x + lowerCorner.X, y + lowerCorner.Y, z + lowerCorner.Z, "cityGMLpts", new int[] { 0, 255, 0 });

            return new XYZ(x, y, z);
        }

        /// <summary>
        /// Checks polygon conditions (Start = End)
        /// </summary>
        /// <param name="points">PointList Polygon</param>
        /// <param name="lowerCorner">Coordinate XYZ lower Corner</param>
        /// <returns>List of Polygon Points</returns>
        private bool SameStartAndEndPt(List<XYZ> polygon)
        {
            var start = polygon.First();
            var end = polygon.Last();

            if(start.X != end.X || start.Y != end.Y || start.Z != end.Z)
                return false;

            return true;
        }

        /// <summary>
        /// Checks polygon conditions (Redundant Points?)
        /// </summary>
        /// <param name="points">PointList Polygon</param>
        /// <returns>List of Polygon Points</returns>
        private bool NoRedundantPts(List<XYZ> polygon)
        {
            foreach(var pt in polygon)
            {
                var samePts = from p in polygon
                              where (pt != p && pt.X == p.X && pt.Y == p.Y && pt.Z == p.Z)
                              select p;

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