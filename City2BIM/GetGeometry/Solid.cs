using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using static City2BIM.Prop;

namespace City2BIM.GetGeometry
{
    public class Solid
    {
        private Dictionary<string, Plane> planes = new Dictionary<string, Plane>();
        private List<Vertex> vertices = new List<Vertex>();

        public void AddPlane(string id, string faceType, string ringType, List<XYZ> polygon)
        {
            var idDict = id + "_" + faceType + "_" + ringType;      //nur so unique!

            polygon.Add(polygon.First());       //Methode benötigt geschlossenes Polygon, daher wird Startpunkt hier wieder dem Ende hinzugefügt

            if(polygon.Count < 4)
            {
                Log.Error("Zu wenig Eckpunkte! (AddPlane()), Polygon falsch generiert. Anzahl Eckpunkte = " + polygon.Count);
            }

            XYZ normal = new XYZ(0, 0, 0);
            XYZ centroid = new XYZ(0, 0, 0);

            List<int> verts = new List<int>(polygon.Count);
            for(int i = 1; i < polygon.Count; i++)
            {
                //Prüfung zur Einhaltung der Punktreihenfolge und Redundanz von Punkten (S.69, MA)

                bool notmatched = true;
                for(int j = 0; j < vertices.Count; j++)
                {
                    double dist = XYZ.DistanceSq(polygon[i], vertices[j].Position);

                    if(dist < Distolsq)
                    {
                        vertices[j].AddPlane(idDict);
                        verts.Add(j);
                        notmatched = false;
                        break;
                    }
                }

                if(notmatched)
                {
                    Vertex v = new Vertex(polygon[i], idDict);
                    verts.Add(vertices.Count);
                    vertices.Add(v);
                }

                //------------------------------------------------------------------------------

                normal += XYZ.CrossProduct(polygon[i - 1], polygon[i]);

                //Log.Information("normale vor normierung: " + normal.X + ", " + normal.Y + ", " + normal.Z);

                centroid += polygon[i];
            }

            Plane.FaceType fType = Plane.FaceType.unknown;

            switch(faceType)
            {
                case ("GroundSurface"):
                    fType = Plane.FaceType.ground;
                    break;

                case ("WallSurface"):
                    fType = Plane.FaceType.wall;
                    break;

                case ("ClosureSurface"):
                    fType = Plane.FaceType.closure;
                    break;

                case ("RoofSurface"):
                    fType = Plane.FaceType.roof;
                    break;

                default:
                    break;
            }

            Plane.RingType rType = Plane.RingType.unknown;

            switch(ringType)
            {
                case ("exterior"):
                    rType = Plane.RingType.exterior;
                    break;

                case ("interior"):
                    rType = Plane.RingType.interior;
                    break;

                default:
                    break;
            }



            var plane = new Plane(id, verts, XYZ.Normalized(normal), centroid / ((double)verts.Count), fType, rType);

            //Log.Information("plane: " + id);

            //foreach (var v in plane.Vertices)
            //{
            //    Log.Information(v.ToString());
            //}

            planes.Add(idDict, plane);
        }

