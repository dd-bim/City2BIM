using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using static City2BIM.Prop;

namespace City2BIM.GetGeometry
{
    public class C2BSolid
    {
        private Dictionary<string, C2BPlane> planes = new Dictionary<string, C2BPlane>();
        private List<C2BVertex> vertices = new List<C2BVertex>();

        public List<C2BVertex> Vertices
        {
            get { return vertices; }
        }

        public Dictionary<string, C2BPlane> Planes
        {
            get { return planes; }
        }

        public void AddPlane(string id, List<C2BPoint> polygon)
        {
            if(polygon.Count < 5)
            {
                Log.Error("Not enough points for valid plane: " + polygon.Count);
            }

            C2BPoint normal = new C2BPoint(0, 0, 0);
            C2BPoint centroid = new C2BPoint(0, 0, 0);

            List<int> verts = new List<int>(polygon.Count);
            for(int i = 1; i < polygon.Count; i++)
            {
                bool notmatched = true;
                for(int j = 0; j < Vertices.Count; j++)
                {
                    double dist = C2BPoint.DistanceSq(polygon[i], vertices[j].Position);

                    if(dist < Distolsq)
                    {
                        vertices[j].AddPlane(id);
                        verts.Add(j);
                        notmatched = false;
                        break;
                    }
                }

                if(notmatched)
                {
                    C2BVertex v = new C2BVertex(polygon[i], id);
                    verts.Add(vertices.Count);
                    vertices.Add(v);
                }

                //------------------------------------------------------------------------------

                normal += C2BPoint.CrossProduct(polygon[i - 1], polygon[i]);

                //Log.Information("normale vor normierung: " + normal.X + ", " + normal.Y + ", " + normal.Z);

                centroid += polygon[i];
            }

            var plane = new C2BPlane(id, verts, C2BPoint.Normalized(normal), centroid / ((double)verts.Count));

            Planes.Add(id, plane);
        }

        //private void CheckPlanes()
        //{
        //    var remPlanes = new List<string>();
        //    var compPlanes = new List<C2BPlane>();

        //    var vGr3 = (from v in vertices
        //                where v.Planes.Count > 3
        //                select v).ToList();

        //    HashSet<HashSet<string>> vEqPlanesL = new HashSet<HashSet<string>>();

        //    for(int i = 0; i < vGr3.Count() - 1; i++)     //Schleife durch alle Vertices mit mehr als 3 Ebenen
        //    {
        //        string[] vPlanesGr3 = vGr3[i].Planes.ToArray<string>();

        //        int vPlanesCt = vPlanesGr3.Length;

        //        for(int j = 0; j < vPlanesCt - 1; j++)     //Schleife durch alle Planes pro Vertex
        //        {
        //            HashSet<string> eqPlanes = new HashSet<string>();

        //            var p1 = planes[vPlanesGr3[j]];

        //            bool vMatch = false;

        //            for(int k = j + 1; k < vPlanesCt - 1; k++)     //Schleife durch alle Planes pro Vertex
        //            {
        //                var p2 = planes[vPlanesGr3[k]];

        //                double skalar = Math.Abs(C2BPoint.ScalarProduct(p1.Normal, p2.Normal));
        //                var degSkalar = Math.Acos(skalar) * (180 / Math.PI);

        //                if(degSkalar < 1)
        //                {
        //                    vMatch = true;
        //                    eqPlanes.Add(p2.ID);           //Hinzufügen des Planes der inneren Schleife

        //                    Log.Information("Equal Plane = " + p2.ID);
        //                    Log.Information("Normale, alte Ebene2 = " + p2.Normal.X + ", " + p2.Normal.Y + ", " + p2.Normal.Z);
        //                }

        //                if(vPlanesCt == 4 && vMatch)
        //                    break;          //wenn in Vertex match gefunden wurde und "nur" 4 Ebenen vorhanden sind, dann Abbruch der inneren Schleife
        //            }   //Ende innere Schleife über planes in Vertex i

        //            if(vMatch)
        //            {
        //                eqPlanes.Add(p1.ID);       //Hinzufügen des Planes der äußeren Schleife
        //                vEqPlanesL.Add(eqPlanes);           //Hinzufügen der Kombination aus PlaneList und Vertex zu Dictionary

