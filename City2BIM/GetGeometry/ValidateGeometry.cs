using System.Collections.Generic;
using System.Linq;
using City2BIM.GmlRep;
using Serilog;

namespace City2BIM.GetGeometry
{
    internal class ValidateGeometry
    {
        public List<GmlSurface> FilterUnneccessaryPoints(List<GmlSurface> rawSurfaces)
        {
            var filteredSurfaces = new List<GmlSurface>();
            var ptListWithPolyID = new Dictionary<C2BPoint, string>();

            foreach(var rawSurface in rawSurfaces)
            {
                //Prüfung - gleicher Start - und Endpunkt
                //Entfernen des letzten Punktes, wenn gleich zum Startpunkt (Normalfall bei Polygon)
                // ---------------------------------------- -
                Log.Debug("Check polygon geometry for same start and end point...");
                var checkPolyEx = SameStartAndEndPt(rawSurface.Exterior);

                if(!checkPolyEx)
                    Log.Error("Not equal at exterior polygon!");
                else
                    rawSurface.Exterior.Remove(rawSurface.Exterior.Last());

                if(rawSurface.Interior != null)
                {
                    var checkPolyIn = SameStartAndEndPt(rawSurface.Interior);

                    if(!checkPolyIn)
                        Log.Error("Not equal at interior polygon!");
                    else
                        rawSurface.Interior.Remove(rawSurface.Interior.Last());
                }
                //------------------------------------------------------------------------------

                //Prüfung - keine redundanten Punkte (außer Start und End)
                //erstmal nur Logging, wie behandeln? unterschiedliche Handungsweisen nötig je nach Reihenfolge
                //-----------------------------------------
                Log.Debug("Check polygon geometry for redundant points in polygon (beside of start/end...");
                var checkRedunEx = NoRedundantPts(rawSurface.Exterior);

                if(!checkRedunEx)
                    Log.Error("Redundant points at exterior polygon!");

                if(rawSurface.Interior != null)
                {
                    var checkRedunIn = NoRedundantPts(rawSurface.Interior);

                    if(!checkRedunIn)
                        Log.Error("Redundant points at interior polygon!");
                }
                //-------------------------------------------------------------------------------------------------------------

                //Combining of all polygon points of a building for determining of points which are not part of at least 3 planes
                //Storing of id neccessary for later identification and updating of polygon point list
                Log.Debug("Check all building polygon points (vertices) for occurence in at least 3 surfaces (planes)...");
                foreach(var rawPt in rawSurface.Exterior)
                {
                    ptListWithPolyID.Add(rawPt, rawSurface.SurfaceId);
                }

                if(rawSurface.Interior != null)
                {
                    foreach(var rawPt in rawSurface.Interior)
                    {
                        ptListWithPolyID.Add(rawPt, rawSurface.SurfaceId + "_void");
                    }
                }
            }

            var bldgXYZ = ptListWithPolyID.Keys.OrderBy(x => x.X).OrderBy(y => y.Y).OrderBy(z => z.Z).ToList();

            var bldgXYZF = new List<C2BPoint>();    //temporäre Liste für vermutlich falsche Punkte (1 oder 2 Polygone)

            var redCt = 0;  //Schleifenvariable (=0 für Start)

            for(int i = 0; i < bldgXYZ.Count; i += redCt)   //Schleife zur Suche identischer Punkte (innerhalb Toleranz, Hochzählen um Anzahl ident. Pkt.)
            {
                int locRedCt = 0;                       //interner Zähler für redundante Punkte
                var redPts = new List<C2BPoint>();       //Initialsierung für Liste ident. Punkte pro Durchlauf

                redCt = 1;                          //Punkt i zählt mit

                for(int j = i + 1; j < bldgXYZ.Count; j++)      //interne Schleife (j jeweils i+1, nächster Punkt in Liste)
                                                                //foreach(var xyzOther in bldgXYZ)
                {
                    if(bldgXYZ[i] == bldgXYZ[j])        //zur Sicherheit
                        continue;

                    double ptDist = C2BPoint.DistanceSq(bldgXYZ[i], bldgXYZ[j]);     //Distanzberechnung

                    if(ptDist < Prop.Distolsq)
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
                ptListWithPolyID.Remove(ptF);  //jeder vermutlich falsche Punkt wird aus Punktliste gelöscht
            }

            if(bldgXYZF.Count > 0)
                Log.Warning("Amount of points not occured at min 3 planes: " + bldgXYZF.Count);

            var polyL = ptListWithPolyID.Values.Distinct();  //Distinct: Zusammenfassen aller gleichen PolygonIDs zu einer ID-Liste

            foreach(var polyID in polyL)
            {
                if(polyID.Contains("void"))
                    continue;

                var points = (from p in ptListWithPolyID
                              where p.Value == polyID
                              select p.Key).ToList();                  //Selektieren aller Punkte pro Polygon-ID

                Log.Debug("Create new surface without detected points...");

                var filteredSurface = new GmlSurface();
                filteredSurface.SurfaceId = polyID;

                var pInt = from p in polyL
                           where p.Contains("void")
                           select p;

                if(pInt.Count() > 0)
                {
                    foreach(var voidP in pInt)
                    {
                        var idInt = voidP.Split('_')[0];

                        if(idInt == polyID)
                        {
                            var pointsInt = (from p in ptListWithPolyID
                                             where p.Value == voidP
                                             select p.Key).ToList();                  //Selektieren aller Punkte pro Polygon-ID

                            filteredSurface.Interior = pointsInt;
                        }
                    }
                }

                filteredSurface.Exterior = points;

                var surface = (from a in rawSurfaces
                               where a.SurfaceId == polyID
                               select a).SingleOrDefault();

                filteredSurface.Facetype = surface.Facetype;
                filteredSurface.SurfaceAttributes = surface.SurfaceAttributes;

                filteredSurfaces.Add(filteredSurface);
            }

            return filteredSurfaces;
        }

        /// <summary>
        /// Checks polygon conditions (Start = End)
        /// </summary>
        /// <param name="points">PointList Polygon</param>
        /// <param name="lowerCorner">Coordinate XYZ lower Corner</param>
        /// <returns>List of Polygon Points</returns>
        private bool SameStartAndEndPt(List<C2BPoint> polygon)
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
        private bool NoRedundantPts(List<C2BPoint> polygon)
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