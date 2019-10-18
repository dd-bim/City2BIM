//using System.Collections.Generic;
//using System.Linq;
//using City2BIM.GmlRep;
//using Serilog;

//namespace City2BIM.GetGeometry
//{
//    internal class ValidateGeometry
//    {
//        public List<GmlSurface> FlatteningSurfaces(List<GmlSurface> rawSurfaces)
//        {
//            var flattenedSurfaces = new List<GmlSurface>();

//            foreach(var surface in rawSurfaces)
//            {
//                var rawPoints = surface.PlaneExt.PolygonPts;

//                C2BPoint normalVc = new C2BPoint(0, 0, 0);
//                C2BPoint centroidPl = new C2BPoint(0, 0, 0);

//                for(var c = 1; c < rawPoints.Count; c++)
//                {
//                    normalVc += C2BPoint.CrossProduct(rawPoints[c - 1], rawPoints[c]);

//                    centroidPl += rawPoints[c];
//                }

//                var centroid = centroidPl / (rawPoints.Count - 1);
//                var normalizedVc = C2BPoint.Normalized(normalVc);

//                var projectedVerts = new List<C2BPoint>();

//                foreach(var pt in rawPoints)
//                {
//                    var vecPtCent = pt - centroid;
//                    var d = C2BPoint.ScalarProduct(vecPtCent, normalizedVc);

//                    var vecLotCent = new C2BPoint(d * normalizedVc.X, d * normalizedVc.Y, d * normalizedVc.Z);
//                    var vertNew = pt - vecLotCent;

//                    projectedVerts.Add(vertNew);
//                }

//                surface.PlaneExt.Normal = normalizedVc;
//                surface.PlaneExt.Centroid = centroid;
//                surface.PlaneExt.PolygonPts = projectedVerts;

//                flattenedSurfaces.Add(surface);
//            }

//            return flattenedSurfaces;
//        }

//        public List<GmlSurface> FilterUnneccessaryPoints(List<GmlSurface> planeSurfaces)
//        {
//            var filteredSurfaces = new List<GmlSurface>();
//            var ptListWithPolyID = new Dictionary<C2BPoint, string>();

//            foreach(var planeSurface in planeSurfaces)
//            {
//                //Prüfung - gleicher Start - und Endpunkt
//                //Entfernen des letzten Punktes, wenn gleich zum Startpunkt (Normalfall bei Polygon)
//                // ---------------------------------------- -
//                var checkPolyEx = SameStartAndEndPt(planeSurface.PlaneExt.PolygonPts);

//                if(!checkPolyEx)
//                    Log.Error("Not equal at exterior polygon!");
//                else
//                    planeSurface.PlaneExt.PolygonPts.Remove(planeSurface.PlaneExt.PolygonPts.Last());

//                //if(planeSurface.PlaneInt.Count > 0)
//                //{
//                //    foreach(var pl in planeSurface.PlaneInt)
//                //    {
//                //        var checkPolyIn = SameStartAndEndPt(pl.PolygonPts);

//                //        if(!checkPolyIn)
//                //            Log.Error("Not equal at interior polygon!");
//                //        else
//                //            pl.PolygonPts.Remove(pl.PolygonPts.Last());
//                //    }
//                //}
//                //------------------------------------------------------------------------------

//                //Prüfung - keine redundanten Punkte (außer Start und End)
//                //erstmal nur Logging, wie behandeln? unterschiedliche Handungsweisen nötig je nach Reihenfolge
//                //-----------------------------------------
//                var checkRedunEx = NoRedundantPts(planeSurface.PlaneExt.PolygonPts);

//                if(!checkRedunEx)
//                    Log.Error("Redundant points at exterior polygon!");

//                //if(planeSurface.PlaneInt.Count > 0)
//                //{
//                //    foreach(var pl in planeSurface.PlaneInt)
//                //    {
//                //        var checkRedunIn = NoRedundantPts(pl.PolygonPts);

//                //        if(!checkRedunIn)
//                //            Log.Error("Redundant points at interior polygon!");
//                //    }
//                //}
//                //-------------------------------------------------------------------------------------------------------------

//                //Combining of all polygon points of a building for determining of points which are not part of at least 3 planes
//                //Storing of id neccessary for later identification and updating of polygon point list
//                foreach(var rawPt in planeSurface.PlaneExt.PolygonPts)
//                {
//                    ptListWithPolyID.Add(rawPt, planeSurface.PlaneExt.ID);
//                }

//                //if(planeSurface.PlaneInt.Count > 0)
//                //{
//                //    foreach(var pl in planeSurface.PlaneInt)
//                //    {
//                //        foreach(var rawPt in pl.PolygonPts)
//                //        {
//                //            ptListWithPolyID.Add(rawPt, pl.ID);
//                //        }
//                //    }
//                //}
//            }

//            var bldgXYZ = ptListWithPolyID.Keys.OrderBy(x => x.X).OrderBy(y => y.Y).OrderBy(z => z.Z).ToList();

//            var bldgXYZF = new List<C2BPoint>();    //temporäre Liste für vermutlich falsche Punkte (1 oder 2 Polygone)

//            var redCt = 0;  //Schleifenvariable (=0 für Start)

//            for(int i = 0; i < bldgXYZ.Count; i += redCt)   //Schleife zur Suche identischer Punkte (innerhalb Toleranz, Hochzählen um Anzahl ident. Pkt.)
//            {
//                int locRedCt = 0;                       //interner Zähler für redundante Punkte
//                var redPts = new List<C2BPoint>();       //Initialsierung für Liste ident. Punkte pro Durchlauf

