using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BimGisCad.Representation.Geometry;

namespace BimGisCad.Collections
{

    //public class UniquePointList<T> : IEnumerable<T> where T:IPoint
    //{
    //    private class TreeNode
    //    {
    //        public readonly int idx;

    //        public TreeNode Left = null;
    //        public TreeNode Right = null;

    //        public TreeNode(int idx)
    //        {
    //            this.idx = idx;
    //        }

    //        public TreeNode this[int compare]
    //        {
    //            get
    //            {
    //                return compare <= 0 ? this.Left : this.Right;
    //            }

    //            set
    //            {
    //                if(compare <= 0)
    //                {
    //                    this.Left = value;
    //                }
    //                else
    //                {
    //                    this.Right = value;
    //                }
    //            }
    //        }

    //    }

    //    public KDTree(IReadOnlyList<Point3> points) => this.points = points;

    //    private readonly IReadOnlyList<Point3> points;
    //    private KdTreeNode root = null;

    //    private static bool equalsXY(Point3 p1, Point3 p2)
    //    {
    //        double dx = p2.X - p1.X;
    //        double dy = p2.Y - p1.Y;
    //        return IsNearlyZeroSquared((dx * dx) + (dy * dy));
    //    }

    //    private static bool equalsZ(Point3 p1, Point3 p2) => IsNearlyZero(p2.Z - p1.Z);

    //    public int Add(Point3 point)
    //    {
    //        var addNode = new KdTreeNode(this.points.Count);
    //        if(this.root == null)
    //        {
    //            this.root = addNode;
    //        }
    //        else
    //        {
    //            var curr = this.root;
    //            bool dox = true;

    //            while(true)
    //            {
    //                var currPoint = this.points[curr.idxs[0]];
    //                if(equalsXY(currPoint, point))
    //                {
    //                    foreach(int idx in curr.idxs)
    //                    {
    //                        if(equalsZ(this.points[idx], point))
    //                        {
    //                            return -idx;
    //                        }
    //                    }
    //                    curr.Add(this.points.Count);
    //                    return this.points.Count;
    //                }

    //                int compare = dox
    //                    ? point.X.CompareTo(currPoint.X)
    //                    : point.Y.CompareTo(currPoint.Y);

    //                if(curr[compare] == null)
    //                {
    //                    curr[compare] = addNode;
    //                    break;
    //                }
    //                else
    //                { curr = curr[compare]; }
    //                dox = !dox;
    //            }
    //        }
    //        return addNode.idxs[0];
    //    }












    //    public IEnumerator<T> GetEnumerator()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
