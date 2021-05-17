using BimGisCad.Representation.Geometry;
using BimGisCad.Representation.Geometry.Elementary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Linq;
using BimGisCad.Representation.Geometry.Composed;

namespace BimGisCad.Collections
{

    /// <summary>
    /// Klasse für 2D Netze aufgebaut mit Halbkanten
    /// </summary>
    public class MeshHe2
    {

        /// <summary>
        /// Halbkante
        /// </summary>
        public class HEdge
        {
            private HEdge next, prev, twin;
            //private Face face;

            public int Point
            {
                get; set;
            }

            public HEdge Next
            {
                get { return next; }
                set
                {
                    next = value;
                    next.prev = this;
                }
            }

            public override string ToString()
            {
                string str = "(" + Point + ", ";
                var curr = this.next;
                while(curr != null && curr != this)
                {
                    str += curr.Point + ", ";
                    curr = curr.next;
                }
                str.Remove(str.Length - 2, 2);
                str += ")";
                return str;
                //return Next == null ? $"({Point}-)" : $"({Point}-{Next.Point})";
            }

            public HEdge Prev
            {
                get { return prev; }
                set
                {
                    prev = value;
                    prev.next = this;
                }
            }

            public HEdge Twin
            {
                get { return twin; }
                set
                {
                    twin = value;
                    if (value != null)
                    { twin.twin = this; }
                }
            }
        }

        /// <summary>
        /// Polygon
        /// </summary>
        public class Polygon
        {
            /// <summary>
            /// Äußere Begrenzung
            /// </summary>
            public HEdge Exterior { get; set; }

            /// <summary>
            /// Innere Begrenzungen (Löcher)
            /// </summary>
            public List<HEdge> Interiors { get; } = new List<HEdge>();
        }

        private readonly double minDist;

        /// <summary>
        /// Punkte
        /// </summary>
        public List<Point2> Points { get; }

        /// <summary>
        /// Alle Abgehenden Kanten des Punktes
        /// </summary>
        public List<HashSet<HEdge>> PointHEdges { get; }

        /// <summary>
        /// Polygone
        /// </summary>
        public List<Polygon> Polygons { get; }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="minDist">Mindestabstand unterschiedlicher Punkte</param>
        private MeshHe2(double minDist)
        {
            this.minDist = minDist;
            Points = new List<Point2>();
            PointHEdges = new List<HashSet<HEdge>>();
            Polygons = new List<Polygon>();
        }

