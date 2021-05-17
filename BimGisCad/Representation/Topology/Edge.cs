using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BimGisCad.Representation.Geometry;
using BimGisCad.Representation.Geometry.Elementary;

namespace BimGisCad.Representation.Topology
{
    ///// <summary>
    ///// 
    ///// </summary>
    //public struct Edge
    //{
    //    private Edge(int idx, int faceIdx, int startIdx, int endIdx, int? prevIdx, int? nextIdx, int? twinIdx)
    //    {
    //        this.Idx = idx;
    //        this.FaceIdx = faceIdx;
    //        this.StartIdx = startIdx;
    //        this.EndIdx = endIdx;
    //        this.PrevIdx = prevIdx;
    //        this.NextIdx = nextIdx;
    //        this.TwinIdx = twinIdx;
    //    }

    //    /// <summary>
    //    /// Index in Shell
    //    /// </summary>
    //    public int Idx { get; }

    //    /// <summary>
    //    /// Index des Faces in Shell
    //    /// </summary>
    //    public int FaceIdx { get; }

    //    /// <summary>
    //    /// Index des Startvertex in Shell
    //    /// </summary>
    //    public int StartIdx { get; }

    //    /// <summary>
    //    /// Index des Endvertex in Shell
    //    /// </summary>
    //    public int EndIdx { get; }

    //    /// <summary>
    //    /// Index der vorherigen Kante des Faces in Shell
    //    /// </summary>
    //    public int? PrevIdx { get; }

    //    /// <summary>
    //    /// Index der nachfolgenden Kante des Faces in Shell
    //    /// </summary>
    //    public int? NextIdx { get; }

    //    /// <summary>
    //    /// Index des Twins in Shell
    //    /// </summary>
    //    public int? TwinIdx { get; }

    //    /// <summary>
    //    /// Erzeugt neue Kante, nur aus Face aufrufen!
    //    /// </summary>
    //    /// <param name="shell"></param>
    //    /// <param name="faceIdx"></param>
    //    /// <param name="startIdx"></param>
    //    /// <param name="endIdx"></param>
    //    /// <returns></returns>
    //    internal static int? Create(Shell shell, int faceIdx, int startIdx, int endIdx)
    //    {
    //        if(startIdx == endIdx)
    //        { return null; }
    //        // evtl. Vorhandene Kante suchen, eine Kante darf nur einmal vorhanden sein
    //        if(shell.Vertices[startIdx].Incident.Any(ei =>
    //        {
    //            var e = shell.Edges[ei];
    //            return e.StartIdx == startIdx && e.EndIdx == endIdx;
    //        }))
    //        { return null; }

    //        int idx = shell.Edges.Count;

    //        // Kante den Vertices hinzufügen
    //        Vertex.AddIncident(shell, startIdx, idx);
    //        Vertex.AddIncident(shell, endIdx, idx);

    //        // erzeugen
    //        var edge = new Edge(idx, faceIdx, startIdx, endIdx, null, null, null);
    //        shell.Edges.Add(edge);

    //        return idx;
    //    }

    //    ///// <summary>
    //    ///// Ändert Startvertex, Achtung am Ende Connect aufrufen, da alle Verknüpfungen entfernt werden
    //    ///// </summary>
    //    ///// <param name="shell"></param>
    //    ///// <param name="idx"></param>
    //    ///// <param name="startIdx"></param>
    //    ///// <returns></returns>
    //    //internal static bool SetStart(Shell shell, int idx, int startIdx)
    //    //{
    //    //    var edge = shell.Edges[idx];
    //    //    if(startIdx != edge.StartIdx)
    //    //    {
    //    //        // evtl. Vorhandene Kante suchen, eine Kante darf nur einmal vorhanden sein
    //    //        if(shell.Vertices[startIdx].Incident.Any(ei => {
    //    //            var e = shell.Edges[ei];
    //    //            return e.StartIdx == startIdx && e.EndIdx == edge.EndIdx;
    //    //        }))
    //    //        { return false; }

    //    //        // Vertex Referenzen ändern
    //    //        if(!Vertex.AddIncident(shell, startIdx, idx))
    //    //        { return false; }
    //    //        Vertex.RemoveIncident(shell, edge.StartIdx, idx);

    //    //        // Vorgänger entfernen
    //    //        if(edge.PrevIdx is int pidx && shell.Edges[pidx].PrevIdx.HasValue)
    //    //        {
    //    //           var tedge = shell.Edges[pidx];
    //    //           shell.Edges[pidx] = new Edge(tedge.Idx, tedge.FaceIdx, tedge.StartIdx, tedge.EndIdx, tedge.PrevIdx, null, tedge.TwinIdx);

    //    //        }

    //    //        // Twin entfernen
    //    //        if(edge.TwinIdx is int tidx && shell.Edges[tidx].TwinIdx.HasValue)
    //    //        {
    //    //            var tedge = shell.Edges[tidx];
    //    //            shell.Edges[tidx] = new Edge(tedge.Idx, tedge.FaceIdx, tedge.StartIdx, tedge.EndIdx, tedge.PrevIdx, tedge.NextIdx, null);
    //    //        }

    //    //        shell.Edges[idx] = new Edge(idx, edge.FaceIdx, startIdx, edge.EndIdx, null, edge.NextIdx, null);
    //    //    }
    //    //    return true;
    //    //}

