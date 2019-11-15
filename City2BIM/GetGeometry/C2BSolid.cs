using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using static City2BIM.Prop;

namespace City2BIM.GetGeometry
{
    public class C2BSolid
    {
        private Guid internalID;
        private string gmlID;
        private Dictionary<string, C2BPlane> _planes = new Dictionary<string, C2BPlane>();
        private List<C2BVertex> _vertices = new List<C2BVertex>();
        private List<C2BEdge> _edges = new List<C2BEdge>();

        public List<C2BVertex> Vertices
        {
            get { return _vertices; }
            set
            {
                this._vertices = value;
            }
        }

        public Dictionary<string, C2BPlane> Planes
        {
            get { return _planes; }

        }

        public List<C2BEdge> Edges { get => _edges; set => _edges = value; }

        public Guid InternalID { get => internalID; }

        public C2BSolid(string gmlID)
        {
            this.internalID = Guid.NewGuid();
            this.gmlID = gmlID;
        }

        public void AddPlane(string id, List<C2BPoint> polygon, List<List<C2BPoint>> innerPolygons = null)
        {
            C2BPoint normal = new C2BPoint(0, 0, 0);
            C2BPoint centroid = new C2BPoint(0, 0, 0);

            List<C2BEdge> locEdges = new List<C2BEdge>();

            List<int> extVerts = CalculateVertexCoords(id, polygon, true, ref normal, ref centroid);
            List<List<int>> intVertsList = new List<List<int>>();

            foreach (List<C2BPoint> inPoly in innerPolygons)
            {
                List<int> intVerts = CalculateVertexCoords(id, inPoly, false, ref normal, ref centroid);
                intVertsList.Add(intVerts);
            }

            C2BPoint planeNormal = C2BPoint.Normalized(normal);

            for (var v = 0; v < extVerts.Count; v++)
            {
                int beforeInt = 0;

                if (v == 0)
                    beforeInt = extVerts.Last();
                else
                    beforeInt = extVerts[v - 1];

                var edge = new C2BEdge(beforeInt, extVerts[v], id, planeNormal);

                locEdges.Add(edge);
                Edges.Add(edge);
            }

            //create plane..
            //with plane normal (via normalization of spanned normals of the poly points) 
            //with centroid dependent of number of poly points
            var plane = new C2BPlane(id, extVerts, intVertsList, planeNormal, centroid / ((double)extVerts.Count), locEdges);

            Planes.Add(id, plane);

        }

        private List<int> CalculateVertexCoords(string id, List<C2BPoint> polygon, bool exterior, ref C2BPoint normal, ref C2BPoint centroid)
        {
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
                    double dist = C2BPoint.DistanceSq(polygon[i], Vertices[j].Position);

                    //case: distance smaller than setted Distolsq (= points are topological identical --> Vertex)
                    //if points are identical, an equivalent vertex is still existinng in vertices list
                    if (dist < Distolsq)
                    {
                        currentInt = j;

                        //add plane id to current vertex in list
                        Vertices[j].AddPlane(id);
                        //add vertex-iterator to verts list (for later identification)
                        verts.Add(j);
                        notmatched = false;
                        break;
                    }
                }

                //no match --> a new vertex needs to create
                if (notmatched)
                {
                    currentInt = Vertices.Count;

                    C2BVertex v = new C2BVertex(polygon[i]);
                    v.AddPlane(id);
                    //list of verts gets a new number at the end of list
                    verts.Add(Vertices.Count);
                    //Vertex bldg list gets new Vertex
                    Vertices.Add(v);
                }

                //------------------------------------------------------------------------------

                if (exterior)
                {
                    //adds normal value (normal of plane which current point and the point before span) 
                    var locN = C2BPoint.CrossProduct(polygon[i - 1], polygon[i]);

                    normal += locN;

                    //adds current coordinates to centroid variable for later centroid calculation
                    centroid += polygon[i];
                }
            }