        /// <summary>
        /// Erzeugt Mesh aus Umringen
        /// </summary>
        /// <param name="minDist">Mindestabstand von Punkten</param>
        /// <param name="rings">Umringe</param>
        /// <param name="removeTwins">Doppelte Kanten entfernen</param>
        /// <returns>Erfolgreich?</returns>
        public static MeshHe2 Create(double minDist, IReadOnlyList<IReadOnlyList<Point2>> rings, bool removeTwins)
        {

            if (rings == null ||  rings.Count == 0)
            { throw new Exception("Keine Umringe vorhanden");  }

            var tpointHEdges = new List<HashSet<HEdge>>();
            var tpoints = new List<Point2>();


            // Ringe einlesen und verarbeiten
            {
                var mpoints = new List<MultiPoint2>();

                // Hilfsfunktion fügt Punkt gemittelt hinzu
                Func<Point2, int> addPoint = (point) =>
                {
                    int pi = mpoints.Count;
                    for (int i = 0; i < mpoints.Count; i++)
                    {
                        if (mpoints[i].Add(point))
                        {
                            pi = i;
                            break;
                        }
                    }
                    if (pi == mpoints.Count)
                    {
                        mpoints.Add(new MultiPoint2(minDist, point));
                        tpointHEdges.Add(new HashSet<HEdge>());
                    }
                    return pi;
                };

                foreach (var ring in rings)
                {
                    if (ring == null || ring.Count < 3)
                    {
                        throw new Exception("Umring hat weniger als 3 Punkte");
                    }

                    // Kanten anlegen
                    var it = ring.GetEnumerator();
                    it.MoveNext();
                    var first = new HEdge { Point = addPoint(it.Current) };
                    HEdge prev = first, curr, next;
                    while (it.MoveNext())
                    {
                        curr = new HEdge { Point = addPoint(it.Current) };
                        prev.Next = curr;
                        prev = curr;
                    }
                    prev.Next = first;

                    // evtl. identische Kanten bereinigen
                    {
                        curr = first;
                        do
                        {
                            next = curr.Next;
                            if (next.Point == curr.Point)
                            {
                                curr.Next = next.Next;
                                if (next == first)
                                {
                                    first = curr;
                                    break;
                                }
                            }
                            curr = curr.Next;
                        } while (curr != first);
                    }
                    // evtl. gerade Kanten bereinigen
                    {
                        prev = first.Prev;
                        curr = first;
                        var prevp = mpoints[prev.Point].Point;
                        var currp = mpoints[curr.Point].Point;
                        int cnt = 0;
                        do
                        {
                            cnt++;
                            next = curr.Next;
                            var nextp = mpoints[next.Point].Point;
                            double d = Math.Abs(Point2.SignedDistance(prevp, nextp, currp));
                            if (d < minDist)
                            {
                                prev.Next = next;
                                if (curr == first)
                                { first = prev; }
                                cnt--;
                            }
                            else
                            {
                                prev = curr;
                                prevp = currp;
                            }
                            currp = nextp;
                            curr = next;
                        } while (curr != first);
                        if (cnt < 3)
                        {
                            throw new Exception("Bereinigter Umring hat weniger als 3 Punkte");
                        }
                    }

                    // Richtung bestimmen, linke untere Ecke muss ein konvexer Knick sein
                    {
                        var lue = first;
                        var lup = mpoints[first.Point];
                        curr = first.Next;
                        while (curr != first)
                        {
                            var p = mpoints[curr.Point];
                            if (p.Y < lup.Y || (p.Y == lup.Y && p.X < lup.X))
                            {
                                lue = curr;
                                lup = p;
                            }
                            curr = curr.Next;
                        }
                        var prevp = mpoints[lue.Prev.Point].Point;
                        if (Vector2.Det(lup.Point - prevp, mpoints[lue.Next.Point].Point - prevp) < 0.0)
                        {
                            // umdrehen
                            var tfirst = new HEdge { Point = first.Point };
                            prev = tfirst;
                            curr = first.Prev;
                            while (curr != first)
                            {
                                prev.Next = new HEdge { Point = curr.Point };
                                prev = prev.Next;
                                curr = curr.Prev;
                            }
                            prev.Next = tfirst;
                            first = tfirst;
                        }
                    }

                    // prüfen ob Kante schon vorhanden, Twin hinzu
                    {
                        curr = first;
                        do
                        {
                            if (GetHEdge(tpointHEdges, curr.Point, curr.Next.Point) != null)
                            {
                                throw new Exception("Kante schon vorhanden");
                            }
                            tpointHEdges[curr.Point].Add(curr);
                            curr.Twin = GetHEdge(tpointHEdges, curr.Next.Point, curr.Point);
                            curr = curr.Next;
                        } while (curr != first);
                    }

                }
                // hier müsste evtl. noch ein Check auf Überlappung hinein, da das nicht garantiert werden kann

                tpoints.AddRange(mpoints.Select(m => m.Point));
            }


            // evtl. Punkte auf Kanten suchen (Anfangspunkt muss gleich bleiben)
            {
                for (int i = 0; i < tpointHEdges.Count; i++)
                {
                    var start = tpoints[i];
                    foreach (var he in tpointHEdges[i])
                    {
                        var endi = he.Next.Point;
                        var segment = Segment2.Create(start, tpoints[endi]);
                        for (int j = 0; j < tpoints.Count; j++)
                        {
                            var mid = tpoints[j];
                            if (!tpointHEdges[j].Any() ||  i == j || endi == j || Math.Abs(Segment2.CrossDistance(segment, mid)) >= minDist) { continue; }
                            var d = Segment2.LineDistance(segment, mid);
                            if (d < 0.0 || d > segment.Length) { continue; }
                            // Kante teilen
                            var nhe = new HEdge { Point = j };
                            he.Next.Prev = nhe;
                            he.Next = nhe;
                            tpointHEdges[j].Add(nhe);
                            // evtl. Twin teilen
                            if (he.Twin != null)
                            {
                                var tnhe = new HEdge { Point = j };
                                he.Twin.Next.Prev = tnhe;
                                he.Twin.Next = tnhe;
                                he.Twin.Twin = nhe;
                                he.Twin = tnhe;
                                tpointHEdges[j].Add(tnhe);
                            }
                            // evtl. vorhandene Twinkante zuweisen
                            else
                            { nhe.Twin = GetHEdge(tpointHEdges, nhe.Next.Point, j); }
                        }
                    }
                }
            }

            // Twins entfernen
            if (removeTwins)
            {
                Action<HEdge> mergeStraight = (edge) =>
                {
                    if (edge == null)
                    { return; }

                    var next = edge.Next;
                    int p1 = edge.Point;
                    int p2 = next.Next.Point;
                    int pm = next.Point;
                    bool hasTwin = edge.Twin is HEdge;

                    // Darf nur einen (durchgängigen) oder keinen Nachbarn haben, sowie keine sonstigen abgehenden Kanten
                    // Abstand von verbleibender Kante muss < mindist sein
                    if ((hasTwin && edge.Twin.Prev != next.Twin)
                        || (!hasTwin && tpointHEdges[pm].Count > 1)
                        || Math.Abs(Point2.SignedDistance(tpoints[p1], tpoints[p2], tpoints[pm])) >= minDist)
                    { return; }

                    // mit Next.Next verbinden
                    edge.Next = next.Next;

                    // Next entfernen
                    tpointHEdges[pm].Remove(next);
                    next = null;

                    if (hasTwin)
                    {
                        var twin = edge.Twin;
                        var tprev = twin.Prev;

                        // mit Twin.Prev verbinden
                        tprev.Next = twin.Next;

                        // twin entfernen
                        tpointHEdges[pm].Remove(twin);
                        twin = null;

                        edge.Twin = tprev;
                    }
                };

                var twins = tpointHEdges.SelectMany(tp => tp.Where(he => he.Twin != null)).ToArray();
                var ignore = new HashSet<HEdge>();
                foreach (var he in twins)
                {
                    if (ignore.Contains(he)) {
                        continue;
                    }
                    var twin = he.Twin;
                    var prev = he.Prev;
                    var tprev = twin.Prev;

                    // Reihenfolge anpassen
                    prev.Next = twin.Next;
                    tprev.Next = he.Next;

                    // Kanten entfernen
                    tpointHEdges[he.Point].Remove(he);
                    tpointHEdges[twin.Point].Remove(twin);
                    ignore.Add(twin);
 
                    // verbleibende evtl. Kanten bereinigen
                    mergeStraight(prev);
                    mergeStraight(tprev);
                }
            }

            // neue Meshdaten ohne ungenutzte Punkte
            {
                int last = tpoints.Count - 1;
                while (!tpointHEdges.Any())
                { last--; }

                for (int i = 0; i < last; i++)
                {
                    if(!tpointHEdges.Any())
                    {
                        foreach (var he in tpointHEdges[last])
                        {
                            he.Point = i;
                        }
                        tpoints[i] = tpoints[last];

                        last--;
                        while (!tpointHEdges.Any())
                        { last--; }
                    }
                }

                tpoints.RemoveRange(last + 1, tpoints.Count - last - 1);
                tpointHEdges.RemoveRange(last + 1, tpointHEdges.Count - last - 1);
            }
            var mesh = new MeshHe2(minDist);
            mesh.Points.AddRange(tpoints);
            mesh.PointHEdges.AddRange(tpointHEdges);

            // abschließend Polygone finden und sicherstellen das sich keine Kanten überlappen
            {
                // Anfangspunkte suchen
                var used = new HashSet<HEdge>();
                // Äussere Umringe suchen       
                var interiors = new List<HEdge>();
                foreach (var pedges in mesh.PointHEdges)
                {
                    foreach (var he in pedges)
                    {
                        if (used.Contains(he))
                        { continue; }
                        // Richtung bestimmen
                        var lue = he;
                        var lup = mesh.Points[he.Point];
                        var curr = he.Next;
                        used.Add(he);
                        while (curr != he)
                        {
                            used.Add(curr);
                            var p = mesh.Points[curr.Point];
                            if (p.Y < lup.Y || (p.Y == lup.Y && p.X < lup.X))
                            {
                                lue = curr;
                                lup = p;
                            }
                            curr = curr.Next;
                        }
                        var pp = mesh.Points[lue.Prev.Point];
                        if (Vector2.Det(lup - pp, mesh.Points[lue.Next.Point] - pp) > 0.0)
                        {
                            mesh.Polygons.Add(new Polygon { Exterior = he });
                        }
                        else
                        {
                            interiors.Add(he);
                        }
                    }

                    foreach (var poly in mesh.Polygons)
                    {
                        // Umring anlegen
                        var spts = new List<Point2>(); // Startpunkte
                        var vecs = new List<Vector2>(); // Vektoren der Kanten
                        var curr = poly.Exterior;
                        do
                        {
                            var p = mesh.Points[curr.Point];
                            spts.Add(p);
                            vecs.Add(mesh.Points[curr.Next.Point] - p);
                            curr = curr.Next;
                        } while (curr != poly.Exterior);
                        spts.Add(spts[0]);

                        // Innenringe prüfen ob innen
                        for (int i = interiors.Count - 1; i >= 0; i--)
                        {
                            var he = interiors[i];
                            curr = he;
                            bool inside = true;
                            do
                            {
                                // Winding Number Algorithmus
                                int wn = 0;
                                var p = mesh.Points[curr.Point];
                                for (int j = 0; j < vecs.Count; j++)
                                {
                                    var s = spts[j];
                                    var e = spts[j + 1];
                                    if (s.Y <= p.Y)
                                    {
                                        if (e.Y > p.Y && Vector2.Det(vecs[j], p - s) > 0.0)
                                        { wn++; }
                                    }
                                    else if (e.Y <= p.Y && Vector2.Det(vecs[j], p - s) < 0.0)
                                    { wn--; }
                                }
                                inside = wn > 0;
                                curr = curr.Next;
                            } while (curr != he && inside);
                            // alle Punkte innerhalb, zu Polygon hinzu, aus Liste entfernen
                            if (inside)
                            {
                                poly.Interiors.Add(he);
                                interiors.RemoveAt(i);
                            }
                        }
                    }
                }
            }


            return mesh;
        }

