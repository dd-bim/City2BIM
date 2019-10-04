using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using static City2BIM.Prop;

namespace City2BIM.GetGeometry
{
    public class C2BSolid
    {
        private Dictionary<string, C2BPlane> planes = new Dictionary<string, C2BPlane>();
        private Dictionary<string, C2BPlane> planesCopy = new Dictionary<string, C2BPlane>();
        private List<C2BVertex> vertices = new List<C2BVertex>();
        private List<C2BEdge> edges = new List<C2BEdge>();

        public List<C2BVertex> Vertices
        {
            get { return vertices; }
        }



        public Dictionary<string, C2BPlane> Planes
        {
            get { return planes; }
        }

        public List<C2BEdge> Edges { get => edges; set => edges = value; }

        public void AddPlane(string id, List<C2BPoint> polygon)
        {
            if (polygon.Count < 5)
            {
                Log.Error("Not enough points for valid plane: " + polygon.Count);
            }

            C2BPoint normal = new C2BPoint(0, 0, 0);
            C2BPoint centroid = new C2BPoint(0, 0, 0);

            List<C2BEdge> locEdges = new List<C2BEdge>();

            //list for vertex-integers (size = polygon-points.Length)
            List<int> verts = new List<int>(polygon.Count);
            int currentInt = 0;

            //Loop over all polygon points (starts with 1 for no redundant point detecting)
            for (int i = 1; i < polygon.Count; i++)
            {
                int beforeInt = currentInt;

                //bool for decision if new vertex must be created
                bool notmatched = true;

                //inner loop over all vertices (list per whole building (!), if vertices contains objects)
                for (int j = 0; j < Vertices.Count; j++)
                {
                    //calclation of distance between current polygon point and current vertex in list 
                    double dist = C2BPoint.DistanceSq(polygon[i], vertices[j].Position);

                    //case: distance smaller than setted Distolsq (= points are topological identical --> Vertex)
                    //if points are identical, an equivalent vertex is still existinng in vertices list
                    if (dist < Distolsq)
                    {
                        currentInt = j;

                        //add plane id to current vertex in list
                        vertices[j].AddPlane(id);
                        //add vertex-iterator to verts list (for later identification)
                        verts.Add(j);
                        notmatched = false;
                        break;
                    }
                }

                //no match --> a new vertex needs to create
                if (notmatched)
                {
                    currentInt = vertices.Count;

                    C2BVertex v = new C2BVertex(polygon[i], id);
                    //list of verts gets a new number at the end of list
                    verts.Add(vertices.Count);
                    //Vertex bldg list gets new Vertex
                    vertices.Add(v);
                }

                //------------------------------------------------------------------------------

                //adds normal value (normal of plane which current point and the point before span) 
                normal += C2BPoint.CrossProduct(polygon[i - 1], polygon[i]);

                //adds current coordinates to centroid variable for later centroid calculation
                centroid += polygon[i];

                //edge needs start- and end vertex, and also normal between points for later identification of similar planes

            }

            C2BPoint planeNormal = C2BPoint.Normalized(normal);

            for (var v = 0; v < verts.Count; v++)
            {
                int beforeInt = 0;

                if (v == 0)
                    beforeInt = verts.Last();
                else
                    beforeInt = verts[v - 1];

                var edge = new C2BEdge(beforeInt, verts[v], id, planeNormal);

                locEdges.Add(edge);
                Edges.Add(edge);
            }

            //create plane..
            //with plane normal (via normalization of spanned normals of the poly points) 
            //with centroid dependent of number of poly points
            var plane = new C2BPlane(id, verts, planeNormal, centroid / ((double)verts.Count), locEdges);

            Planes.Add(id, plane);

        }

        public void IdentifySimilarPlanes()
        {
            //----------------------------------------------------
            //PlaneyCopy list needed for later address at level cuts
            //original list will not be corrupted because original surface geometry should be imported (not combined surfaces)
            planesCopy = new Dictionary<string, C2BPlane>();

            foreach (var pl in Planes)
            {
                planesCopy.Add(pl.Key, pl.Value);
            }
            //----------------------------------------------------

            //cases for similar planes
            //1.) simple case: similar plane normal and one shared edge
            //2.) special cases: more than two planes with similar plane normal and respectively one shared edge (case 1 applied multiple times)
            //the possibility of case 2 requires combining of two planes and a new search for similar planes after each combination

            bool similarPlanes = true;

            while (similarPlanes)
            {
                AggregatePlanes(ref similarPlanes);
            }
        }