            return verts;
        }


        public void IdentifySimilarPlanes()
        {
            //cases for similar Planes
            //1.) simple case: similar plane normal and one shared edge
            //2.) special cases: more than two Planes with similar plane normal and respectively one shared edge (case 1 applied multiple times)
            //the possibility of case 2 requires combining of two Planes and a new search for similar Planes after each combination

            bool similarPlanes = true;

            if (Edges.Count > 0)
            {
                while (similarPlanes)
                {
                    AggregatePlanes(ref similarPlanes);
                }
            }

            RemoveNoCornerVertices();
        }


        /// <summary>
        /// Aggregates polygon planes which are (almost) the same plane
        /// </summary>
        /// <param name="simPlanes">bool for loop break control</param>
        private void AggregatePlanes(ref bool simPlanes)
        {
            //defined tolerance for same planes (distance between ends of normalized plane vector)
            double locDistolsq = 0.0025; //== 5 cm

            //loop over all edges at solid
            for (var i = 0; i < Edges.Count - 1; i++)
            {
                for (var j = i + 1; j < Edges.Count; j++)
                {
                    //distane between end of normalized normal vectors
                    double distNorm = C2BPoint.DistanceSq(Edges[i].PlaneNormal, Edges[j].PlaneNormal);

                    //condition: distance within tolerance and same edge (Start=End, End=Start)
                    if (Edges[i].Start == Edges[j].End && Edges[i].End == Edges[j].Start &&
                        distNorm < locDistolsq)
                    {
                        try
                        {
                            //identify planes where same edges apply
                            C2BPlane plane1 = Planes[Edges[i].PlaneId];
                            C2BPlane plane2 = Planes[Edges[j].PlaneId];

                            bool samePlane = false;

                            if (plane1.ID == plane2.ID)
                                samePlane = true;

                            int cursorEdgeA = plane1.Edges.IndexOf(Edges[i]);
                            int cursorEdgeB = plane2.Edges.IndexOf(Edges[j]);

                            if (!samePlane)
                            {

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

                                var currentID1 = Edges[i].PlaneId;
                                var currentID2 = Edges[j].PlaneId;

                                foreach (var e in Edges)
                                {
                                    //logPlanes.Information(e.PlaneId);

                                    if (e.PlaneId == currentID1 || e.PlaneId == currentID2)
                                    {
                                        e.PlaneId = newID;
                                    }
                                }

                                Edges[i].PlaneId = null;
                                Edges[j].PlaneId = null;

                                //calc logic for new plane normal and new plane centroid

                                C2BPoint planeNormal = plane1.Normal;
                                C2BPoint planeCentroid = plane1.Centroid;

                                List<int[]> innerVerts = plane1.InnerVertices;

                                innerVerts.AddRange(plane2.InnerVertices);

                                UpdatePlaneParameters(newID, newVerts, ref planeNormal, ref planeCentroid);

                                Planes.Remove(plane1.ID);
                                Planes.Remove(plane2.ID);
                                Planes.Add(newID, new C2BPlane(newID, newVerts, innerVerts, planeNormal, planeCentroid, cpdEdgeList));

                                Log.Information("Match Plane 1: " + plane1.ID);
                                Log.Information("Match Plane 2: " + plane2.ID);

                                Log.Information("United Plane:  " + newID);

                            }
                            else
                            {
                                Log.Information("Match at unified Plane: " + plane1.ID);

                                var uniPlane = plane1;

                                var vs = uniPlane.Vertices.GroupBy(vtx => vtx);
                                var vt = vs.Where(vx => vx.Count() > 1);
                                var vtt = vt.First().Key;

                                var ind = uniPlane.Vertices.ToList().IndexOf(vtt);

                                int vdeadEnd = 0;

                                if (ind == uniPlane.Vertices.Length - 1)
                                    vdeadEnd = uniPlane.Vertices[0];
                                else
                                    vdeadEnd = uniPlane.Vertices[ind + 1];

                                var verts = uniPlane.Vertices.ToList();

                                verts.RemoveAt(ind);
                                verts.Remove(vdeadEnd);

                                uniPlane.Vertices = verts.ToArray();

                                var pNormal = uniPlane.Normal;
                                var pCentr = uniPlane.Centroid;

                                UpdatePlaneParameters(uniPlane.ID, verts, ref pNormal, ref pCentr);

                                Edges[i].PlaneId = null;
                                Edges[j].PlaneId = null;

                                Planes[plane1.ID].Centroid = pCentr;
                                Planes[plane1.ID].Normal = pNormal;
                                Planes[plane1.ID].Vertices = verts.ToArray();

                            }

                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message);
                            continue;
                        }
                        break;
                    }
                    else
                        simPlanes = false;

                    if (j == Edges.Count - 1)
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
        }

        private void RemoveNoCornerVertices()
        {
            var remVerts = Vertices.Where(v => v.Planes.Count < 3);

            foreach (var v in remVerts)
            {
                int vIndex = Vertices.IndexOf(v);

                var PlanesInd = from p in Planes
                                where p.Value.Vertices.Contains(vIndex)
                                select p.Value;

                foreach (var pl in PlanesInd)
                {
                    pl.Vertices = pl.Vertices.Where(i => i != vIndex).ToArray();
                }

                var PlanesIndInt = from p in Planes
                                   where p.Value.InnerVertices.Any()
                                   select p.Value;

                foreach (var plane in PlanesIndInt)
                {
                    for (int j = 0; j < plane.InnerVertices.Count; j++)
                    {
                        if (plane.InnerVertices[j].Contains(vIndex))
                            plane.InnerVertices[j] = plane.InnerVertices[j].Where(i => i != vIndex).ToArray();
                    }
                }
            }
        }

        public void CalculatePositions()
        {

            Log.Debug("START Vertext-Calculation at: " + gmlID + "----------------------------------------------");

            Dictionary<string, List<int>> PlanesToSplit = new Dictionary<string, List<int>>();

            for (var v = 0; v < Vertices.Count; v++)
            {
                try
                {

                    #region Level Cut (3 planes - unambiguous)

                    if (Vertices[v].Planes.Count == 3)    //optimal wished case --> no danger of non planar curve loops
                    {
                        C2BPoint vertex = new C2BPoint(0, 0, 0);

                        string[] vPlanes = Vertices[v].Planes.ToArray<string>();

                        Vertices[v].Position = CalculateLevelCut(Planes[vPlanes[0]], Planes[vPlanes[1]], Planes[vPlanes[2]]);
                    }

                    #endregion Level Cut (3 planes - unambiguous)

                    #region Level Cut (> 3 planes - ambiguous, further calculations)

                    if (Vertices[v].Planes.Count > 3)
                    {
                        Log.Debug("LEVEL CUT ambiguous: more than 3 Vertices at " + v + "---------------------");

                        string[] vPlanes = Vertices[v].Planes.ToArray<string>();
                        string[] splitPlanes = new string[vPlanes.Count() - 3];

                        double d = 100;
                        C2BPoint origPos = Vertices[v].Position;
                        C2BPoint calcPos = origPos;

                        List<string> ordPlanes = new List<string>();
                        List<string> unordPlanes = new List<string>();

                        List<string> convexPl = new List<string>();
                        int ctConcave = 0;

                        #region Test - Selection of cutted planes regarding concaveness 

                        //foreach (string pId in vPlanes)
                        //{
                        //    var sPlane = Planes[pId];
                        //    var sVerts = sPlane.Vertices;
                        //    int vertCt = sVerts.Count();

                        //    int v1 = 0;
                        //    int v3 = 0;
                        //    int v4 = 0;

                        //    FindAdjacentIndices(sVerts, v, out v1, out v3, out v4);

                        //    bool divisible = true;

                        //    //CCW test for plane at vertex where cut of more than 3 planes applied
                        //    bool ccw = C2BPoint.CCW(Vertices[v1].Position, Vertices[v].Position, Vertices[v3].Position, sPlane.Normal);

                        //    //CCW test with possibly new edge and other polygon vertices to avoid false split generation

                        //    int[] vRest = sVerts.Where(vtx => vtx != v1 && vtx != v && vtx != v3).ToArray();

                        //    if (ccw)
                        //    {
                        //        foreach (int vtx in vRest)
                        //        {
                        //            bool ccwPlane = C2BPoint.CCW(Vertices[v1].Position, Vertices[v3].Position, Vertices[vtx].Position, sPlane.Normal);

                        //            if (!ccwPlane)
                        //            {
                        //                bool ccwPlane2 = C2BPoint.CCW(Vertices[v1].Position, Vertices[v].Position, Vertices[vtx].Position, sPlane.Normal);
                        //                bool ccwPlane3 = C2BPoint.CCW(Vertices[v].Position, Vertices[v3].Position, Vertices[vtx].Position, sPlane.Normal);

                        //                if (ccwPlane2 && ccwPlane3)
                        //                {
                        //                    divisible = false;
                        //                    break;
                        //                }
                        //            }
                        //        }
                        //    }
                        //    else
                        //        divisible = false;

                        //    //if CCW gets false there is an concave angle at vertex for the investigated plane 
                        //    //if CCW gets an value next to 0 there is an collinear situation at vertex for the investigated plane (also false result)
                        //    //for both of this situations a splitting of the planes later on could lead to a wrong solid calculation
                        //    //therefore this planes should be part in the level cut (so they not have to be splitted)
                        //    //exeptional cases are ih the concave plane has 4 vertices --> then a splitting with the opposite vertex is also possible 



                        //    if (!divisible && vertCt > 4)
                        //    {
                        //        ordPlanes.Add(pId);
                        //        ctConcave++;
                        //    }
                        //    else
                        //        unordPlanes.Add(pId);
                        //}

                        unordPlanes = vPlanes.ToList();

                        int ctNoConvex = 0;
                        List<string> unordPlanes1 = new List<string>();
                        List<string> unordPlanes2 = new List<string>();

                        foreach (var plane in unordPlanes)
                        {
                            bool convex = true;

                            var adjacPlane = Planes[plane];
                            var verts = adjacPlane.Vertices;

                            bool ccwAngleStart = C2BPoint.CCW(Vertices[verts.Length - 1].Position, Vertices[verts[0]].Position, Vertices[verts[1]].Position, adjacPlane.Normal);
                            bool ccwAngleEnd = C2BPoint.CCW(Vertices[verts.Length - 2].Position, Vertices[verts.Length - 1].Position, Vertices[verts[0]].Position, adjacPlane.Normal);

                            if (!ccwAngleStart || !ccwAngleEnd)
                            {
                                convex = false;
                            }

                            for (int vtx = 1; vtx < verts.Length - 1; vtx++)
                            {
                                bool ccwAngle = C2BPoint.CCW(Vertices[vtx - 1].Position, Vertices[vtx].Position, Vertices[vtx + 1].Position, adjacPlane.Normal);

                                if (!ccwAngle)
                                {
                                    convex = false;
                                }
                            }

                            if (!convex && verts.Length > 4)
                            {
                                unordPlanes1.Add(plane);
                                ctNoConvex++;
                            }
                            else
                            {
                                unordPlanes2.Add(plane);
                            }

                        }

                        ordPlanes.AddRange(unordPlanes1.OrderByDescending(p => Planes[p].Vertices.Count()));
                        ordPlanes.AddRange(unordPlanes2.OrderByDescending(p => Planes[p].Vertices.Count()));

                        Log.Debug("RESULT of Selection:");
                        Log.Debug("...forced PlanesA: " + ctConcave);
                        Log.Debug("...forced PlanesB: " + ctNoConvex);
                        Log.Debug("Cut1: " + Planes[ordPlanes[0]].Vertices.Count());
                        Log.Debug("Cut2: " + Planes[ordPlanes[1]].Vertices.Count());
                        Log.Debug("Cut3: " + Planes[ordPlanes[2]].Vertices.Count());

                        for (int i = 3; i < ordPlanes.Count; i++)
                        {
                            Log.Debug("Split" + (i + 1) + ": " + Planes[ordPlanes[i]].Vertices.Count());
                        }

                        Log.Debug("------------------------------------");

                        for (var i = 0; i < ordPlanes.Count - 2; i++)
                        {
                            for (var j = i + 1; j < ordPlanes.Count - 1; j++)
                            {
                                for (var k = j + 1; k < ordPlanes.Count; k++)
                                {
                                    C2BPoint currPos = CalculateLevelCut(Planes[ordPlanes[i]], Planes[ordPlanes[j]], Planes[ordPlanes[k]]);

                                    if (currPos == null)
                                        continue;

                                    double dNew = C2BPoint.DistanceSq(origPos, currPos);

                                    if (dNew < d)        //shorter distance of level cut result to original coord
                                    {
                                        calcPos = currPos;
                                        d = dNew;

                                        //not at level cut involved planes --> those ones must be splitted if they have more than 3 vertices for planarity
                                        splitPlanes = ordPlanes.Where(w => w != ordPlanes[i] && w != ordPlanes[j] && w != ordPlanes[k]).ToArray();

                                    }

                                    if ((ctConcave+ctNoConvex) > 2)
                                        break;

                                    if (d < 0.05)
                                        break;
                                }

                                if ((ctConcave + ctNoConvex) == 2)
                                    break;

                                if (d < 0.05)
                                    break;
                            }
                            if ((ctConcave + ctNoConvex) == 1)
                                break;

                            if (d < 0.05)
                                break;
                        }

                        Log.Debug("RESULT of Level Cut = " + d);
                        Log.Debug("------------------------------------");

                        #endregion Test - Selection of cutted planes regarding concaveness

                        Vertices[v].Position = calcPos;

                        foreach (string pl in splitPlanes)
                        {
                            if (PlanesToSplit.ContainsKey(pl))
                                PlanesToSplit[pl].Add(v);
                            else
                                PlanesToSplit.Add(pl, new List<int> { v });
                        }
                    }

                    #endregion Level Cut (> 3 planes - ambiguous, further calculations)
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    continue;
                }

            }

            #region Split Planes which were not inclued in level cut

            Log.Debug("RESULT of Splits: ");
            Log.Debug("------------------------------------");

            foreach (var vPl in PlanesToSplit)
            {
                try
                {

                    //Info: each plane consists of its ID as Key and a List of integers which represent the vertices where a split is neccessary

                    //List of integer-Arrays which represent the Vertex-Lists of the new planes                
                    List<int[]> newPlaneVertices = new List<int[]>();

                    //properties of the the plane which has to be split
                    var sPlane = Planes[vPl.Key];
                    int[] sVerts = sPlane.Vertices;
                    int ctVerts = sVerts.Length;

                    //in case of triangle, no split is neccessary (3 vertices define unambiguously a plane) 
                    if (ctVerts == 3)
                    {
                        Log.Debug("NO SPLIT: Plane is Triangle");
                        continue;
                    }

                    //no matter of number of vertices, at first the first Vertex will be investigated
                    int vtx = vPl.Value.First();

                    //adjacent vertices at plane will be read
                    FindAdjacentIndices(sVerts, vtx, out int vertBefore, out int vertNext, out int vertOpp);

                    //definition of first result of split is an triangle (beside of inner else: number of vertices > 1 and plane has more than 4 corners) 
                    int[] triangleS1 = new int[3];

                    triangleS1[0] = vertBefore;
                    triangleS1[1] = vtx;

                    //no matter of number of vertices where a split should apply: if plane has 4 corners, it could be split like the follow (result 2 triangles)
                    if (ctVerts == 4)
                    {
                        Log.Debug("SIMPLE, 2 Triangles, at " + gmlID + "_" + sPlane.ID);

                        triangleS1[2] = vertOpp;

                        int[] triangleS2 = new int[3] { vtx, vertNext, vertOpp };

                        bool ccwOpp1 = C2BPoint.CCW(Vertices[vertBefore].Position, Vertices[vtx].Position, Vertices[vertOpp].Position, sPlane.Normal);
                        bool ccwOpp2 = C2BPoint.CCW(Vertices[vtx].Position, Vertices[vertNext].Position, Vertices[vertOpp].Position, sPlane.Normal);
                        bool ccwVtx1 = C2BPoint.CCW(Vertices[vertBefore].Position, Vertices[vtx].Position, Vertices[vertNext].Position, sPlane.Normal);
                        bool ccwVtx2 = C2BPoint.CCW(Vertices[vertBefore].Position, Vertices[vertNext].Position, Vertices[vertOpp].Position, sPlane.Normal);

                        if (!ccwOpp1 || !ccwOpp2)
                        {
                            if (ccwVtx1 && ccwVtx2)
                            {
                                triangleS1[2] = vertNext;
                                
                                triangleS2[0] = vertBefore;
                                triangleS2[1] = vertNext;
                                triangleS2[2] = vertOpp;

                                Log.Information("Split with 2 Trinagles changed!");
                            }
                        }

                        newPlaneVertices.Add(triangleS1);
                        newPlaneVertices.Add(triangleS2);
                    }
                    //if plane has more than 4 corners, we must differentiate regarding the number vertices where a split should apply 
                    else
                    {
                        //if only one split apply, there is a simple division into a triangle and a polygon with n vertices
                        if (vPl.Value.Count == 1)
                        {
                            Log.Debug("SIMPLE, 1 Triangle, 1 nPolygon, at " + gmlID + "_" + sPlane.ID);

                            triangleS1[2] = vertNext;

                            int[] polygonS2 = sVerts.Where(s => s != vtx).ToArray();
                            int[] vertSplit2 = sVerts.Where(s => s != vtx).ToArray();

                            newPlaneVertices.Add(triangleS1);
                            newPlaneVertices.Add(polygonS2);
                        }
                        //if more than one split apply, there is a complicated division regarding the order of the relevant vertices in original polygon
                        //NOTE: this could be fail, if 
                        //...there is a concave vertex inside
                        //...centroid is outside of polygon
                        else
                        {
                            Log.Debug("COMPLICATED, nTriangles at centroid, at " + gmlID + "_" + sPlane.ID);

                            //new index at end of vertex-list for centroid
                            int indvCen = Vertices.Count;

                            //creation of triangle-vertex-arrays (each edge and the centroid of original plane create a new triangle)
                            for (int i = 1; i < ctVerts; i++)
                            {
                                int[] splitTri = new int[3] { sVerts[i - 1], sVerts[i], indvCen };
                                newPlaneVertices.Add(splitTri);
                            }

                            int[] splitTriLast = new int[3] { sVerts[ctVerts - 1], sVerts[0], indvCen };
                            newPlaneVertices.Add(splitTriLast);

                            //new instance of Vertex for centroid
                            C2BVertex vCen = new C2BVertex(sPlane.Centroid);

                            //add the new Vertex to Solid.Vertices-List
                            Vertices.Add(vCen);
                        }
                    }

                    //create planes out of new Vertex-Arrays and add them to Solid.Planes and Solid.Vertices[i].Planes
                    foreach (int[] newPoly in newPlaneVertices)
                    {
                        string splitId = vPl.Key + Guid.NewGuid().ToString();

                        CalculatePlaneParameters(newPoly.ToList(), out var normalPoly, out var centerPoly);

                        C2BPlane splitPlanePoly =
                            new C2BPlane(splitId, newPoly.ToList(), sPlane.InnerVertices, normalPoly, centerPoly, sPlane.Edges);

                        Planes.Add(splitId, splitPlanePoly);
                        Planes.Remove(vPl.Key);

                        foreach (int v in newPoly)
                        {
                            Vertices[v].AddPlane(splitId);
                        }
                    }

                    #endregion Split Planes which were not inclued in level cut

                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    continue;
                }

            }

            Log.Debug("END Vertext-Calculation at: " + gmlID + "----------------------------------------------");
            Log.Debug("---------------------------------------------------------------------------------------");
            Log.Debug("---------------------------------------------------------------------------------------");
            Log.Debug("---------------------------------------------------------------------------------------");
        }

        private void FindAdjacentIndices(int[] plVertices, int vertex, out int vBefore, out int vNext, out int vOpp)
        {
            int index = Array.IndexOf(plVertices, vertex);

            vBefore = 0;
            vNext = 0;
            vOpp = 0;

            if (index == 0)
            {
                vBefore = plVertices[plVertices.Length - 1];
                vNext = plVertices[1];
                vOpp = plVertices[2];
            }
            else if (index == plVertices.Length - 1)
            {
                vBefore = plVertices[index - 1];
                vNext = plVertices[0];
                vOpp = plVertices[1];
            }
            else
            {
                vBefore = plVertices[index - 1];
                vNext = plVertices[index + 1];
                if (index == plVertices.Length - 2)
                    vOpp = plVertices[0];
                else
                    vOpp = plVertices[index + 2];
            }
        }

        private void CalculatePlaneParameters(List<int> planeVerts, out C2BPoint planeNormal, out C2BPoint center)
        {
            var verts = (from v in planeVerts
                         select Vertices[v].Position).ToList();

            C2BPoint normal = new C2BPoint(0, 0, 0);
            C2BPoint centroid = new C2BPoint(0, 0, 0);

            for (int i = 1; i < verts.Count; i++)
            {
                //adds normal value (normal of plane which current point and the point before span) 
                normal += C2BPoint.CrossProduct(verts[i - 1], verts[i]);

                //adds current coordinates to centroid variable for later centroid calculation
                centroid += verts[i];
            }
            centroid += verts[0];
            normal += C2BPoint.CrossProduct(verts[verts.Count - 1], verts[0]);

            planeNormal = C2BPoint.Normalized(normal);
            center = centroid / verts.Count;

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