//                redCt = 1;                          //Punkt i zählt mit

//                for(int j = i + 1; j < bldgXYZ.Count; j++)      //interne Schleife (j jeweils i+1, nächster Punkt in Liste)
//                                                                //foreach(var xyzOther in bldgXYZ)
//                {
//                    if(bldgXYZ[i] == bldgXYZ[j])        //zur Sicherheit
//                        continue;

//                    double ptDist = C2BPoint.DistanceSq(bldgXYZ[i], bldgXYZ[j]);     //Distanzberechnung

//                    if(ptDist < Prop.Distolsq)
//                    {
//                        //wenn kleiner als Toleranz (siehe Prop-Klasse) ist der Punkt redundant
//                        locRedCt++;
//                        redCt++;

//                        redPts.Add(bldgXYZ[j]);
//                    }
//                    else
//                    {
//                        break;          //Abbruch, wenn kein "identischer" Punkt mehr gefunden wird (durch geordnete Liste wird keiner vergessen)
//                    }
//                }

//                if(locRedCt < 2)        //gewünschter Fall: pro Vertex sind 3 Punkte in CityGML enthalten
//                {
//                    bldgXYZF.Add(bldgXYZ[i]);      //bei weniger als 3 (kein Ebenenschnitt möglich) -> Speichern in temp. Liste
//                    bldgXYZF.AddRange(redPts);
//                }
//            }

//            foreach(var ptF in bldgXYZF)
//            {
//                ptListWithPolyID.Remove(ptF);  //jeder vermutlich falsche Punkt wird aus Punktliste gelöscht
//            }

//            if(bldgXYZF.Count > 0)
//                Log.Warning("Amount of points not occured at min 3 planes: " + bldgXYZF.Count);

//            //Now: recirculation of the points to the respective polygon via id

//            var polyL = ptListWithPolyID.Values.Distinct();  //Distinct: Combine all the same PolygonIDs to an ID list

//            foreach(var polyID in polyL)
//            {
//                //skip for interior polygons (will be handled internally)
//                if(polyID.Contains("void"))
//                    continue;

//                //Identify points which are belonging to the same polygon via same id

//                var points = (from p in ptListWithPolyID
//                              where p.Value == polyID
//                              select p.Key).ToList();

//                Log.Debug("Create new surface without detected points...");

//                //Identify the matching GmlSurface

//                var surfaceEquiv = (from a in planeSurfaces
//                                    where a.PlaneExt.ID == polyID
//                                    select a).SingleOrDefault();

//                //Adding of startpoint at the end for valid polygon (was removed before for correct identifying of unneccessary points)
//                points.Add(points[0]);

//                surfaceEquiv.PlaneExt.PolygonPts = points;

//                //----handling of possible interior polygons-----
//                //if(surfaceEquiv.PlaneInt.Count > 0)            //if original surface has interior polygons...
//                //{
//                //    //identify interior polygons

//                //    var intPoints = from poly in polyL
//                //                    where poly.Contains(surfaceEquiv.SurfaceId + "_void")
//                //                    select poly;

//                //    var intPlanes = new List<C2BPlane>();

//                //    //there could be more than one hole (interior polygon)
//                //    foreach(var pId in intPoints)
//                //    {
//                //        var intPlane = new C2BPlane(pId);

//                //        //Identify points which are belonging to the same polygon via same id

//                //        var pointsInt = (from pt in ptListWithPolyID
//                //                         where pt.Value == pId
//                //                         select pt.Key).ToList();

//                //        //Adding of startpoint at the end for valid polygon (was removed before for correct identifying of unneccessary points)
//                //        pointsInt.Add(pointsInt[0]);

//                //        intPlane.PolygonPts = pointsInt;

//                //        intPlanes.Add(intPlane);
//                //    }

//                //    surfaceEquiv.PlaneInt = intPlanes;
//                //}
//                //----------------------------------------------
//                filteredSurfaces.Add(surfaceEquiv);
//            }
//            return filteredSurfaces;
//        }

//        /// <summary>
//        /// Checks polygon conditions (Start = End)
//        /// </summary>
//        /// <param name="points">PointList Polygon</param>
//        /// <param name="lowerCorner">Coordinate XYZ lower Corner</param>
//        /// <returns>List of Polygon Points</returns>
//        private bool SameStartAndEndPt(List<C2BPoint> polygon)
//        {
//            var start = polygon.First();
//            var end = polygon.Last();

//            if(start.X != end.X || start.Y != end.Y || start.Z != end.Z)
//                return false;

//            return true;
//        }

//        /// <summary>
//        /// Checks polygon conditions (Redundant Points?)
//        /// </summary>
//        /// <param name="points">PointList Polygon</param>
//        /// <returns>List of Polygon Points</returns>
//        private bool NoRedundantPts(List<C2BPoint> polygon)
//        {
//            foreach(var pt in polygon)
//            {
//                var samePts = from p in polygon
//                              where (pt != p && pt.X == p.X && pt.Y == p.Y && pt.Z == p.Z)
//                              select p;

//                if(pt == polygon.First() && samePts.Count() > 1)
//                    return false;

//                if(pt == polygon.Last() && samePts.Count() > 1)
//                    return false;

//                if(pt != polygon.First() && pt != polygon.Last() && samePts.Count() > 0)
//                    return false;
//            }

//            return true;
//        }
//    }
//}