        private void AggregatePlanes(ref bool simPlanes)
        {


            for (var i = 0; i < Edges.Count; i++)
            {
                for (var j = i + 1; j < Edges.Count; j++)
                {
                    if (Edges[i].Start == Edges[j].End && Edges[i].End == Edges[j].Start &&
                        C2BPoint.DistanceSq(Edges[i].PlaneNormal, Edges[j].PlaneNormal) < Distolsq)
                    {
                        var logPlanes = new LoggerConfiguration()
                            .MinimumLevel.Debug()
                            .WriteTo.File(@"C:\Users\goerne\Desktop\logs_revit_plugin\\log_planes" + DateTime.UtcNow.ToFileTimeUtc() + ".txt"/*, rollingInterval: RollingInterval.Day*/)
                            .CreateLogger();

                        logPlanes.Information("Potential similar Planes: ");
                        logPlanes.Information("Plane A: " + Edges[i].PlaneId);
                        logPlanes.Information("Plane B: " + Edges[j].PlaneId);

                        logPlanes.Information("Edges before:");

                        foreach (var ed in edges)
                        {
                            logPlanes.Information(ed.PlaneId + ": " + ed.Start + " / " + ed.End);
                        }

                        logPlanes.Information("PlanesCopy before:");

                        foreach (var pl in planesCopy)
                        {
                            logPlanes.Information(pl.Key);
                        }

                        C2BPlane plane1 = planesCopy[Edges[i].PlaneId];
                        C2BPlane plane2 = planesCopy[Edges[j].PlaneId];

                        int cursorEdgeA = plane1.Edges.IndexOf(Edges[i]);
                        int cursorEdgeB = plane2.Edges.IndexOf(Edges[j]);

                        for (var k = 0; k < cursorEdgeB; k++)
                        {
                            var edge = plane2.Edges[k];
                            plane2.Edges.RemoveAt(k);
                            plane2.Edges.Add(edge);
                        }

                        List<C2BEdge> cpdEdgeList = new List<C2BEdge>();

                        cpdEdgeList.AddRange(plane1.Edges);
                        cpdEdgeList.RemoveAt(cursorEdgeA);
                        cpdEdgeList.InsertRange(cursorEdgeA, plane2.Edges);
                        cpdEdgeList.Remove(Edges[j]);

                        var newVerts = cpdEdgeList.Select(e => e.End).ToList();
                        var newID = Edges[i].PlaneId + "_" + Edges[j].PlaneId;


                        var changeVert1 = from v in Vertices
                                          where v.Planes.Contains(Edges[i].PlaneId)
                                          select v;

                        var changeVert2 = from v in Vertices
                                          where v.Planes.Contains(Edges[j].PlaneId)
                                          select v;

                        foreach (var v in changeVert1)
                        {
                            v.Planes.Remove(Edges[i].PlaneId);
                            v.Planes.Add(newID);
                        }

                        foreach (var v in changeVert2)
                        {
                            v.Planes.Remove(Edges[j].PlaneId);
                            v.Planes.Add(newID);
                        }

                        //var edgesPlane1 = from e in Edges
                        //                 where e.PlaneId.Equals(Edges[i].PlaneId)
                        //                 select e;

                        //var edgesPlane2 = from e in Edges
                        //                  where e.PlaneId.Equals(Edges[j].PlaneId)
                        //                  select e;

                        //var combEdgesPl1Pl2 = edgesPlane1.Concat(edgesPlane2);

                        //foreach (var ed in combEdgesPl1Pl2)
                        //{
                        //    ed.PlaneId = newID;
                        //}

                        var currentID1 = Edges[i].PlaneId;
                        var currentID2 = Edges[j].PlaneId;

                        foreach (var e in Edges)
                        {
                            logPlanes.Information(e.PlaneId);

                            if (e.PlaneId == currentID1 || e.PlaneId == currentID2)
                            {
                                e.PlaneId = newID;
                                logPlanes.Information("true");
                            }
                            else
                                logPlanes.Information("false");
                        }

                        Edges[i].PlaneId = null;
                        Edges[j].PlaneId = null;

                        planesCopy.Remove(plane1.ID);
                        planesCopy.Remove(plane2.ID);
                        planesCopy.Add(newID, new C2BPlane(newID, newVerts, plane1.Normal, plane1.Centroid, cpdEdgeList));

                        logPlanes.Information("Edges after:");

                        foreach (var ed in edges)
                        {
                            logPlanes.Information(ed.PlaneId + ": " + ed.Start + " / " + ed.End);
                        }

                        logPlanes.Information("PlanesCopy after:");

                        foreach (var pl in planesCopy)
                        {
                            logPlanes.Information(pl.Key);
                        }

                        break;
                    }
                    else
                        simPlanes = false;
                }
                //break;
            }
        }


