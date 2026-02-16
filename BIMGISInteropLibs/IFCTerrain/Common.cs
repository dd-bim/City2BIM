using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
//Include localization
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace BIMGISInteropLibs.IfcTerrain
{
    public static class Common
    {
        public static readonly Dictionary<string, double> ToMeter = new Dictionary<string, double>()
        {
            ["millimeter"] = 0.001,
            ["centimeter"] = 0.01,
            ["kilometer"] = 1000.0,
            ["foot"] = 0.3048,
            ["inch"] = 0.0254,
            ["mile"] = 1609.34,
            ["ussurveyfoot"] = 1200.0 / 3937.0
        };
        /// <summary>
        /// Calculates Envelope from config, taking current data into account (if provided by result).
        /// Extends of "0" are interpreted as inf. Envelopes only containing inf boders are set to null. 
        /// If no origin is provided by config, envelope is null.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static Envelope GetEnvelope(Config config, Result result = null)
        {
            Envelope envelope = new Envelope();
            if (config.xExtend.HasValue || config.yExtend.HasValue)
            {
                if (config.xOrigin.HasValue &&
                    config.yOrigin.HasValue)
                {
                    envelope = new Envelope(
                        config.xExtend.HasValue && (config.xExtend != 0.0) ? (double)config.xOrigin - 0.5 * (double)config.xExtend : double.NegativeInfinity,
                        config.xExtend.HasValue && (config.xExtend != 0.0) ? (double)config.xOrigin + 0.5 * (double)config.xExtend : double.PositiveInfinity,
                        config.yExtend.HasValue && (config.yExtend != 0.0) ? (double)config.yOrigin - 0.5 * (double)config.yExtend : double.NegativeInfinity,
                        config.yExtend.HasValue && (config.yExtend != 0.0) ? (double)config.yOrigin + 0.5 * (double)config.yExtend : double.PositiveInfinity
                    );
                    if(double.IsNegativeInfinity(envelope.MinX) && double.IsNegativeInfinity(envelope.MinY) && 
                        double.IsInfinity(envelope.MaxX) && double.IsInfinity(envelope.MaxY))
                    {
                        envelope.SetToNull();
                    }
                    if(result != null)
                    {
                        Envelope pointsEnvelope = new Envelope();
                        result.pointList.ForEach(x => pointsEnvelope.ExpandToInclude(x.X, x.Y));
                        envelope = envelope.Intersection(pointsEnvelope);
                    }
                }
            }
            return envelope;
        }
        /// <summary>
        /// ApplyEnvelope to results input-data (Result.pointList & result.triMap).
        /// For triMap triangle-edges are cut by envelope and triangles with outer vertices are removed, leading to additional, unconnected vertices in pointList.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="envelope"></param>
        public static void ApplyEnvelope(Result result, Envelope envelope)
        {
            if (result == null || envelope == null || envelope.IsNull) return;

            // 1) Cut triangle edges by envelope and add intersection points to pointList

            // Geometry helpers
            GeometryFactory geometryFactory = new GeometryFactory(new PrecisionModel(), 25833);
            NetTopologySuite.Geometries.LinearRing envelopeRing = geometryFactory.CreateLinearRing(new[]
            {
                new Coordinate(envelope.MinX, envelope.MinY),
                new Coordinate(envelope.MinX, envelope.MaxY),
                new Coordinate(envelope.MaxX, envelope.MaxY),
                new Coordinate(envelope.MaxX, envelope.MinY),
                new Coordinate(envelope.MinX, envelope.MinY)
            });

            // 1.1) Collect unique undirected edges (IDs) from triMap
            var edges = new HashSet<(int a, int b)>();
            if (result.triMap != null)
            {
                foreach (var tri in result.triMap)
                {
                    var v = tri.triValues;
                    if (v == null || v.Length < 3) continue;
                    for (int e = 0; e < 3; e++)
                    {
                        int a = v[e];
                        int b = v[(e + 1) % 3];
                        if (a == b) continue;
                        var key = a < b ? (a, b) : (b, a);
                        edges.Add(key);
                    }
                }
            }

            // 1.2) Compute intersection points for each unique edge (but do NOT modify result.pointList yet)
            var intersectionPoints = new List<NetTopologySuite.Geometries.Point>();
            foreach (var (a, b) in edges)
            {
                if (a < 0 || a >= result.pointList.Count || b < 0 || b >= result.pointList.Count) continue;

                var line = geometryFactory.CreateLineString(new[]
                {
                    result.pointList[a].Coordinate,
                    result.pointList[b].Coordinate
                });

                var inter = envelopeRing.Intersection(line);
                if (inter.IsEmpty) continue;

                if (inter is NetTopologySuite.Geometries.Point pt)
                {
                    // linear interpolation for Z
                    pt.Z = line.StartPoint.Z + (line.EndPoint.Z - line.StartPoint.Z) * (pt.Distance(line.StartPoint) / line.Length);
                    intersectionPoints.Add(pt);
                }
            }

            // 2) Determine original points outside envelope
            var outsidePoints = new List<NetTopologySuite.Geometries.Point>();
            foreach(var point in result.pointList)
            {
                if (!envelope.Contains(point.X, point.Y))
                {
                    outsidePoints.Add(point);
                }
            }
            result.RemovePoints(outsidePoints);

            // 3) Append intersection points as isolated (unconnected) points
            result.pointList.AddRange(intersectionPoints);
        }
    }
}
