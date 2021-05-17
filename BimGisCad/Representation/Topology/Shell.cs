using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BimGisCad.Collections;
using BimGisCad.Representation.Geometry;
using BimGisCad.Representation.Geometry.Elementary;

namespace BimGisCad.Representation.Topology
{
    ///// <summary>
    ///// Hülle eines 3D Körpers oder 2D Fläche
    ///// der generische Typ wird für die Bestimmung der Dimension und 
    ///// </summary>
    //public class Shell
    //{
    //    private Shell(IList<IPoint> points)
    //    {
    //        this.Vertices = Enumerable.Range(0, points.Count).Select(i => new Vertex(i, null)).ToList();
    //        this.Edges = new List<Edge>();
    //        this.Faces = new List<Face>();
    //        this.Points = points.ToList();
    //    }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public List<Vertex> Vertices { get; }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public List<Edge> Edges { get; }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public List<Face> Faces { get; }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public List<IPoint> Points { get; }

    //    /// <summary>
    //    /// Prüft ob die Shell geschlossen ist
    //    /// </summary>
    //    /// <returns></returns>
    //    public bool IsClosed()
    //    {
    //        foreach(var face in this.Faces)
    //        {
    //            if(!face.IsContiguous(this) || !face.IsClosed(this))
    //            { return false; }
    //        }
    //        foreach(var edge in this.Edges)
    //        {
    //            if(!edge.TwinIdx.HasValue)
    //            { return false; }
    //        }
    //        return true;
    //    }

    //    /// <summary>
    //    /// Erzeugt Neue Shell aus Liste von Punktlisten, in den Punktlisten muss Anfangs- und Endpunkt identisch sein
    //    /// </summary>
    //    /// <param name="faces"></param>
    //    /// <param name="shell"></param>
    //    /// <returns></returns>
    //    public static bool CreateBRep(IEnumerable<IEnumerable<IPoint>> faces, out Shell shell)
    //    {
    //        var points = new List<IPoint>();
    //        var faceids = new List<List<int>>();
    //        foreach(var face in faces)
    //        {
    //            var fis = new List<int>();
    //            foreach(var point in face)
    //            {
    //                int idx = -1;
    //                for(int i = 0; idx < 0 && i < points.Count; i++)
    //                {
    //                    if(points[i].Coincident(point))
    //                    { idx = i; }
    //                }
    //                if(idx < 0)
    //                {
    //                    idx = points.Count;
    //                    points.Add(point);
    //                }
    //                fis.Add(idx);
    //            }
    //            if(fis.Count < 4 || fis.First() != fis.Last())
    //            {
    //                shell = null;
    //                return false;
    //            }
    //            faceids.Add(fis);
    //        }

    //        if(faceids.Count > 0)
    //        {
    //            shell = new Shell(points);
    //            foreach(var fi in faceids)
    //            {
    //                int f = Face.Create(shell);
    //                if(Face.AddFirstEdge(shell, f, fi[0], fi[1]))
    //                {
    //                    for(int i = 2; i < fi.Count; i++)
    //                    {
    //                        if(!Face.AddVertex(shell, f, fi[i]))
    //                        {
    //                            shell = null;
    //                            return false;
    //                        }
    //                    }
    //                }
    //            }
    //            return true;
    //        }

    //        shell = null;
    //        return false;
    //    }
    //}
}
