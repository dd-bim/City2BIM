using BimGisCad.Representation.Geometry.Composed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BIMGISInteropLibs.Logging;
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages
using BIMGISInteropLibs.NTSApi;
using BimGisCad.Representation.Geometry.Elementary;
using BIMGISInteropLibs.Geometry;

namespace BIMGISInteropLibs.Triangulator
{
    class IfcTerrainTriangulator
    {
        /// <summary>
        /// A function to create a TIN using BimGisCad TIN builder and an interface to NetTopologySuite class library to calculate triangles.
        /// </summary>
        /// <param name="dtmPointList">A list of double arrays. Each array contains the x, y and z coordinate of a DTM point.</param>
        /// <returns>A TIN structured by BimGisCad TIN builder</returns>
        public static Tin CreateTin(List<double[]> dtmPointList)
        {
            //Get a list of triangles via NetTopologySuite class library using the interface object
            List<List<double[]>> dtmTriangleList = new NtsApi().MakeTriangleList(dtmPointList);

            //Return TIN
            return BuildTin(dtmTriangleList);
        }

        /// <summary>
        /// A function to create a TIN using BimGisCad TIN builder and an interface to NetTopologySuite class library to calculate triangles.
        /// </summary>
        /// <param name="dtmPointList">A list of double arrays. Each array contains the x, y and z coordinate of a DTM point.</param>
        /// <param name="constraintList">A list of double arrays. Each array contains the x, y and z coordinate of a constraint vertex.</param>
        /// <returns>A TIN structured by BimGisCad TIN builder</returns>
        public static Tin CreateTin(List<double[]> dtmPointList, List<double[]> constraintList)
        {
            //Get a list of triangles via NetTopologySuite class library using the interface object
            List<List<double[]>> dtmTriangleList = new NtsApi().MakeTriangleList(dtmPointList, constraintList);

            //Return TIN
            return BuildTin(dtmTriangleList);
        }

        /// <summary>
        /// An auxiliary function to build a TIN of triangle data.
        /// </summary>
        /// <param name="dtmTriangleList">A list of lists of double arrays. Each list contains data of one triangle. Each array contains the x, y and z coordinate of a triangle vertex.</param>
        /// <returns>A TIN structured by BimGisCad TIN builder</returns>
        public static Tin BuildTin(List<List<double[]>> dtmTriangleList)
        {
            //Initialize TIN builder
            var tinBuilder = Tin.CreateBuilder(true);

            //Log TIN builder initalization
            LogWriter.Add(LogType.verbose, "[IfcTerrainTriangulator] Initialize a TIN builder.");

            //init hash set (for unquie points)
            var uptList = new HashSet<Geometry.uPoint3>();

            foreach (List<double[]> dtmTriangle in dtmTriangleList)
            {
                //Read out the three vertices of one triangle at each loop
                Point3 p1 = Point3.Create(dtmTriangle[0][0], dtmTriangle[0][1], dtmTriangle[0][2]);
                Point3 p2 = Point3.Create(dtmTriangle[1][0], dtmTriangle[1][1], dtmTriangle[1][2]);
                Point3 p3 = Point3.Create(dtmTriangle[2][0], dtmTriangle[2][1], dtmTriangle[2][2]);

                //add points to list [note: logging will be done in support function]
                int pnrP1 = terrain.addToList(uptList, p1);
                int pnrP2 = terrain.addToList(uptList, p2);
                int pnrP3 = terrain.addToList(uptList, p3);

                //add triangle via point numbers above
                tinBuilder.AddTriangle(pnrP1, pnrP2, pnrP3);

                //log
                LogWriter.Add(LogType.verbose, "[IfcTerrainTriangulator] Triangle [" + pnrP1 + "; " + pnrP2 + "; " + pnrP3 + "] set.");
            }

            //loop through point list 
            foreach (Geometry.uPoint3 pt in uptList)
            {
                tinBuilder.AddPoint(pt.pnr, pt.point3);
            }

            //Build and return a TIN via BimGisCad class library and log
            Tin tin = tinBuilder.ToTin(out var pointIndex2NumberMap, out var triangleIndex2NumberMap);
            LogWriter.Add(LogType.verbose, "[IfcTerrainTriangulator] Creating TIN via TIN builder.");
            return tin;
        }
    }
}