        //    List<string> PlanesToRemove = new List<string>();
        //    List<C2BPlane> PlanesToAdd = new List<C2BPlane>();

        //    foreach (var plane in planesCopy)
        //    {
        //        if (PlanesToRemove.Contains(plane.Key))
        //            continue;

        //        var plEdges = plane.Value.Edges;
        //        var otherPlEdges = new List<C2BEdge>();

        //        for (var i = 0; i < plEdges.Count; i++)
        //        {
        //            for (var j = 0; j < Edges.Count; j++)
        //            {
        //                if (plane.Key.Equals(Edges[j].PlaneId))
        //                    continue;

        //                if (plEdges[i].Start == Edges[j].End && plEdges[i].End == Edges[j].Start &&
        //                    C2BPoint.DistanceSq(plEdges[i].PlaneNormal, Edges[j].PlaneNormal) < Distolsq)
        //                {

        //                    Log.Information("Potential similar Planes: ");
        //                    Log.Information("bldg: " + id);
        //                    Log.Information("Plane A: " + plEdges[i].PlaneId);
        //                    Log.Information("Plane B: " + Edges[j].PlaneId);

        //                    otherPlEdges.Add(Edges[j]);
        //                }
        //            }
        //        }
        //        if (otherPlEdges.Any())
        //        {
        //            var cpdPlane = CompoundPlanes(plane.Value, otherPlEdges);      //--> Rückgabe, was in PlanesCopy geändert werden muss (add / remove) --> Schreiben in Liste --> Transaction nach Durchlauf ?

        //            PlanesToAdd.Add(cpdPlane);

        //            foreach (var ed in otherPlEdges)
        //            {
        //                PlanesToRemove.Add(ed.PlaneId);
        //            }
        //        }
        //    }

        //    foreach (var pl in PlanesToRemove)
        //    {
        //        planesCopy.Remove(pl);
        //    }

        //    foreach (var pl in PlanesToAdd)
        //    {
        //        planesCopy.Add(pl.ID, pl);
        //    }


        //}



        private C2BPlane CompoundPlanes(C2BPlane plane, List<C2BEdge> otherEdges)
        {
            try
            {
                string newID = plane.ID;
                HashSet<string> oldIDs = new HashSet<string>();
                var cpdPlane = new C2BPlane(newID);

                foreach (var oEdge in otherEdges)
                {
                    List<C2BEdge> cpdEdgeList = new List<C2BEdge>();

                    C2BPlane plane2 = Planes[oEdge.PlaneId];

                    C2BEdge plEdge = plane.Edges.Where(a => a.Start == oEdge.End).Where(a => a.End == oEdge.Start).FirstOrDefault();

                    int cursorEdgeA = plane.Edges.IndexOf(plEdge);
                    int cursorEdgeB = plane2.Edges.IndexOf(oEdge);

                    for (var i = 0; i < cursorEdgeB; i++)
                    {
                        var edge = plane2.Edges[i];
                        plane2.Edges.RemoveAt(i);
                        plane2.Edges.Add(edge);
                    }

                    cpdEdgeList.AddRange(plane.Edges);
                    cpdEdgeList.RemoveAt(cursorEdgeA);
                    cpdEdgeList.InsertRange(cursorEdgeB + 1, plane2.Edges);
                    cpdEdgeList.Remove(oEdge);


                    var newVerts = cpdEdgeList.Select(e => e.End).ToList();
                    newID = plane.ID + "_" + plane2.ID;
                    oldIDs.Add(plane.ID);
                    oldIDs.Add(plane2.ID);

                    cpdPlane.ID = newID;
                    cpdPlane.Vertices = newVerts.ToArray();
                    cpdPlane.Edges = cpdEdgeList;
                    //planesCopy.Add(cpdPlane.ID, cpdPlane);

                    plane = cpdPlane;
                }

                return cpdPlane;


                //List<C2BEdge> cpdEdgeList = new List<C2BEdge>();

                //C2BPlane plane1 = Planes[a.PlaneId];
                //C2BPlane plane2 = Planes[b.PlaneId];

                //int cursorEdgeA = plane1.Edges.IndexOf(a);
                //int cursorEdgeB = plane2.Edges.IndexOf(b);

                //for (var i = 0; i < cursorEdgeB; i++)
                //{
                //    var edge = plane2.Edges[i];
                //    plane2.Edges.RemoveAt(i);
                //    plane2.Edges.Add(edge);
                //}

                //cpdEdgeList.AddRange(plane1.Edges);
                //cpdEdgeList.RemoveAt(cursorEdgeA);
                //cpdEdgeList.InsertRange(cursorEdgeB + 1, plane2.Edges);
                //cpdEdgeList.Remove(b);

                //edgeList saved
                //--------------------------------------

                //create compoundPlane with same Parameters as in one of the similar planes
                //Discussion: new Centroid-Calculation neccessary?, new Normal? (no significant changes)

                //foreach (var oldID in oldIDs)
                //{
                //    var changeVert = from v in Vertices
                //                     where v.Planes.Contains(oldID)
                //                     select v;


                //    foreach (var v in changeVert)
                //    {
                //        v.Planes.Remove(oldID);
                //        v.Planes.Add(newID);
                //    }

                //    planesCopy.Remove(oldID);
                //}



            }
            catch
            {
                //Log.Error("error in compoundPlane" + a.ID + " , " + b.ID);

                return null;
            }
        }

