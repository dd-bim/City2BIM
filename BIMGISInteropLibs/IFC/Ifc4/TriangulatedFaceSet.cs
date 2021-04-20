using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry.Elementary; //provides Vector
using BimGisCad.Representation.Geometry.Composed;   //provides TIN
using BimGisCad.Collections;                        //provides MESH

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc4.GeometricModelResource;         //IfcShellBasedSurfaceModel or IfcGeometricCurveSet

//Transfer class for the reader (IFCTerrain)
using BIMGISInteropLibs.IfcTerrain;

//logging
using BIMGISInteropLibs.Logging;                                    //access to log writer
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain;    //to set log messages

namespace BIMGISInteropLibs.IFC.Ifc4
{
    /// <summary>
    /// Class to provide methods to work with IfcTriangulatedFaceSet
    /// </summary>
    public class TriangulatedFaceSet
    {
        /// <summary>
        /// Creating DTM via IfcTFS (processing a MESH)
        /// </summary>
        public static IfcTriangulatedFaceSet CreateViaMesh(IfcStore model, Vector3 origin, Result result,
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {
            //get MESH from result class
            Mesh mesh = result.Mesh;

            //error checking if mesh is not correctly, can not be processed
            if (mesh.MaxFaceCorners != 3 || mesh.MinFaceCorners != 3)
            { throw new Exception("Mesh is not Triangular"); }

            //start transaction for IfcTFS
            using (var txn = model.BeginTransaction("Create TIN"))
            {
                //logging
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "IfcTFS shape representation started..."));

                //init empty dictionary
                var vmap = new Dictionary<int, int>();
                var cpl = model.Instances.New<IfcCartesianPointList3D>(c =>
                {
                    for (int i = 0, j = 0; i < mesh.Points.Count; i++)
                    {
                        if (mesh.VertexEdges[i] < 0)
                        { continue; }
                        vmap.Add(i, j + 1);
                        var pt = mesh.Points[i];
                        var coo = c.CoordList.GetAt(j++);
                        coo.Add(pt.X - origin.X);
                        coo.Add(pt.Y - origin.Y);
                        coo.Add(pt.Z - origin.Z);
                    }
                });

                //return stats to result class
                result.wPoints = cpl.CoordList.Count;

                //TFS write --> need cpl
                var tfs = model.Instances.New<IfcTriangulatedFaceSet>(t =>
                {
                    //Attribute #3 - Closed (IfcBoolean) 
                    t.Closed = false; //set to true only for closed surface (never the case for DTM)

                    //Attribute #5 - PnIndex
                    t.Coordinates = cpl;

                    //Attribute #4 - CoordIndex (maximum length = number of triangles)
                    int cnt = 0;

                    //create mesh via edge vertices
                    foreach (int fe in mesh.FaceEdges)
                    {
                        var fi = t.CoordIndex.GetAt(cnt++);
                        fi.Add(vmap[mesh.EdgeVertices[fe]]);
                        fi.Add(vmap[mesh.EdgeVertices[mesh.EdgeNexts[fe]]]);
                        fi.Add(vmap[mesh.EdgeVertices[mesh.EdgeNexts[mesh.EdgeNexts[fe]]]]);
                    }
                    //Attribut #5 - Number of Triangels (will be read; not explicit)

                    //return stats to result class
                    result.wFaces = int.Parse(t.NumberOfTriangles.Value.ToString());
                });
                //describe the representation
                representationIdentifier = RepresentationIdentifier.Body;
                representationType = RepresentationType.Tessellation;

                //commit otherwise will be roll back
                txn.Commit();
                
                //logging
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "IfcTFS shape representation created."));

                return tfs;
            }
        }


        /// <summary>
        /// Creating DTM via IfcTFS (processing a TIN)
        /// </summary>
        public static IfcTriangulatedFaceSet CreateViaTin(IfcStore model, Vector3 origin, Result result,
            //double? breakDist, //currently not aviable
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {
            //get TIN from result class
            Tin tin = result.Tin;

            //start transaction (ACID) - need to be commited
            using (var txn = model.BeginTransaction("Create TIN"))
            {
                //logging
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "IfcTFS shape representation started..."));

                //init empty dictionary
                var vmap = new Dictionary<int, int>();

                //Cartesian Point List
                var cpl = model.Instances.New<IfcCartesianPointList3D>(c =>
                {
                    //process all points in tin
                    for (int i = 0, j = 0; i < tin.Points.Count; i++)
                    {
                        //add point to dicitionary
                        vmap.Add(i, j + 1);
                        
                        //get current point
                        var pt = tin.Points[i];

                        //add point to coordlist with x y z values
                        var coo = c.CoordList.GetAt(j++);
                        coo.Add(pt.X - origin.X);
                        coo.Add(pt.Y - origin.Y);
                        coo.Add(pt.Z - origin.Z);
                    }
                });
                //logging
                LogWriter.Entries.Add(new LogPair(LogType.debug, "CoordList created."));

                //TFS write --> need cpl
                var tfs = model.Instances.New<IfcTriangulatedFaceSet>(t =>
                {
                    //Attribute #3 - Closed (IfcBoolean) 
                    t.Closed = false; //set to true only for closed surface (never the case for DTM)

                    //Attribute #5 - PnIndex
                    t.Coordinates = cpl;

                    //Attribute #4 - CoordIndex (maximum length = number of triangles)
                    int pos = 0;
                    foreach (var tri in tin.TriangleVertexPointIndizes())
                    {
                        //get triangle
                        var fi = t.CoordIndex.GetAt(pos++);
                        //set the points (form CoordList)
                        fi.Add(vmap[tri[0]]);
                        fi.Add(vmap[tri[1]]);
                        fi.Add(vmap[tri[2]]);
                    }

                    //Attribut #5 - Number of Triangels (will be read; not explicit)

                    //Logging number of triangles --> write to result class
                    result.wPoints = cpl.CoordList.Count;
                    result.wFaces = int.Parse(t.NumberOfTriangles.Value.ToString());
                });
                //describe the representation
                representationIdentifier = RepresentationIdentifier.Body;
                representationType = RepresentationType.Tessellation;

                //Create tin commit otherwise the changes will not be incorporated (ACID)
                txn.Commit();

                //logging
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "IfcTFS shape representation created."));
                
                return tfs;
            }
        }
    }
}
