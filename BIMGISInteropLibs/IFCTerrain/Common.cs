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
            var oldPoints = result.pointList;
            if (oldPoints == null || oldPoints.Count == 0) return;

            var newPoints = new List<NetTopologySuite.Geometries.Point>(oldPoints.Count);
            var indexMap = new Dictionary<int, int>(oldPoints.Count);
            bool bPointsModified = false; //flag if some point was modified
            for (int i = 0; i < oldPoints.Count; i++)
            {
                var p = oldPoints[i];
                if (p != null && envelope.Contains(p.X, p.Y))
                {
                    indexMap[i] = newPoints.Count;
                    newPoints.Add(p);
                }
                else
                {
                    bPointsModified = true;
                }
            }

            //Cut Edges of existing triangles
            GeometryFactory geometryFactory = new GeometryFactory(new PrecisionModel(), 25833);

            NetTopologySuite.Geometries.LinearRing envelopeRing = geometryFactory.CreateLinearRing([
                        new Coordinate(envelope.MinX, envelope.MinY),
                        new Coordinate(envelope.MinX, envelope.MaxY),
                        new Coordinate(envelope.MaxX, envelope.MaxY),
                        new Coordinate(envelope.MaxX, envelope.MinY),
                        new Coordinate(envelope.MinX, envelope.MinY)]);
            foreach (var tri in result.triMap)
            {
                LineString line_a = geometryFactory.CreateLineString([oldPoints[tri.triValues[0]].Coordinate, oldPoints[tri.triValues[1]].Coordinate]);
                LineString line_b = geometryFactory.CreateLineString([oldPoints[tri.triValues[1]].Coordinate, oldPoints[tri.triValues[2]].Coordinate]);
                LineString line_c = geometryFactory.CreateLineString([oldPoints[tri.triValues[2]].Coordinate, oldPoints[tri.triValues[0]].Coordinate]);
                foreach (var line in new LineString[] { line_a, line_b, line_c })
                {
                    var inter = envelopeRing.Intersection(line);
                    if (!inter.IsEmpty && inter is Point pt)
                    {
                        pt.Z = line.StartPoint.Z + (line.EndPoint.Z - line.StartPoint.Z) * (pt.Distance(line.StartPoint) / line.Length);
                        newPoints.Add(pt);
                    }
                }
            }

            // Check triangles -> remap indices or remove triangles with out-of-envelope points
            bool bTriMapModified = false;
            if (result.triMap != null && bPointsModified)
            {
                var newTriMap = new HashSet<Triangulator.triangleMap>();
                foreach (var tri in result.triMap)
                {
                    var vals = tri.triValues;
                    if (vals == null || vals.Length < 3) continue;

                    bool skip = false;
                    var newVals = new int[vals.Length];
                    for (int k = 0; k < vals.Length; k++)
                    {
                        if (!indexMap.TryGetValue(vals[k], out int mapped))
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
                        bTriMapModified = true;
                    }

                }
                result.triMap = newTriMap;
            }
            if (bTriMapModified && result.currentConversion == DtmConversionType.conversion)
            {
                // if no conversion was needed before, but triangles were removed now, so set to faces
                result.currentConversion = DtmConversionType.faces;
            }
            result.pointList = newPoints;
        }
    }
}