        //private C2BPlane CreateNewPlane(List<int> newVerts, string idA, string idB)
        //{
        //    C2BPoint centroid = new C2BPoint(0, 0, 0);
        //    C2BPoint normal = new C2BPoint(0, 0, 0);
        //    var xyzList = new List<C2BPoint>();

        //    foreach(var vInd in newVerts)
        //    {
        //        GetGeometry.C2BPoint vertPos = vertices[vInd].Position;
        //        xyzList.Add(vertPos);
        //    }

        //    xyzList.Add(xyzList.First());

        //    for(int m = 1; m < xyzList.Count; m++)
        //    {
        //        normal += C2BPoint.CrossProduct(xyzList[m - 1], xyzList[m]);
        //        centroid += xyzList[m];
        //    }

        //    var norm = C2BPoint.Normalized(normal);
        //    var compPlane = new C2BPlane(idA + idB, newVerts, norm, centroid / ((double)newVerts.Count), C2BPlane.FaceType.unknown, C2BPlane.RingType.unknown);

        //    Log.Information("Normale, neue Ebene = " + norm.X + ", " + norm.Y + ", " + norm.Z);

        //    return compPlane;
        //}

        //private void RemoveVertices()
        //{
        //    var vert2Pl = from v in vertices
        //                  where v.Planes.Count < 3
        //                  select v;

        //    var examInd = new List<int>();

        //    foreach(var v2Pl in vert2Pl)
        //    {
        //        var index = vertices.IndexOf(v2Pl);
        //        examInd.Add(index);
        //    }

        //    foreach(var ind in examInd)
        //    {
        //        foreach(var pla in planes)
        //        {
        //            if(pla.Value.Vertices.Contains(ind))
        //            {
        //                var newVerts = pla.Value.Vertices.Where(val => val != ind).ToArray();

        //                pla.Value.Vertices = newVerts;

        //                //.............Redundanz, noch in Methode zusammenfassen, Ebenenparameter müssen nach Löschen neu bestimmt werden

        //                Log.Information("Normale, noch nicht veränderte Ebene = " + pla.Value.Normal.X + ", " + pla.Value.Normal.Y + ", " + pla.Value.Normal.Z);

        //                C2BPoint centroid = new C2BPoint(0, 0, 0);
        //                C2BPoint normal = new C2BPoint(0, 0, 0);
        //                var xyzList = new List<C2BPoint>();

        //                foreach(var vInd in pla.Value.Vertices)
        //                {
        //                    GetGeometry.C2BPoint vertPos = vertices[vInd].Position;
        //                    xyzList.Add(vertPos);
        //                }

        //                xyzList.Add(xyzList.First());

        //                for(int m = 1; m < xyzList.Count; m++)
        //                {
        //                    normal += C2BPoint.CrossProduct(xyzList[m - 1], xyzList[m]);
        //                    centroid += xyzList[m];
        //                }

