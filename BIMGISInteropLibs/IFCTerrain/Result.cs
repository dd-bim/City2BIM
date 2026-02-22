using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NetTopologySuite.Geometries;

namespace BIMGISInteropLibs.IfcTerrain
{
    /// <summary>
    /// Exchange class for processing tins, break edges and error messages
    /// </summary>
    public class Result
    {
        /// <summary>
        /// internale use
        /// </summary>
        public DtmConversionType currentConversion { get; set; }

        /// <summary>
        /// [FILE-READING] transfer point list (for NTS only)
        /// </summary>
        public List<Point> pointList { get; set; }

        /// <summary>
        /// [FILE-READING] transfer triangles (NTS)
        /// </summary>
        public List<Polygon> triangleList { get; set; }

        /// <summary>
        /// [FILE-READING] transfer breaklines (NTS)
        /// </summary>
        public List<LineString> lines { get; set; }

        /// <summary>
        /// [FILE-WRITING] unqiue list of coordinates in a dtm
        /// </summary>
        public CoordinateList coordinateList { get; set; } = null;

        /// <summary>
        /// [FILE-WRITING] txt export (only internal support)
        /// </summary>
        public GeometryCollection geomStore { get; set; } = null;

        /// <summary>
        /// [FILE-WRITING] mapped int values (point indicies) 
        /// </summary>
        public HashSet<Triangulator.triangleMap> triMap { get; set; } = new HashSet<Triangulator.triangleMap>();

        /// <summary>
        /// Gets or sets the <see cref="CancellationTokenSource"/> used to signal cancellation for ongoing operations.
        /// </summary>
        public CancellationTokenSource cancellationTokenSource { get; set; } = new CancellationTokenSource();

        /// <summary>
        /// [IFCTerrain] exchange origin
        /// </summary>
        public Coordinate origin { get; set; } = null;

        /// <summary>
        /// Remove points by their indices (indices refer to current pointList positions).
        /// Updates triMap by remapping indices; triangles that reference removed indices are discarded.
        /// triangleList is cleared to avoid stale geometry — rebuild from triMap+pointList if needed.
        /// </summary>
        /// <param name="Ids">List of indices into pointList to remove</param>
        public void RemovePoints(List<int> Ids)
        {
            if (Ids == null || Ids.Count == 0) return;
            if (pointList == null || pointList.Count == 0) return;

            var removeSet = new HashSet<int>(Ids);
            int oldCount = pointList.Count;

            // validate indices and clamp
            removeSet.RemoveWhere(id => id < 0 || id >= oldCount);
            if (removeSet.Count == 0) return;

            // build old->new mapping
            var oldToNew = new int[oldCount];
            for (int i = 0; i < oldCount; i++) oldToNew[i] = -1;

            var newPointList = new List<Point>(oldCount - removeSet.Count);
            int newIdx = 0;
            for (int i = 0; i < oldCount; i++)
            {
                if (removeSet.Contains(i)) continue;
                oldToNew[i] = newIdx;
                newPointList.Add(pointList[i]);
                newIdx++;
            }

            // rebuild triMap: remap indices; drop triangles that reference removed points
            bool triMapChanged = false;
            if (triMap != null && triMap.Count > 0)
            {
                var newTriMap = new HashSet<Triangulator.triangleMap>();
                foreach (var tri in triMap)
                {
                    if (tri?.triValues == null || tri.triValues.Length < 3) continue;

                    bool skip = false;
                    var newVals = new int[tri.triValues.Length];
                    for (int k = 0; k < tri.triValues.Length; k++)
                    {
                        int oldId = tri.triValues[k];
                        if (oldId < 0 || oldId >= oldCount)
                        {
                            skip = true;
                            break;
                        }
                        if (removeSet.Contains(oldId))
                        {
                            skip = true;
                            break;
                        }
                        int mapped = oldToNew[oldId];
                        if (mapped < 0)
                        {
                            skip = true;
                            break;
                        }
                        newVals[k] = mapped;
                    }
                    if (!skip)
                    {
                        newTriMap.Add(new Triangulator.triangleMap()
                        {
                            triNumber = tri.triNumber,
                            triValues = newVals
                        });
                    }
                    else
                    {
                        triMapChanged = true;
                    }
                }
                triMap = newTriMap;
            }

            // clear triangleList to avoid mismatch; caller can rebuild it from triMap/pointList
            if (triangleList != null && triangleList.Count > 0)
            {
                triangleList.Clear();
            }

            // assign new pointList
            pointList = newPointList;

            // adjust conversion state if triangles were removed
            if (triMapChanged && currentConversion == DtmConversionType.conversion)
            {
                currentConversion = DtmConversionType.faces;
            }
        }

        /// <summary>
        /// Existing RemovePoints by Point-value — kept for compatibility (maps points -> indices and calls index-based RemovePoints).
        /// Assumes the provided point instances are the same instances held in pointList (reference equality).
        /// </summary>
        public void RemovePoints(List<Point> points)
        {
            if (points == null || points.Count == 0) return;
            if (pointList == null || pointList.Count == 0) return;

            var set = new HashSet<Point>(points);
            var ids = new List<int>();
            for (int i = 0; i < pointList.Count; i++)
            {
                if (set.Contains(pointList[i])) ids.Add(i);
            }
            RemovePoints(ids);
        }
    }

    /// <summary>
    /// different szenarios for dtm conversion
    /// </summary>
    public enum DtmConversionType
    {
        /// <summary>
        /// dtm contains points --> need to do a delauny triangulation
        /// </summary>
        points,

        /// <summary>
        /// dtm contains faces --> conversion of the given faces
        /// </summary>
        faces,

        /// <summary>
        /// dtm contains points & breaklines --> need to do a conforming delauny triangulation
        /// </summary>
        points_breaklines,

        /// <summary>
        /// dtm contains faces & breaklines --> need to do a conforming delauny triangulation
        /// </summary>
        faces_breaklines,

        /// <summary>
        /// dtm contains index map (delauany triangulation is not necessary) (no breakline processing)
        /// </summary>
        conversion
    }
}