    //    ///// <summary>
    //    ///// Ändert Endvertex, Achtung am Ende Connect aufrufen, da alle Verknüpfungen entfernt werden
    //    ///// </summary>
    //    ///// <param name="shell"></param>
    //    ///// <param name="idx"></param>
    //    ///// <param name="endIdx"></param>
    //    ///// <returns></returns>
    //    //internal static bool SetEnd(Shell shell, int idx, int endIdx)
    //    //{
    //    //    var edge = shell.Edges[idx];
    //    //    if(endIdx != edge.EndIdx)
    //    //    {
    //    //        // evtl. Vorhandene Kante suchen, eine Kante darf nur einmal vorhanden sein
    //    //        if(shell.Vertices[endIdx].Incident.Any(ei => {
    //    //            var e = shell.Edges[ei];
    //    //            return e.EndIdx == endIdx && e.StartIdx == edge.StartIdx;
    //    //        }))
    //    //        { return false; }

    //    //        // Vertex Referenzen ändern
    //    //        if(!Vertex.AddIncident(shell, endIdx, idx))
    //    //        { return false; }
    //    //        Vertex.RemoveIncident(shell, edge.EndIdx, idx);

    //    //        // Nachfolger entfernen
    //    //        if(edge.NextIdx is int nidx && shell.Edges[nidx].PrevIdx.HasValue)
    //    //        {
    //    //            var tedge = shell.Edges[nidx];
    //    //            shell.Edges[nidx] = new Edge(tedge.Idx, tedge.FaceIdx, tedge.StartIdx, tedge.EndIdx, tedge.PrevIdx, tedge.NextIdx, null);

    //    //        }

    //    //        // Twin entfernen
    //    //        if(edge.TwinIdx is int tidx && shell.Edges[tidx].TwinIdx.HasValue)
    //    //        {
    //    //            var tedge = shell.Edges[tidx];
    //    //            shell.Edges[tidx] = new Edge(tedge.Idx, tedge.FaceIdx, tedge.StartIdx, tedge.EndIdx, tedge.PrevIdx, tedge.NextIdx, null);
    //    //        }

    //    //        shell.Edges[idx] = new Edge(idx, edge.FaceIdx, edge.StartIdx, endIdx, edge.PrevIdx, null, null);
    //    //    }
    //    //    return true;
    //    //}

    //    //internal bool Split(Shell shell, int edgeIdx, int vtxIdx, out int firstEdge, out int secondEdge)
    //    //{
    //    //    firstEdge = -1;
    //    //    secondEdge = -1;





    //    //    return false;
    //    //}

    //    /// <summary>
    //    /// Erzeugt alle Verknüpfungen
    //    /// </summary>
    //    /// <param name="shell"></param>
    //    /// <param name="idx"></param>
    //    internal static void Connect(Shell shell, int idx)
    //    {
    //        var edge = shell.Edges[idx];
    //        int? twinIdx = edge.TwinIdx;
    //        int? prevIdx = edge.PrevIdx;
    //        int? nextIdx = edge.NextIdx;
    //        // Twin suchen
    //        if(!twinIdx.HasValue)
    //        {
    //            int i = Array.FindIndex(shell.Vertices[edge.StartIdx].Incident, ei =>
    //            {
    //                var e = shell.Edges[ei];
    //                return !e.TwinIdx.HasValue && e.FaceIdx != edge.FaceIdx
    //                && e.StartIdx == edge.EndIdx && e.EndIdx == edge.StartIdx;
    //            });
    //            if(i >= 0)
    //            {
    //                var twin = shell.Edges[i];
    //                shell.Edges[i] = new Edge(i, twin.FaceIdx, twin.StartIdx, twin.EndIdx, twin.PrevIdx, twin.NextIdx, idx);
    //                twinIdx = i;
    //            }
    //        }

    //        // Vorgänger setzen
    //        if(!prevIdx.HasValue)
    //        {
    //            int i = Array.FindIndex(shell.Vertices[edge.StartIdx].Incident, ei =>
    //            {
    //                var e = shell.Edges[ei];
    //                return !e.NextIdx.HasValue && e.FaceIdx == edge.FaceIdx && e.EndIdx == edge.StartIdx;
    //            });
    //            if(i >= 0)
    //            {
    //                var prev = shell.Edges[i];
    //                shell.Edges[i] = new Edge(i, prev.FaceIdx, prev.StartIdx, prev.EndIdx, prev.PrevIdx, idx, prev.TwinIdx);
    //                prevIdx = i;
    //            }
    //        }

    //        // Nachfolger setzen
    //        if(!nextIdx.HasValue)
    //        {
    //            int i = Array.FindIndex(shell.Vertices[edge.EndIdx].Incident, ei =>
    //            {
    //                var e = shell.Edges[ei];
    //                return !e.PrevIdx.HasValue && e.FaceIdx == edge.FaceIdx && e.StartIdx == edge.EndIdx;
    //            });
    //            if(i >= 0)
    //            {
    //                var next = shell.Edges[i];
    //                shell.Edges[i] = new Edge(i, next.FaceIdx, next.StartIdx, next.EndIdx, idx, next.NextIdx, next.TwinIdx);
    //                nextIdx = i;
    //            }
    //        }

    //        shell.Edges[idx] = new Edge(idx, edge.FaceIdx, edge.StartIdx, edge.EndIdx, prevIdx, nextIdx, twinIdx);
    //    }
    //}
}
