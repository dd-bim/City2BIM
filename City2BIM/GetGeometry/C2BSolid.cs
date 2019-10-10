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

        public Dictionary<string, C2BPlane> PlanesCopy
        {
            get { return planesCopy; }
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

            RemoveNoCornerVertices();
        }


        private void AggregatePlanes(ref bool simPlanes)
        {
            //double locDistolsq = 0.000001; //1mm^2! -8
            //double locDistolsq = 0.0000001; //1mm^2! -7
            //double locDistolsq = 0.00000001; //1mm^2! -6
            //double locDistolsq = 0.00001; //1mm^2! -5
            double locDistolsq = 0.0001; //1mm^2! -4
            //double locDistolsq = 0.001; //1mm^2! -2
            //double locDistolsq = 0.01; //1mm^2! -2

            for (var i = 0; i < Edges.Count; i++)
            {
                for (var j = i + 1; j < Edges.Count; j++)
                {
                    //if (i == j)
                    //    continue;

                    if (Edges[i].Start == Edges[j].End && Edges[i].End == Edges[j].Start &&
                        C2BPoint.DistanceSq(Edges[i].PlaneNormal, Edges[j].PlaneNormal) < locDistolsq)
                    {
                        //var logPlanes = new LoggerConfiguration()
                        //    .MinimumLevel.Debug()
                        //    .WriteTo.File(@"C:\Users\goerne\Desktop\logs_revit_plugin\\log_planes" + DateTime.UtcNow.ToFileTimeUtc() + ".txt"/*, rollingInterval: RollingInterval.Day*/)
                        //    .CreateLogger();

                        //logPlanes.Information("Potential similar Planes: ");
                        //logPlanes.Information("Plane A: " + Edges[i].PlaneId);
                        //logPlanes.Information("Plane B: " + Edges[j].PlaneId);

                        //logPlanes.Information("Edges before:");

                        //foreach (var ed in edges)
                        //{
                        //    logPlanes.Information(ed.PlaneId + ": " + ed.Start + " / " + ed.End);
                        //}

                        //logPlanes.Information("PlanesCopy before:");

                        //foreach (var pl in planesCopy)
                        //{
                        //    logPlanes.Information(pl.Key);
                        //}

                        C2BPlane plane1 = planesCopy[Edges[i].PlaneId];
                        C2BPlane plane2 = planesCopy[Edges[j].PlaneId];

                        int cursorEdgeA = plane1.Edges.IndexOf(Edges[i]);
                        int cursorEdgeB = plane2.Edges.IndexOf(Edges[j]);

                        for (var k = 0; k < cursorEdgeB; k++)
                        {
                            var edge = plane2.Edges[0];
                            plane2.Edges.RemoveAt(0);
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
                            //logPlanes.Information(e.PlaneId);

                            if (e.PlaneId == currentID1 || e.PlaneId == currentID2)
                            {
                                e.PlaneId = newID;
                                //logPlanes.Information("true");
                            }
                            //else
                            //    logPlanes.Information("false");
                        }

                        Edges[i].PlaneId = null;
                        Edges[j].PlaneId = null;

                        //calc logic for new plane normal and new plane centroid

                        C2BPoint planeNormal = plane1.Normal;
                        C2BPoint planeCentroid = plane1.Centroid;

                        UpdatePlaneParameters(newID, newVerts, ref planeNormal, ref planeCentroid);

                        planesCopy.Remove(plane1.ID);
                        planesCopy.Remove(plane2.ID);
                        planesCopy.Add(newID, new C2BPlane(newID, newVerts, planeNormal, planeCentroid, cpdEdgeList));

                        break;
                    }
                    else
                        simPlanes = false;
                }
            }
        }

        private void UpdatePlaneParameters(string planeID, List<int> newVerts, ref C2BPoint planeNormal, ref C2BPoint planeCentroid)
        {
            C2BPoint norm = new C2BPoint(0, 0, 0);
            C2BPoint centr = new C2BPoint(0, 0, 0);

            for (var i = 0; i < newVerts.Count; i++)
            {
                int lastVert = newVerts.Last();

                if (i != 0)
                    lastVert = newVerts[i - 1];

                int currVert = newVerts[i];

                C2BPoint lastPt = Vertices[lastVert].Position;
                C2BPoint thisPt = Vertices[currVert].Position;

                centr += thisPt;

                norm += C2BPoint.CrossProduct(lastPt, thisPt);

                Vertices[newVerts[i]].Planes.Add(planeID);

            }

            planeNormal = C2BPoint.Normalized(norm);
            planeCentroid = centr / (double)newVerts.Count;

            UpdateVertexPositions(newVerts, planeNormal, planeCentroid);

        }

        private void RemoveNoCornerVertices()
        {
            var remVerts = Vertices.Where(v => v.Planes.Count < 3);



            foreach (var v in remVerts)
            {
                int vIndex = Vertices.IndexOf(v);

                var planesInd = from p in PlanesCopy
                                where p.Value.Vertices.Contains(vIndex)
                                select p.Value;

                foreach (var pl in planesInd)
                {
                    pl.Vertices = pl.Vertices.Where(i => i != vIndex).ToArray();
                }



            }



            //Removes Vertices with less than 3 Planes out of Vertex-List
            //Vertices.RemoveAll(v => v.Planes.Count < 3);

            //Also Removement in Planes(Copy)-List necessary


        }

        private void UpdateVertexPositions(List<int> newVerts, C2BPoint planeNormal, C2BPoint planeCentroid)
        {
            var projectedVerts = new List<C2BPoint>();

            for (var i = 0; i < newVerts.Count; i++)
            {
                C2BPoint thisPt = Vertices[newVerts[i]].Position;

                var vecPtCent = thisPt - planeCentroid;
                var d = C2BPoint.ScalarProduct(vecPtCent, planeNormal);

                var vecLotCent = new C2BPoint(d * planeNormal.X, d * planeNormal.Y, d * planeNormal.Z);
                var vertNew = thisPt - vecLotCent;

                //-----debug

                var diffPt = thisPt - vertNew;



                Vertices[newVerts[i]].Position = vertNew;


            }

        }

        public void CalculatePositions(ref int[] ctPlanes, ref bool detF)
        {
            Dictionary<int, string[]> planesToSplit = new Dictionary<int, string[]>();

            for (var v = 0; v < vertices.Count; v++)
            {
                ctPlanes[vertices[v].Planes.Count]++;

                Log.Debug("Calculation for " + v);

                if (vertices[v].Planes.Count < 3)     //cases of removed vertices before, no longer consideration but no removement of Vertex-List because of consistency
                {
                    Log.Debug("Skip because < 3 Planes!");
                    continue;
                }

                if (vertices[v].Planes.Count == 3)    //optimal wished case --> no danger of non planar curve loops
                {
                    Log.Debug("Optimal case: 3 Planes!");

                    C2BPoint vertex = new C2BPoint(0, 0, 0);

                    string[] vplanes = vertices[v].Planes.ToArray<string>();

                    //vertices[v].Position = CalculateLevelCut(planes[vplanes[0]], planes[vplanes[1]], planes[vplanes[2]]);
                    vertices[v].Position = CalculateLevelCut(planesCopy[vplanes[0]], planesCopy[vplanes[1]], planesCopy[vplanes[2]]);
                }

                if (vertices[v].Planes.Count > 3)
                {
                    Log.Debug("Dangerous case: " + vertices[v].Planes.Count + " Planes!");

                    string[] vplanes = vertices[v].Planes.ToArray<string>();

                    int first = 0, second = 0, third = 0;
                    double d = 100;
                    C2BPoint origPos = vertices[v].Position;
                    C2BPoint calcPos = origPos;

                    for (var i = 0; i < vplanes.Length - 2; i++)
                    {
                        for (var j = i + 1; j < vplanes.Length - 1; j++)
                        {
                            for (var k = j + 1; k < vplanes.Length; k++)
                            {
                                C2BPoint currPos = CalculateLevelCut(planesCopy[vplanes[i]], planesCopy[vplanes[j]], planesCopy[vplanes[k]]);

                                if (currPos == null)
                                    continue;

                                double dOld = d;
                                double dNew = C2BPoint.DistanceSq(origPos, currPos);

                                if (dNew < dOld)
                                {
                                    calcPos = currPos;
                                    first = i;
                                    second = j;
                                    third = k;

                                    d = dNew;
                                }
                            }
                        }
                    }

                    //Freitag: obigen Algorithmus erweitern
                    //nicht in jedem Fall Lösung mit geringster Distanz--> Wenn vertretbar (Grenze überlegen!) auch andere Lösungen in Betracht ziehen
                    //vermieden werden sollte, dass als splitPlanes, Ebenen mit vielen Vertices übrig bleiben
                    //Splitten ist bei denen nicht ohne Weiteres möglich --> ideal 3 oder 4 Vertices!
                    //wenn mehr als 4, Algorithmus zur Kompletttriangulation überlegen


                    vertices[v].Position = calcPos;

                    string[] splitPlanes = vplanes.Where(w => w != vplanes[first] && w != vplanes[second] && w != vplanes[third]).ToArray();

                    planesToSplit.Add(v, splitPlanes);

                }
            }

            foreach (var vPl in planesToSplit)
            {
                Log.Debug("Split at " + vPl.Key + " for:");

                try
                {

                    foreach (var p in vPl.Value)
                    {
                        Log.Debug(p);

                        var sPlane = planesCopy[p];
                        var sVerts = sPlane.Vertices;

                        if (sVerts.Length == 3)
                            continue;

                        var index = Array.IndexOf(sVerts, vPl.Key);

                        int vertBefore = 0;
                        int vertNext = 0;

                        if (index == 0)
                            vertBefore = sVerts[sVerts.Length - 1];
                        else
                            vertBefore = sVerts[index - 1];

                        if (index == sVerts[sVerts.Length - 1])
                            vertNext = sVerts[0];
                        else
                            vertNext = sVerts[index + 1];

                        //Normale ist hier alte nicht exakte Normale, centroid und edges sind def falsch (sollte aber alles nicht mehr relevant sein)
                        C2BPlane splitPlaneTri =
                            new C2BPlane(p + "_split1", new List<int>() { vertBefore, vPl.Key, vertNext }, sPlane.Normal, sPlane.Centroid, sPlane.Edges);

                        List<int> restVerts = sVerts.Where(w => w != vPl.Key).ToList();

                        //Rest des Planes (Dreieck an Vertex abgeschnitten), beachte falsche Edges, (Normale), Centroid
                        C2BPlane splitPlaneRest =
                            new C2BPlane(p + "_split2", restVerts, sPlane.Normal, sPlane.Centroid, sPlane.Edges);



                        //check CCW order in new splitted triangle:

                        //_isCCW = Vector2.Det(lup - prev, next - prev) > 0.0;
                        //Det: Det(Vector2 a, Vector2 b) => (a.X * b.Y) - (a.Y * b.X);

                        var ptBef = Vertices[vertBefore].Position;
                        var ptCurr = Vertices[vPl.Key].Position;
                        var ptNext = Vertices[vertNext].Position;

                        var vecBef = ptCurr - ptBef;
                        var vecNext = ptNext - ptBef;

                        var det = (vecBef.X * vecNext.Y) - (vecBef.Y * vecNext.X);



                        string detS = "";

                        if (det < -0.05)
                            detS = "minus";
                        else if (det > 0.05)
                            detS = "plus";
                        else
                            detS = "null";


                        planesCopy.Add(detS + splitPlaneTri.ID, splitPlaneTri);
                        planesCopy.Add(detS + splitPlaneRest.ID, splitPlaneRest);
                        planesCopy.Remove(p);


                    }
                }
                catch
                {
                    continue; }

            }
        }


        private C2BPoint CalculateLevelCut(C2BPlane plane1, C2BPlane plane2, C2BPlane plane3)
        {
            double determinant = 0;
            determinant = C2BPoint.ScalarProduct(plane1.Normal, C2BPoint.CrossProduct(plane2.Normal, plane3.Normal));

            if (Math.Abs(determinant) > Determinanttol)
            {
                C2BPoint pos = (C2BPoint.CrossProduct(plane2.Normal, plane3.Normal) * C2BPoint.ScalarProduct(plane1.Centroid, plane1.Normal) +
                           C2BPoint.CrossProduct(plane3.Normal, plane1.Normal) * C2BPoint.ScalarProduct(plane2.Centroid, plane2.Normal) +
                           C2BPoint.CrossProduct(plane1.Normal, plane2.Normal) * C2BPoint.ScalarProduct(plane3.Centroid, plane3.Normal)) /
                           determinant;
                return pos;
            }
            else
                return null;
        }
    }
}