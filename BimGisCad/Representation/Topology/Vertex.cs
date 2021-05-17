using System;
using System.Collections.Generic;
using System.Linq;

using BimGisCad.Representation.Geometry;
using BimGisCad.Representation.Geometry.Elementary;

namespace BimGisCad.Representation.Topology
{
    ///// <summary>
    ///// Vertex
    ///// </summary>
    //public struct Vertex:ITopoItem
    //{
    //    internal Vertex(int idx, IEnumerable<int> incident)
    //    {
    //        this.Idx = idx;
    //        this.Incident = incident?.ToArray() ?? Array.Empty<int>();
    //    }

    //    /// <summary>
    //    /// Index in Shell
    //    /// </summary>
    //    public int Idx { get; }

    //    /// <summary>
    //    /// Indizes der abgehenden Kanten in Shell
    //    /// </summary>
    //    public int[] Incident { get; }

    //    /// <summary>
    //    ///  Erzeugt neuen Vertex
    //    /// </summary>
    //    /// <param name="shell">  </param>
    //    internal static int Create(Shell shell)
    //    {
    //        int idx = shell.Vertices.Count;
    //        shell.Vertices.Add(new Vertex(idx, null));
    //        return idx;
    //    }

    //    internal static bool AddIncident(Shell shell, int idx, int edgeIdx)
    //    {
    //        var vtx = shell.Vertices[idx];
    //        if(!vtx.Incident.Contains(edgeIdx))
    //        {
    //            // ein Vertex darf nur zweimal in einem Face verwendet werden
    //            if(vtx.Incident.Count(ei => shell.Edges[ei].FaceIdx == shell.Edges[edgeIdx].FaceIdx) > 1)
    //            { return false; }
    //            var cis = vtx.Incident.ToList();
    //            cis.Add(edgeIdx);
    //            shell.Vertices[idx] = new Vertex(idx, cis);
    //        }
    //        return true;
    //    }

    //    internal static void RemoveIncident(Shell shell, int idx, int edgeIdx)
    //    {
    //        var vtx = shell.Vertices[idx];
    //        int i = Array.IndexOf(vtx.Incident, edgeIdx);
    //        if(i >= 0)
    //        {
    //            var cis = vtx.Incident.ToList();
    //            cis.RemoveAt(i);
    //            shell.Vertices[idx] = new Vertex(idx, cis);
    //        }
    //    }
    //}
}