        /// <summary>
        /// Halbkante zwischen Punkten oder null
        /// </summary>
        /// <param name="pointHEdges"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static HEdge GetHEdge(IReadOnlyList<HashSet<HEdge>> pointHEdges,  int p1, int p2)
        {
            foreach (var he in pointHEdges[p1])
            {
                if(he.Next.Point == p2)
                {
                    return he;
                }
            }
            return null;
        }


        ///// <summary>
        ///// Schreibt mesh in simple OBJ-Datei
        ///// </summary>
        ///// <param name="shiftOrigin">Ursprung auf linke untere Ecke setzen</param>
        ///// <param name="fileName">Dateiname</param>
        //public void WriteObj(bool shiftOrigin, string fileName)
        //{
        //    var ic = CultureInfo.InvariantCulture;
        //    using (StreamWriter obj = File.CreateText(fileName + ".obj"))
        //    {
        //        foreach (Point2 p in Points)
        //        {
        //            obj.WriteLine(string.Format(ic, "v {0:f} {1:f} 0.0", p.X, p.Y));
        //        }

        //        obj.WriteLine();
        //        foreach (var face in Faces)
        //        {
        //            StringBuilder sb = new StringBuilder();
        //            var he = face.FirstHEdge;
        //            int cnt = int.MaxValue;
        //            do
        //            {
        //                sb.Append(" " + (he.Point + 1));
        //                he = he.Next;
        //            } while (he != face.FirstHEdge && cnt-- > 0);
        //            if (cnt < 0)
        //            { throw new Exception("Face not closed"); }