        //                Log.Information("Equal Plane = " + p1.ID);
        //                Log.Information("Normale, alte Ebene1 = " + p1.Normal.X + ", " + p1.Normal.Y + ", " + p1.Normal.Z);

        //                if(vPlanesCt == 4)
        //                    break;
        //            }
        //        }   //Ende äußere Schleife über planes in Vertex i
        //    }      // Ende Schleife über alle Vertices

        //    //Verareitung der Ergebnisse des Checks
        //    //Dictionary enthält Plane-Listen und zugehörigen Vertex (Vertex kann redundant sein!)

        //    //Betrachtung

        //    Log.Information("Ct List: " + vEqPlanesL.Count);

        //    foreach(var pl in vEqPlanesL)
        //    {
        //        var eqPl = pl.OrderByDescending(i => i).ToList();

        //        if(planes.ContainsKey(eqPl[0]) && planes.ContainsKey(eqPl[1]))
        //        {
        //            var newVerts = CompoundPlanes(planes[eqPl[0]], planes[eqPl[1]]);

        //            if(newVerts.Any())
        //            {
        //                var compPlane = CreateNewPlane(newVerts, eqPl[0], eqPl[1]);

        //                planes.Remove(eqPl[0]);
        //                planes.Remove(eqPl[1]);

        //                if(eqPl.Count > 2)
        //                {
        //                    for(int i = 2; i < eqPl.Count - 1; i++)
        //                    {
        //                        newVerts = CompoundPlanes(compPlane, planes[eqPl[i]]);
        //                        compPlane = CreateNewPlane(newVerts, compPlane.ID, eqPl[i]);

        //                        planes.Remove(eqPl[1]);

        //                        foreach(var v in vertices)
        //                        {
        //                            if(v.Planes.Contains(eqPl[i]))
        //                                v.Planes.Remove(eqPl[i]);
        //                        }
        //                    }
        //                }

        //                planes.Add(compPlane.ID, compPlane);

        //                foreach(var v in vertices)
        //                {
        //                    if(v.Planes.Contains(eqPl[0]))
        //                        v.Planes.Remove(eqPl[0]);

        //                    if(v.Planes.Contains(eqPl[1]))
        //                        v.Planes.Remove(eqPl[1]);
        //                }

        //                foreach(var i in newVerts)
        //                {
        //                    vertices[i].AddPlane(compPlane.ID);
        //                }
        //            }
        //        }
        //    }
        //}

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