        //                var norm = C2BPoint.Normalized(normal);

        //                pla.Value.Normal = norm;
        //                pla.Value.Centroid = centroid / ((double)newVerts.Length);
        //                //var compPlane = new Plane(idA + idB, newVerts, norm, centroid / ((double)newVerts.Count));

        //                Log.Information("Normale, veränderte Ebene = " + norm.X + ", " + norm.Y + ", " + norm.Z);

        //                //...........................................
        //            }
        //        }
        //    }

        //    vertices.RemoveAll(v => vert2Pl.Contains(v));
        //}

        public void CalculatePositions()
        {
            //Log.Information("Planes.Count vorher = " + planes.Count);

            //var pla18 = from pl in planes.Values
            //            where pl.Vertices.Contains(18)
            //            select pl;

            //var pla24 = from pl in planes.Values
            //            where pl.Vertices.Contains(24)
            //            select pl;

            //foreach(var pl in planes.Values)
            //{
            //    Log.Information("VPlane Id: " + pl.ID);

            //    foreach(var vert in pl.Vertices)
            //    {
            //        Log.Information("Vertices: " + vert);
            //    }
            //}

            //Log.Information("Ebenencheck");
            //Log.Information("--------------");

            ////CheckPlanes();

            //Log.Information("--------------");
            //Log.Information("Planes.Count nachher = " + planes.Count);

            //var a = vertices.Count;
            //Log.Information("Vertices.Count vorher = " + vertices.Count);

            ////RemoveVertices();

            //var b = vertices.Count;
            //Log.Information("Vertices.Count nachher = " + vertices.Count);

            //if((a - b) != 0)
            //    Log.Information("Vertex nach EqualPlanes gelöscht!");

            //foreach(var pl in planes.Values)
            //{
            //    Log.Information("NPlane Id: " + pl.ID);

            //    foreach(var vert in pl.Vertices)
            //    {
            //        Log.Information("Vertices: " + vert);
            //    }
            //}

            foreach (C2BVertex v in vertices)
            {
                //Log.Debug("Level cut at Vertex " + v.Position.X + " ," + v.Position.Y + " ," + v.Position.Z + " for " + v.Planes.Count + " planes.");

                if (v.Planes.Count != 3)
                {
                    Log.Error("Anzahl Ebenen pro Vertex = " + v.Planes.Count);

                    foreach (var vert in v.Planes)
                    {
                        Log.Warning("Vertices: " + vert);
                    }
                }

                if (v.Planes.Count == 3)
                {
                    C2BPoint vertex = new C2BPoint(0, 0, 0);

                    string[] vplanes = v.Planes.ToArray<string>();
                    //C2BPlane plane1 = planes[vplanes[0]];
                    //C2BPlane plane2 = planes[vplanes[1]];
                    //C2BPlane plane3 = planes[vplanes[2]];

                    C2BPlane plane1 = planesCopy[vplanes[0]];
                    C2BPlane plane2 = planesCopy[vplanes[1]];
                    C2BPlane plane3 = planesCopy[vplanes[2]];



                    double determinant = C2BPoint.ScalarProduct(plane1.Normal, C2BPoint.CrossProduct(plane2.Normal, plane3.Normal));

                    if (Math.Abs(determinant) > Determinanttol)
                    {
                        C2BPoint pos = (C2BPoint.CrossProduct(plane2.Normal, plane3.Normal) * C2BPoint.ScalarProduct(plane1.Centroid, plane1.Normal) +
                                   C2BPoint.CrossProduct(plane3.Normal, plane1.Normal) * C2BPoint.ScalarProduct(plane2.Centroid, plane2.Normal) +
                                   C2BPoint.CrossProduct(plane1.Normal, plane2.Normal) * C2BPoint.ScalarProduct(plane3.Centroid, plane3.Normal)) /
                                   determinant;
                        v.Position = pos;
                    }
                    else
                    {
                        Log.Error("Determinante ist falsch bei genau 3 Ebenen!, Determinante = " + determinant);

                        //throw new Exception("Hier ist die Determinante falsch");
                    }
                }
                else if (v.Planes.Count > 3)
                {
                    Log.Information("Vertex > 3) " + v.Position.X + " , " + v.Position.Y + " , " + v.Position.Z);

                    foreach (var p in v.Planes)
                    {
                        Log.Information("Planes " + p);
                    }

                    //Ebenencheck

                    C2BPoint vertex = new C2BPoint(0, 0, 0);

                    string[] vplanes = v.Planes.ToArray<string>();
                    double bestskalar = 1;
                    //C2BPlane plane1 = planes[vplanes[0]];
                    //C2BPlane plane2 = planes[vplanes[0]];
                    //C2BPlane plane3 = planes[vplanes[0]];

                    C2BPlane plane1 = planesCopy[vplanes[0]];
                    C2BPlane plane2 = planesCopy[vplanes[0]];
                    C2BPlane plane3 = planesCopy[vplanes[0]];

                    for (int i = 0; i < v.Planes.Count - 1; i++)
                    {
                        //C2BPlane p1 = planes[vplanes[i]];

                        C2BPlane p1 = planesCopy[vplanes[i]];

                        for (int j = i + 1; j < v.Planes.Count; j++)
                        {
                            //C2BPlane p2 = Planes[vplanes[j]];

                            C2BPlane p2 = planesCopy[vplanes[j]];

                            //Log.Information("Normalen, p1: " + p1.Normal.X + " , " + p1.Normal.Y + " , " + p1.Normal.Z);
                            //Log.Information("Normalen, p2: " + p2.Normal.X + " , " + p2.Normal.Y + " , " + p2.Normal.Z);

                            double skalar = Math.Abs(C2BPoint.ScalarProduct(p1.Normal, p2.Normal));

                            //var degSkalar = Math.Acos(skalar) * (180 / Math.PI);

                            //if(degSkalar < 1)
                            //{
                            //    Log.Warning("Skalarprodukt: " + skalar + " entspricht " + degSkalar + "schlechter Schnitt!");

                            //    CompoundPlanes(p1, p2);

                            //    //Planes zusammenführen...
                            //}
                            //else
                            //    Log.Information("Skalarprodukt: " + skalar + " entspricht " + degSkalar);

                            if (skalar < bestskalar)         //sucht besten skalar für besten Schnitt (möglichst rechtwinklig)
                            {
                                bestskalar = skalar;
                                plane1 = p1;
                                plane2 = p2;
                                vertex = C2BPoint.CrossProduct(plane1.Normal, plane2.Normal);
                            }
                        }
                    }

                    for (int k = 0; k < v.Planes.Count; k++)
                    {
                        //C2BPlane p3 = planes[vplanes[k]];

                        C2BPlane p3 = planesCopy[vplanes[k]];
                        double skalar = Math.Abs(C2BPoint.ScalarProduct(vertex, p3.Normal));
                        if (skalar > bestskalar)
                        {
                            plane3 = p3;
                        }
                    }

                    double determinant = C2BPoint.ScalarProduct(plane1.Normal, C2BPoint.CrossProduct(plane2.Normal, plane3.Normal));

                    if (Math.Abs(determinant) > Determinanttol)
                    {
                        C2BPoint pos = (C2BPoint.CrossProduct(plane2.Normal, plane3.Normal) * C2BPoint.ScalarProduct(plane1.Centroid, plane1.Normal) +
                                   C2BPoint.CrossProduct(plane3.Normal, plane1.Normal) * C2BPoint.ScalarProduct(plane2.Centroid, plane2.Normal) +
                                   C2BPoint.CrossProduct(plane1.Normal, plane2.Normal) * C2BPoint.ScalarProduct(plane3.Centroid, plane3.Normal)) /
                                   determinant;
                        v.Position = pos;
                    }
                    else
                    {
                        Log.Error("Determinante falsch bei " + v.Planes.Count + " Ebenen!, Determinante = " + determinant);

                        //throw new Exception("Hier ist die Determinante falsch");
                    }
                }
                else
                {
                    //vertexErrors.Add(v);

                    Log.Error("Zu wenig Ebenen!, Anzahl Ebenen = " + v.Planes.Count);

                    //throw new Exception("Zu wenig Ebenen");
                }
            }

            foreach (var e in Edges)
            {
                Log.Information("Edge-PlaneID: " + e.PlaneId);
            }

        }





        //public void ClearSolid()
        //{
        //    planes.Clear();
        //    vertices.Clear();
        //}

        //public ReadGeometry ReadData
        //{
        //    get => default(ReadGeometry);
        //    set
        //    {
        //    }
        //}
    }
}