        private void CheckPlanes()
        {
            var remPlanes = new List<string>();
            var compPlanes = new List<Plane>();

            var vGr3 = (from v in vertices
                        where v.Planes.Count > 3
                        select v).ToList();

            HashSet<HashSet<string>> vEqPlanesL = new HashSet<HashSet<string>>();

            for(int i = 0; i < vGr3.Count() - 1; i++)     //Schleife durch alle Vertices mit mehr als 3 Ebenen
            {
                string[] vPlanesGr3 = vGr3[i].Planes.ToArray<string>();

                int vPlanesCt = vPlanesGr3.Length;

                for(int j = 0; j < vPlanesCt - 1; j++)     //Schleife durch alle Planes pro Vertex
                {
                    HashSet<string> eqPlanes = new HashSet<string>();

                    var p1 = planes[vPlanesGr3[j]];

                    bool vMatch = false;

                    for(int k = j + 1; k < vPlanesCt - 1; k++)     //Schleife durch alle Planes pro Vertex
                    {
                        var p2 = planes[vPlanesGr3[k]];

                        double skalar = Math.Abs(XYZ.ScalarProduct(p1.Normal, p2.Normal));
                        var degSkalar = Math.Acos(skalar) * (180 / Math.PI);

                        if(degSkalar < 1)
                        {
                            vMatch = true;
                            eqPlanes.Add(p2.ID);           //Hinzufügen des Planes der inneren Schleife

                            Log.Information("Equal Plane = " + p2.ID);
                            Log.Information("Normale, alte Ebene2 = " + p2.Normal.X + ", " + p2.Normal.Y + ", " + p2.Normal.Z);
                        }

                        if(vPlanesCt == 4 && vMatch)
                            break;          //wenn in Vertex match gefunden wurde und "nur" 4 Ebenen vorhanden sind, dann Abbruch der inneren Schleife
                    }   //Ende innere Schleife über planes in Vertex i

                    if(vMatch)
                    {
                        eqPlanes.Add(p1.ID);       //Hinzufügen des Planes der äußeren Schleife
                        vEqPlanesL.Add(eqPlanes);           //Hinzufügen der Kombination aus PlaneList und Vertex zu Dictionary

                        Log.Information("Equal Plane = " + p1.ID);
                        Log.Information("Normale, alte Ebene1 = " + p1.Normal.X + ", " + p1.Normal.Y + ", " + p1.Normal.Z);

                        if(vPlanesCt == 4)
                            break;
                    }
                }   //Ende äußere Schleife über planes in Vertex i
            }      // Ende Schleife über alle Vertices

            //Verareitung der Ergebnisse des Checks
            //Dictionary enthält Plane-Listen und zugehörigen Vertex (Vertex kann redundant sein!)

            //Betrachtung

            Log.Information("Ct List: " + vEqPlanesL.Count);

            foreach(var pl in vEqPlanesL)
            {
                var eqPl = pl.OrderByDescending(i => i).ToList();

                if(planes.ContainsKey(eqPl[0]) && planes.ContainsKey(eqPl[1]))
                {
                    var newVerts = CompoundPlanes(planes[eqPl[0]], planes[eqPl[1]]);

                    if(newVerts.Any())
                    {
                        var compPlane = CreateNewPlane(newVerts, eqPl[0], eqPl[1]);

                        planes.Remove(eqPl[0]);
                        planes.Remove(eqPl[1]);

                        if(eqPl.Count > 2)
                        {
                            for(int i = 2; i < eqPl.Count - 1; i++)
                            {
                                newVerts = CompoundPlanes(compPlane, planes[eqPl[i]]);
                                compPlane = CreateNewPlane(newVerts, compPlane.ID, eqPl[i]);

                                planes.Remove(eqPl[1]);

                                foreach(var v in vertices)
                                {
                                    if(v.Planes.Contains(eqPl[i]))
                                        v.Planes.Remove(eqPl[i]);
                                }
                            }
                        }

                        planes.Add(compPlane.ID, compPlane);

                        foreach(var v in vertices)
                        {
                            if(v.Planes.Contains(eqPl[0]))
                                v.Planes.Remove(eqPl[0]);

                            if(v.Planes.Contains(eqPl[1]))
                                v.Planes.Remove(eqPl[1]);
                        }

                        foreach(var i in newVerts)
                        {
                            vertices[i].AddPlane(compPlane.ID);
                        }
                    }
                }
            }
        }

        private Plane CreateNewPlane(List<int> newVerts, string idA, string idB)
        {
            XYZ centroid = new XYZ(0, 0, 0);
            XYZ normal = new XYZ(0, 0, 0);
            var xyzList = new List<XYZ>();

            foreach(var vInd in newVerts)
            {
                GetGeometry.XYZ vertPos = vertices[vInd].Position;
                xyzList.Add(vertPos);
            }

            xyzList.Add(xyzList.First());

            for(int m = 1; m < xyzList.Count; m++)
            {
                normal += XYZ.CrossProduct(xyzList[m - 1], xyzList[m]);
                centroid += xyzList[m];
            }

            var norm = XYZ.Normalized(normal);
            var compPlane = new Plane(idA + idB, newVerts, norm, centroid / ((double)newVerts.Count), Plane.FaceType.unknown, Plane.RingType.unknown);

            Log.Information("Normale, neue Ebene = " + norm.X + ", " + norm.Y + ", " + norm.Z);

            return compPlane;
        }