            foreach(C2BVertex v in vertices)
            {
                Log.Debug("Level cut at Vertex " + v.Position.X + " ," + v.Position.Y + " ," + v.Position.Z + " for " + v.Planes.Count + " planes.");

                if(v.Planes.Count != 3)
                {
                    Log.Error("Anzahl Ebenen pro Vertex = " + v.Planes.Count);

                    foreach(var vert in v.Planes)
                    {
                        Log.Warning("Vertices: " + vert);
                    }
                }

                if(v.Planes.Count == 3)
                {
                    C2BPoint vertex = new C2BPoint(0, 0, 0);

                    string[] vplanes = v.Planes.ToArray<string>();
                    C2BPlane plane1 = planes[vplanes[0]];
                    C2BPlane plane2 = planes[vplanes[1]];
                    C2BPlane plane3 = planes[vplanes[2]];

                    double determinant = C2BPoint.ScalarProduct(plane1.Normal, C2BPoint.CrossProduct(plane2.Normal, plane3.Normal));

                    if(Math.Abs(determinant) > Determinanttol)
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
                else if(v.Planes.Count > 3)
                {
                    Log.Information("Vertex > 3) " + v.Position.X + " , " + v.Position.Y + " , " + v.Position.Z);

                    foreach(var p in v.Planes)
                    {
                        Log.Information("Planes " + p);
                    }

                    //Ebenencheck

                    C2BPoint vertex = new C2BPoint(0, 0, 0);

                    string[] vplanes = v.Planes.ToArray<string>();
                    double bestskalar = 1;
                    C2BPlane plane1 = planes[vplanes[0]];
                    C2BPlane plane2 = planes[vplanes[0]];
                    C2BPlane plane3 = planes[vplanes[0]];

                    for(int i = 0; i < v.Planes.Count - 1; i++)
                    {
                        C2BPlane p1 = planes[vplanes[i]];

                        for(int j = i + 1; j < v.Planes.Count; j++)
                        {
                            C2BPlane p2 = Planes[vplanes[j]];

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

                            if(skalar < bestskalar)         //sucht besten skalar für besten Schnitt (möglichst rechtwinklig)
                            {
                                bestskalar = skalar;
                                plane1 = p1;
                                plane2 = p2;
                                vertex = C2BPoint.CrossProduct(plane1.Normal, plane2.Normal);
                            }
                        }
                    }

                    for(int k = 0; k < v.Planes.Count; k++)
                    {
                        C2BPlane p3 = planes[vplanes[k]];
                        double skalar = Math.Abs(C2BPoint.ScalarProduct(vertex, p3.Normal));
                        if(skalar > bestskalar)
                        {
                            plane3 = p3;
                        }
                    }

                    double determinant = C2BPoint.ScalarProduct(plane1.Normal, C2BPoint.CrossProduct(plane2.Normal, plane3.Normal));

                    if(Math.Abs(determinant) > Determinanttol)
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
        }

        //private List<int> CompoundPlanes(C2BPlane a, C2BPlane b)
        //{
        //    try
        //    {
        //        foreach(var i in a.Vertices)
        //        {
        //            Log.Information("A: " + i.ToString());
        //        }

        //        foreach(var i in b.Vertices)
        //        {
        //            Log.Information("B: " + i.ToString());
        //        }

        //        var aList = a.Vertices.ToList();
        //        var bList = b.Vertices.ToList();

        //        bool match = false;

        //        List<int> compVerts = new List<int>();

        //        for(int i = 0; i < aList.Count; i++)      //paarweise Match gesucht
        //        {
        //            for(int j = bList.Count - 1; j >= 0; j--)
        //            {
        //                if(aList[i] == bList[j]) //1.Match
        //                {
        //                    //jeweilige Nachfolger müssen auch matchen für gleiche Kante
        //                    //aber Achtung wenn Anfang oder Ende der Liste:
        //                    //Anfang: könnte nicht 1. Match, sondern 2.Matchpoint sein --> 1.Matchpoint steht dann am Ende
        //                    //Ende: zu untersuchender Nachfolge steht am Anfang und wurde schon gefunden, fälschlicherweise als 1. Match

        //                    int aNext = 0;
        //                    int insertInd = 0;

        //                    if(i == aList.Count - 1)
        //                    {
        //                        aNext = aList[0];
        //                    }
        //                    else
        //                    {
        //                        aNext = aList[i + 1];
        //                        insertInd = i + 1;
        //                    }

        //                    int bNextCW = 0;
        //                    //bool match2at0 = false;

        //                    if(j == 0)
        //                    {
        //                        bNextCW = bList[bList.Count - 1];
        //                        //match2at0 = true;
        //                    }
        //                    else
        //                    {
        //                        bNextCW = bList[j - 1];
        //                    }

        //                    if(aNext == bNextCW)
        //                    {
        //                        //paarweises Match gefunden => gemeinsame Kante
        //                        Log.Information("Match gefunden bei A1:" + aList[i] + ",B1: " + bList[j] + ",A2: " + aNext + ",B2: " + bNextCW);

        //                        var bColl = new List<int>();

        //                        if(j != bList.Count - 1)
        //                        {
        //                            bColl.AddRange(bList.GetRange(j + 1, bList.Count - j - 1));
        //                        }

        //                        bColl.AddRange(bList.GetRange(0, j));

        //                        //if(match2at0)
        //                        //    bColl.RemoveAt(0);

        //                        bColl.Remove(bNextCW);

        //                        compVerts.AddRange(aList);
        //                        compVerts.InsertRange(insertInd, bColl);

        //                        Log.Information("new Vertexlist: ");

        //                        foreach(var v in compVerts)
        //                        {
        //                            Log.Information(v.ToString());
        //                        }

        //                        match = true;
        //                    }
        //                }
        //                if(match)
        //                    break;
        //            }
        //            if(match)
        //                break;
        //        }
        //        return compVerts;
        //    }
        //    catch
        //    {
        //        Log.Error("error in compoundPlane" + a.ID + " , " + b.ID);

        //        return null;
        //    }
        //}



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