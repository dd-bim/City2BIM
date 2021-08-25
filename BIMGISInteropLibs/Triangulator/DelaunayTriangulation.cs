using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NTS = NetTopologySuite;
using NetTopologySuite.Triangulate;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate.QuadEdge;

using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace BIMGISInteropLibs.Triangulator
{
    public class DelaunayTriangulation
    {
        /// <summary>
        /// geometry factory need to use get functions (e.g. -> get triangles)
        /// </summary>
        private static GeometryFactory geometryFactory { get; set; }

        /// <summary>
        /// set up new instance for processing
        /// </summary>
        /// <param name="precision"></param>
        public static void setUp(double precision)
        {
            NTS.NtsGeometryServices.Instance = new NTS.NtsGeometryServices(
                new PrecisionModel(precision));
            geometryFactory = NTS.NtsGeometryServices.Instance.CreateGeometryFactory();
        }

        /// <summary>
        /// execute delaunay triangulation
        /// </summary>
        public static void triangulate(IfcTerrain.Result result)
        {
            setUp(100d);

            //init builder
            var builder = new ConformingDelaunayTriangulationBuilder()
            {
                //TODO set tol from user input (config)
                Tolerance = 0.001
            };
            
            //
            LogWriter.Add(LogType.verbose, "[NTS] Delauny builder initalized.");

            //create geometry collections
            GeometryCollection faces;
            GeometryCollection breaklines;
            GeometryCollection triangles;

            //switch between different conversion types
            switch (result.currentConversion)
            {
                default:
                    //set point list to multi points
                    Point[] points = result.pointList.ToArray();
                    
                    //set points as multi point
                    MultiPoint pointCollection = new MultiPoint(points);

                    //set sites
                    builder.SetSites(pointCollection);
                    break;

                case IfcTerrain.DtmConversionType.faces:
                    faces = new GeometryCollection(result.triangleList.ToArray());
                    //var geom = NTS.Simplify.DouglasPeuckerSimplifier.Simplify(geometryCollection, 2);
                    builder.SetSites(faces);
                    break;

                case IfcTerrain.DtmConversionType.faces_breaklines:
                    
                    faces = new GeometryCollection(result.triangleList.ToArray());
                    builder.SetSites(faces);

                    breaklines = new GeometryCollection(result.lines.ToArray());
                    builder.Constraints = breaklines;
                    break;

                case IfcTerrain.DtmConversionType.points_breaklines:
                    //set point list to multi points
                    Point[] pointList = result.pointList.ToArray();

                    //set points as multi point
                    MultiPoint pointCollection2 = new MultiPoint(pointList);

                    //set sites
                    builder.SetSites(pointCollection2);

                    breaklines = new GeometryCollection(result.lines.ToArray());
                    builder.Constraints = breaklines;

                    break;
            }

            //get triangles -> this one is a processing step (may take some time)
            triangles = builder.GetTriangles(geometryFactory);
            result.geomStore = triangles;

            //get unqiue coord list
            var coordinates = DelaunayTriangulationBuilder.Unique(triangles.Coordinates);

            if (result.currentConversion == IfcTerrain.DtmConversionType.faces_breaklines || result.currentConversion == IfcTerrain.DtmConversionType.points_breaklines)
            {
                //create empty list
                CoordinateList cList = new CoordinateList();

                //loop through every coord
                foreach (var c in coordinates)
                {
                    //if z value is not a number
                    if (double.IsNaN(c.Z))
                    {
                        //loop through each line
                        foreach (var line in result.lines)
                        {
                            //check point is on line
                            double dist = line.Distance(new Point(c));
                            if (dist < 0.0001)
                            {
                                //create new line segement
                                LineSegment lineSegment = new LineSegment(line.StartPoint.Coordinate, line.EndPoint.Coordinate);

                                //interpolate z value
                                double newZ = Vertex.InterpolateZ(c, lineSegment.P0, lineSegment.P1);

                                //add new coord to list
                                cList.Add(new CoordinateZ(c.X, c.Y, newZ));
                            }
                        }
                    }
                    else
                    {
                        //copy coord to list
                        cList.Add(c);
                    }
                }
                //set "new" list
                coordinates = cList;
            }
            //set coord list to result class
            result.coordinateList = coordinates;

            //
            LogWriter.Add(LogType.verbose, "[NTS] Number of unique coordinates: " + coordinates.Count);

            //create coord seqeuenz
            var coordSeq = geometryFactory.CoordinateSequenceFactory.Create(coordinates.ToArray());
           
            setIndexTriangulation(triangles, coordSeq, out HashSet<triangleMap> map);

            result.triMap = map;

            LogWriter.Add(LogType.info, "Points readed: " + coordinates.Count + "; Triangles readed: " + triangles.Count);

            return;

        }

        private static void setIndexTriangulation(GeometryCollection geometryCollection, CoordinateSequence coordinateSequence, out HashSet<triangleMap> map)
        {
            //
            map = new HashSet<triangleMap>();

            //
            int triNumber = 0;

            //
            foreach (var triangle in geometryCollection.Geometries)
            {
                int[] coordIndex = new int[3];

                for (int i = 0; i < 3; i++)
                {
                    var pointIndex = CoordinateSequences.IndexOf(triangle.Coordinates[i], coordinateSequence);
                    coordIndex[i] = pointIndex;
                }

                map.Add(new triangleMap()
                {
                    triNumber = triNumber++,
                    triValues = coordIndex
                });
            }
        }
    }

    public class triangleMap
    {
        public int triNumber { get; set; }
        public int[] triValues { get; set; }
    }
}