        private void RemoveVertices()
        {
            var vert2Pl = from v in vertices
                          where v.Planes.Count < 3
                          select v;

            var examInd = new List<int>();

            foreach(var v2Pl in vert2Pl)
            {
                var index = vertices.IndexOf(v2Pl);
                examInd.Add(index);
            }

            foreach(var ind in examInd)
            {
                foreach(var pla in planes)
                {
                    if(pla.Value.Vertices.Contains(ind))
                    {
                        var newVerts = pla.Value.Vertices.Where(val => val != ind).ToArray();

                        pla.Value.Vertices = newVerts;

                        //.............Redundanz, noch in Methode zusammenfassen, Ebenenparameter müssen nach Löschen neu bestimmt werden

                        Log.Information("Normale, noch nicht veränderte Ebene = " + pla.Value.Normal.X + ", " + pla.Value.Normal.Y + ", " + pla.Value.Normal.Z);

                        XYZ centroid = new XYZ(0, 0, 0);
                        XYZ normal = new XYZ(0, 0, 0);
                        var xyzList = new List<XYZ>();

                        foreach(var vInd in pla.Value.Vertices)
                        {
                            GetGeometry.XYZ vertPos = vertices[vInd].Position;
                            xyzList.Add(vertPos);
                        }

                        xyzList.Add(xyzList.First());

                        for(int m = 1; m < xyzList.Count; m++)
                        {
                            normal += XYZ.CrossProduct(xyzList[m - 1], xyzList[m]);
                            centroid += xyzList[m];
                        }

                        var norm = XYZ.Normalized(normal);

                        pla.Value.Normal = norm;
                        pla.Value.Centroid = centroid / ((double)newVerts.Length);
                        //var compPlane = new Plane(idA + idB, newVerts, norm, centroid / ((double)newVerts.Count));

                        Log.Information("Normale, veränderte Ebene = " + norm.X + ", " + norm.Y + ", " + norm.Z);

                        //...........................................
                    }
                }
            }

            vertices.RemoveAll(v => vert2Pl.Contains(v));
        }

