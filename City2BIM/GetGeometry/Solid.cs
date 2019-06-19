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

        public void AddPlane(string id, List<XYZ> polygon)
        {
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
                        vertices[j].AddPlane(id);
                        verts.Add(j);
                        notmatched = false;
                        break;
                    }
                }

                if(notmatched)
                {
                    Vertex v = new Vertex(polygon[i], id);
                    verts.Add(vertices.Count);
                    vertices.Add(v);
                }

                //------------------------------------------------------------------------------

                normal += XYZ.CrossProduct(polygon[i - 1], polygon[i]);
                centroid += polygon[i];
            }

            planes.Add(id, new Plane(id, verts, XYZ.Normalized(normal), centroid / ((double)verts.Count)));
        }

        public void CalculatePositions()
        {
            foreach(Vertex v in vertices)
            {
                //Log.Information("Anzahl Ebenen pro Vertex = " + v.Planes.Count);


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
                            double skalar = Math.Abs(XYZ.ScalarProduct(p1.Normal, p2.Normal));

                            if(skalar < bestskalar)
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

        //public void RemoveWrongVertices()
        //{
        //    foreach(var errV in vertexErrors)
        //    {
        //        var match = from v in vertices
        //                    where v == errV
        //                    select v;

        //        vertices.Remove(match.Single());
        //    }
        //}

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