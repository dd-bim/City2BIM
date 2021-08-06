using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry.Elementary; //provides Vector

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc4.GeometricModelResource;         //IfcShellBasedSurfaceModel or IfcGeometricCurveSet

//Transfer class for the reader (IFCTerrain)
using BIMGISInteropLibs.IfcTerrain;

//logging
using BIMGISInteropLibs.Logging;                                    //access to log writer
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain;    //to set log messages

//NTS - geometry types
using NetTopologySuite.Geometries;

namespace BIMGISInteropLibs.IFC.Ifc4
{
    /// <summary>
    /// Class to provide methods to work with IfcTriangulatedFaceSet
    /// </summary>
    public class TriangulatedFaceSet
    {
        public static IfcTriangulatedFaceSet Create(IfcStore model, Vector3 origin, Result result,
            //double? breakDist, //currently not aviable
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {

            //get index map for processed triangles
            var triMap = result.triMap;

            //get unique coord list 
            CoordinateList coordinates = result.coordinateList;

            //start transaction (ACID) - need to be commited
            using (var txn = model.BeginTransaction("Create TIN"))
            {
                //logging
                LogWriter.Add(LogType.verbose, "IfcTFS shape representation started...");

                //init empty dictionary
                var vmap = new Dictionary<int, int>();

                //Cartesian Point List
                var cpl = model.Instances.New<IfcCartesianPointList3D>(c =>
                {
                    //process all points in tin
                    for (int i = 0, j = 0; i < coordinates.Count; i++)
                    {
                        //add point to dicitionary
                        vmap.Add(i, j + 1);

                        //get current point
                        var pt = coordinates[i];

                        //add point to coordlist with x y z values
                        var coo = c.CoordList.GetAt(j++);
                        coo.Add(pt.X - origin.X);
                        coo.Add(pt.Y - origin.Y);
                        coo.Add(pt.Z - origin.Z);
                    }
                });
                //logging
                LogWriter.Add(LogType.debug, "CoordList created.");

                //TFS write --> need cpl
                var tfs = model.Instances.New<IfcTriangulatedFaceSet>(t =>
                {
                    //Attribute #3 - Closed (IfcBoolean) 
                    t.Closed = false; //set to true only for closed surface (never the case for DTM)

                    //Attribute #5 - PnIndex
                    t.Coordinates = cpl;

                    //Attribute #4 - CoordIndex (maximum length = number of triangles)
                    int pos = 0;

                    //loop through each triangle in triangle map to get int value of point (index)
                    foreach (var triangle in triMap)
                    {
                        //get posotion
                        var fi = t.CoordIndex.GetAt(pos++);

                        //get values & add them
                        fi.Add(vmap[triangle.triValues[0]]);
                        fi.Add(vmap[triangle.triValues[1]]);
                        fi.Add(vmap[triangle.triValues[2]]);
                    }

                    //Attribut #5 - Number of Triangels (will be read; not explicit)

                    //Logging number of triangles --> write to result class
                    //TODO
                });
                
                //describe the representation
                representationIdentifier = RepresentationIdentifier.Body;
                representationType = RepresentationType.Tessellation;

                //Create tin commit otherwise the changes will not be incorporated (ACID)
                txn.Commit();

                //logging
                LogWriter.Add(LogType.verbose, "IfcTFS shape representation created.");

                return tfs;
            }
        }
    }
}
