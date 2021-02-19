using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OSGeo.OGR;
using BIMGISInteropLibs.Geometry;

namespace BIMGISInteropLibs.OGR
{
    class OGRUtils
    {
        public static List<List<C2BSegment>> getSegmentsFromGeometry(OSGeo.OGR.Geometry geom)
        {
            var geomType = geom.GetGeometryType();
            List<List<C2BSegment>> returnSegs = null;

            if (geomType == wkbGeometryType.wkbPolygon)
            {
                var outerSegs = getOuterRingSegmentsFromPolygon(geom);
                var myList = new List<List<C2BSegment>>();
                myList.Add(outerSegs);
                return myList;
            }

            return returnSegs;
        }

        private static List<C2BSegment> getOuterRingSegmentsFromPolygon(OSGeo.OGR.Geometry geom)
        {
            List<C2BSegment> segments = new List<C2BSegment>();

            var outerRing = geom.GetGeometryRef(0);

            double[] geoPoint = { 0, 0, 0 };

            for (var i = 1; i < outerRing.GetPointCount(); i++)
            {
                outerRing.GetPoint(i - 1, geoPoint);
                C2BPoint startSeg = new C2BPoint(geoPoint[0], geoPoint[1], 0);

                outerRing.GetPoint(i, geoPoint);
                C2BPoint endSeg = new C2BPoint(geoPoint[0], geoPoint[1], 0);

                segments.Add(new C2BSegment(startSeg, endSeg));
            }

            return segments;
        }
    }

    public class OGRSpatialFilter
    {
        public OSGeo.OGR.Geometry Geom { get; set; }
    }

    public class OGRCircularSpatialFilter: OGRSpatialFilter
    {
        public OGRCircularSpatialFilter(double x, double y, double radius)
        {
            var point = new OSGeo.OGR.Geometry(wkbGeometryType.wkbPoint);
            point.AddPoint_2D(x, y);
            var polygon = point.Buffer(radius, 30);
            this.Geom = polygon;
        }
    }

    public class OGRRectangularSpatialFilter: OGRSpatialFilter
    {
        public OGRRectangularSpatialFilter(double x, double y, double xOffset, double yOffset)
        {
            var ring = new OSGeo.OGR.Geometry(wkbGeometryType.wkbLinearRing);
            ring.AddPoint_2D(x - xOffset, y - yOffset);
            ring.AddPoint_2D(x + xOffset, y - yOffset);
            ring.AddPoint_2D(x + xOffset, y + yOffset);
            ring.AddPoint_2D(x - xOffset, y + yOffset);
            ring.AddPoint_2D(x - xOffset, y - yOffset);

            var poly = new OSGeo.OGR.Geometry(wkbGeometryType.wkbPolygon);
            poly.AddGeometry(ring);
            this.Geom = poly;
        }
    }
}