        public void CalculatePositions()
        {
            Log.Information("Planes.Count vorher = " + planes.Count);

            foreach(var pl in planes.Values)
            {
                Log.Information("VPlane Id: " + pl.ID);

                foreach(var vert in pl.Vertices)
                {
                    Log.Information("Vertices: " + vert);
                }
            }

            Log.Information("Ebenencheck");
            Log.Information("--------------");

            //CheckPlanes();

            Log.Information("--------------");
            Log.Information("Planes.Count nachher = " + planes.Count);

            var a = vertices.Count;
            Log.Information("Vertices.Count vorher = " + vertices.Count);

            //RemoveVertices();

            var b = vertices.Count;
            Log.Information("Vertices.Count nachher = " + vertices.Count);

            if((a - b) != 0)
                Log.Information("Vertex nach EqualPlanes gelöscht!");

            foreach(var pl in planes.Values)
            {
                Log.Information("NPlane Id: " + pl.ID);

                foreach(var vert in pl.Vertices)
                {
                    Log.Information("Vertices: " + vert);
                }
            }

            foreach(Vertex v in vertices)
            {
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
                    XYZ vertex = new XYZ(0, 0, 0);

                    string[] vplanes = v.Planes.ToArray<string>();
                    Plane plane1 = planes[vplanes[0]];
                    Plane plane2 = planes[vplanes[1]];
                    Plane plane3 = planes[vplanes[2]];

                    double determinant = XYZ.ScalarProduct(plane1.Normal, XYZ.CrossProduct(plane2.Normal, plane3.Normal));

                    if(Math.Abs(determinant) > Determinanttol)
                    {
                        XYZ pos = (XYZ.CrossProduct(plane2.Normal, plane3.Normal) * XYZ.ScalarProduct(plane1.Centroid, plane1.Normal) +
                                   XYZ.CrossProduct(plane3.Normal, plane1.Normal) * XYZ.ScalarProduct(plane2.Centroid, plane2.Normal) +
                                   XYZ.CrossProduct(plane1.Normal, plane2.Normal) * XYZ.ScalarProduct(plane3.Centroid, plane3.Normal)) /
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

                    XYZ vertex = new XYZ(0, 0, 0);

                    string[] vplanes = v.Planes.ToArray<string>();
                    double bestskalar = 1;
                    Plane plane1 = planes[vplanes[0]];
                    Plane plane2 = planes[vplanes[0]];
                    Plane plane3 = planes[vplanes[0]];

                    for(int i = 0; i < v.Planes.Count - 1; i++)
                    {
                        Plane p1 = planes[vplanes[i]];

                        for(int j = i + 1; j < v.Planes.Count; j++)
                        {
                            Plane p2 = planes[vplanes[j]];

                            //Log.Information("Normalen, p1: " + p1.Normal.X + " , " + p1.Normal.Y + " , " + p1.Normal.Z);
                            //Log.Information("Normalen, p2: " + p2.Normal.X + " , " + p2.Normal.Y + " , " + p2.Normal.Z);

                            double skalar = Math.Abs(XYZ.ScalarProduct(p1.Normal, p2.Normal));

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
                                vertex = XYZ.CrossProduct(plane1.Normal, plane2.Normal);
                            }
                        }
                    }

                    for(int k = 0; k < v.Planes.Count; k++)
                    {
                        Plane p3 = planes[vplanes[k]];
                        double skalar = Math.Abs(XYZ.ScalarProduct(vertex, p3.Normal));
                        if(skalar > bestskalar)
                        {
                            plane3 = p3;
                        }
                    }

                    double determinant = XYZ.ScalarProduct(plane1.Normal, XYZ.CrossProduct(plane2.Normal, plane3.Normal));

                    if(Math.Abs(determinant) > Determinanttol)
                    {
                        XYZ pos = (XYZ.CrossProduct(plane2.Normal, plane3.Normal) * XYZ.ScalarProduct(plane1.Centroid, plane1.Normal) +
                                   XYZ.CrossProduct(plane3.Normal, plane1.Normal) * XYZ.ScalarProduct(plane2.Centroid, plane2.Normal) +
                                   XYZ.CrossProduct(plane1.Normal, plane2.Normal) * XYZ.ScalarProduct(plane3.Centroid, plane3.Normal)) /
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

        private List<int> CompoundPlanes(Plane a, Plane b)
        {
            try
            {
                foreach(var i in a.Vertices)
                {
                    Log.Information("A: " + i.ToString());
                }

                foreach(var i in b.Vertices)
                {
                    Log.Information("B: " + i.ToString());
                }

                var aList = a.Vertices.ToList();
                var bList = b.Vertices.ToList();

                bool match = false;

                List<int> compVerts = new List<int>();

                for(int i = 0; i < aList.Count; i++)      //paarweise Match gesucht
                {
                    for(int j = bList.Count - 1; j >= 0; j--)
                    {
                        if(aList[i] == bList[j]) //1.Match
                        {
                            //jeweilige Nachfolger müssen auch matchen für gleiche Kante
                            //aber Achtung wenn Anfang oder Ende der Liste:
                            //Anfang: könnte nicht 1. Match, sondern 2.Matchpoint sein --> 1.Matchpoint steht dann am Ende
                            //Ende: zu untersuchender Nachfolge steht am Anfang und wurde schon gefunden, fälschlicherweise als 1. Match

                            int aNext = 0;
                            int insertInd = 0;

                            if(i == aList.Count - 1)
                            {
                                aNext = aList[0];
                            }
                            else
                            {
                                aNext = aList[i + 1];
                                insertInd = i + 1;
                            }

                            int bNextCW = 0;
                            //bool match2at0 = false;

                            if(j == 0)
                            {
                                bNextCW = bList[bList.Count - 1];
                                //match2at0 = true;
                            }
                            else
                            {
                                bNextCW = bList[j - 1];
                            }

                            if(aNext == bNextCW)
                            {
                                //paarweises Match gefunden => gemeinsame Kante
                                Log.Information("Match gefunden bei A1:" + aList[i] + ",B1: " + bList[j] + ",A2: " + aNext + ",B2: " + bNextCW);

                                var bColl = new List<int>();

                                if(j != bList.Count - 1)
                                {
                                    bColl.AddRange(bList.GetRange(j + 1, bList.Count - j - 1));
                                }

                                bColl.AddRange(bList.GetRange(0, j));

                                //if(match2at0)
                                //    bColl.RemoveAt(0);

                                bColl.Remove(bNextCW);

                                compVerts.AddRange(aList);
                                compVerts.InsertRange(insertInd, bColl);

                                Log.Information("new Vertexlist: ");

                                foreach(var v in compVerts)
                                {
                                    Log.Information(v.ToString());
                                }

                                match = true;
                            }
                        }
                        if(match)
                            break;
                    }
                    if(match)
                        break;
                }
                return compVerts;
            }
            catch
            {
                Log.Error("error in compoundPlane" + a.ID + " , " + b.ID);

                return null;
            }
        }

        public List<Vertex> Vertices
        {
            get { return vertices; }
        }

        public Dictionary<string, Plane> Planes
        {
            get { return planes; }
        }

        public void ClearSolid()
        {
            planes.Clear();
            vertices.Clear();
        }

        public ReadGeomData ReadData
        {
            get => default(ReadGeomData);
            set
            {
            }
        }
    }
}