        //            obj.WriteLine("f" + sb.ToString());
        //            sb.Append(" " + (face.FirstHEdge.Point + 1));
        //            obj.WriteLine("l" + sb.ToString());
        //        }

        //        obj.WriteLine();

        //    }
        //}

        ///// <summary>
        ///// Schreibt mesh in simple VTK-Datei
        ///// </summary>
        ///// <param name="fileName">Dateiname</param>
        //public void WriteVtk(string fileName)
        //{
        //    var ic = CultureInfo.InvariantCulture;
        //    using (StreamWriter vtk = File.CreateText(fileName + ".vtk"))
        //    {
        //        vtk.WriteLine("# vtk DataFile Version 3.0");
        //        vtk.WriteLine("vtk output");
        //        vtk.WriteLine("ASCII");
        //        vtk.WriteLine("DATASET POLYDATA");
        //        vtk.WriteLine($"POINTS {Points.Count} double");

        //        foreach (Point2 p in Points)
        //        {
        //            vtk.WriteLine(string.Format(ic, "{0:f} {1:f} 0.0", p.X, p.Y));
        //        }

        //        int numpoly = 0;
        //        int numdat = 0;
        //        StringBuilder sb = new StringBuilder();
        //        foreach (var poly in Polygons)
        //        {
        //            foreach(var inter in poly.Interiors)
        //            {
        //                numpoly++;
        //                numdat++;
        //                int cnt = 0;
        //                var he = inter;
        //                string zeile = "";
        //                do
        //                {
        //                    zeile += " " + he.Point;
        //                    he = he.Prev;
        //                    cnt++;
        //                    numdat++;
        //                } while (he != inter);
        //                sb.Append(cnt);
        //                sb.AppendLine(zeile);
        //            }
        //        }
        //        vtk.WriteLine($"POLYGONS {numpoly} {numdat}");
        //        vtk.Write(sb.ToString());

