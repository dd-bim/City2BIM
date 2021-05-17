using BimGisCad.Collections;
using BimGisCad.Representation.Geometry;
using BimGisCad.Representation.Geometry.Elementary;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BimGisCad.Representation.Topology
{
    ///// <summary>
    ///// Face
    ///// </summary>
    //public struct Face
    //{
    //    private const int maxCnt = 100000;

    //    private Face(int idx, int? firstIdx, int? lastIdx)
    //    {
    //        this.Idx = idx;
    //        this.FirstIdx = firstIdx;
    //        this.LastIdx = lastIdx;
    //    }

    //    /// <summary>
    //    /// Face geschlossen?
    //    /// </summary>
    //    /// <param name="shell"></param>
    //    /// <returns></returns>
    //    public bool IsClosed(Shell shell) {
    //        if(this.FirstIdx.HasValue && this.LastIdx.HasValue)
    //        {
    //            int fi = this.FirstIdx.Value;
    //            int li = this.LastIdx.Value;
    //            return shell.Edges[li].NextIdx.HasValue 
    //                && shell.Edges[li].NextIdx.Value == this.FirstIdx.Value
    //                && shell.Edges[fi].PrevIdx.HasValue 
    //                && shell.Edges[fi].PrevIdx.Value == this.LastIdx.Value;
    //        }
    //        return false;
    //    }

    //    /// <summary>
    //    /// Face fortlaufend?
    //    /// </summary>
    //    /// <param name="shell"></param>
    //    /// <returns></returns>
    //    public bool IsContiguous(Shell shell)
    //    {
    //        if(this.FirstIdx.HasValue && this.LastIdx.HasValue)
    //        {
    //            int fi = this.FirstIdx.Value;
    //            int li = this.LastIdx.Value;
    //            int cnt = 0;
    //            while(shell.Edges[fi].NextIdx.HasValue && cnt++ < maxCnt)
    //            {
    //                int ni = shell.Edges[fi].NextIdx.Value;
    //                if(ni == li)
    //                { return true; }
    //                fi = ni;
    //            }
    //        }
    //        return false;
    //    }

    //    /// <summary>
    //    /// Index des Face in Shell
    //    /// </summary>
    //    public int Idx { get; }


    //    /// <summary>
    //    /// Index der ersten Kante in Shell
    //    /// </summary>
    //    public int? FirstIdx { get; }

    //    /// <summary>
    //    /// Index der letzten Kante in Shell
    //    /// </summary>
    //    public int? LastIdx { get; }

    //    internal static int Create(Shell shell)
    //    {
    //        int idx = shell.Faces.Count;
    //        shell.Faces.Add(new Face(idx, null, null));
    //        return idx;
    //    }

    //    internal static bool AddVertex(Shell shell, int faceIdx, int idx)
    //    {
    //        var face = shell.Faces[faceIdx];
    //        if(face.LastIdx.HasValue)
    //        {
    //            var edge = shell.Edges[face.LastIdx.Value];
    //            if(!edge.NextIdx.HasValue)
    //            {
    //                int? edgeIdx = Edge.Create(shell, faceIdx, edge.EndIdx, idx);
    //                if(edgeIdx.HasValue)
    //                {
    //                    Edge.Connect(shell, edgeIdx.Value);
    //                    shell.Faces[faceIdx] = new Face(faceIdx, face.FirstIdx, edgeIdx.Value);
    //                    return true;
    //                }
    //            }
    //        }
    //        return false;
    //    }

    //    internal static bool PrependVertex(Shell shell, int faceIdx, int idx)
    //    {
    //        var face = shell.Faces[faceIdx];
    //        if(face.FirstIdx.HasValue)
    //        {
    //            var edge = shell.Edges[face.FirstIdx.Value];
    //            if(!edge.PrevIdx.HasValue)
    //            {
    //                int? edgeIdx = Edge.Create(shell, faceIdx, idx, edge.StartIdx);
    //                if(edgeIdx.HasValue)
    //                {
    //                    Edge.Connect(shell, edgeIdx.Value);
    //                    shell.Faces[faceIdx] = new Face(faceIdx, edgeIdx.Value, face.LastIdx);
    //                    return true;
    //                }
    //            }
    //        }
    //        return false;
    //    }


    //    internal static bool AddFirstEdge(Shell shell, int faceIdx, int startIdx, int endIdx)
    //    {
    //        var face = shell.Faces[faceIdx];
    //        if(face.FirstIdx.HasValue || face.LastIdx.HasValue)
    //        {
    //            return false;
    //        }
    //        int? edgeIdx = Edge.Create(shell, faceIdx, startIdx, endIdx);
    //        if(edgeIdx.HasValue)
    //        {
    //            Edge.Connect(shell, edgeIdx.Value);
    //            shell.Faces[faceIdx] = new Face(faceIdx, edgeIdx.Value, edgeIdx.Value);
    //            return true;
    //        }
    //        return false;
    //    }

    //}
}