        //        numpoly = 0;
        //        numdat = 0;
        //        sb.Clear();
        //        foreach (var poly in Polygons)
        //        {
        //                numpoly++;
        //                numdat++;
        //                int cnt = 0;
        //                var he = poly.Exterior;
        //                string zeile = "";
        //                do
        //                {
        //                    zeile += " " + he.Point;
        //                    he = he.Next;
        //                    cnt++;
        //                    numdat++;
        //                } while (he != poly.Exterior);
        //                sb.Append(cnt);
        //                sb.AppendLine(zeile + " " + poly.Exterior.Point);
        //        }
        //        vtk.WriteLine($"LINES {numpoly} {numdat}");
        //        vtk.Write(sb.ToString());
        //    }
        //}

        /// <summary>
        /// Schreibt mesh in einfache SVG-Datei
        /// </summary>
        /// <param name="fileName">Dateiname</param>
        /// <param name="fill"></param>
        /// <param name="stroke"></param>
        /// <param name="stroke_width"></param>
        public void WriteSvg(string fileName, 
            string fill = "green", 
            string stroke = "black", 
            string stroke_width = "1")
        {
            double minx = double.MaxValue, maxx = -double.MaxValue,
                miny = double.MaxValue, maxy = -double.MaxValue;
            foreach (var p in Points)
            {
                if (p.X < minx)
                { minx = p.X; }
                if (p.X > maxx)
                { maxx = p.X; }
                if (p.Y < miny)
                { miny = p.Y; }
                if (p.Y > maxy)
                { maxy = p.Y; }
            }
            int width = (int)Math.Ceiling((maxx - minx) / minDist);
            int height = (int)Math.Ceiling((maxy - miny) / minDist);

            var document = new XmlDocument();
            var root = document.CreateElement("svg");
            document.AppendChild(root);
            root.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
            root.SetAttribute("width", width.ToString());
            root.SetAttribute("height", height.ToString());
            root.SetAttribute("stroke", stroke);
            root.SetAttribute("fill", fill);
            root.SetAttribute("stroke-width", stroke_width);

            foreach (var poly in Polygons)
            {
                var pa = document.CreateElement("path");
                pa.SetAttribute("fill-rule", "nonzero");//nonzero?
                StringBuilder sb = new StringBuilder("M");
                var he = poly.Exterior;
                do
                {
                    var p = Points[he.Point];
                    int x = (int)Math.Round((p.X - minx) / minDist);
                    int y = (int)Math.Round((maxy - p.Y) / minDist);
                    sb.Append(x + " " + y + " L");
                    he = he.Next;
                } while (he != poly.Exterior);
                sb[sb.Length - 1] = 'Z';
                foreach (var first in poly.Interiors)
                {
                    sb.AppendLine();
                    sb.Append("M");
                    he = first;
                    do
                    {
                        var p = Points[he.Point];
                        int x = (int)Math.Round((p.X - minx) / minDist);
                        int y = (int)Math.Round((maxy - p.Y) / minDist);
                        sb.Append(x + " " + y + " L");
                        he = he.Next;
                    } while (he != first);
                    sb[sb.Length - 1] = 'Z';
                }
                pa.SetAttribute("d", sb.ToString());
                root.AppendChild(pa);
            }

            document.Save(fileName + ".svg");
        }
    }
}
