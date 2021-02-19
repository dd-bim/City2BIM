using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry.Elementary;
using BimGisCad.Representation.Geometry.Composed;
using BimGisCad.Collections;                    //provides MESH --> will be removed

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc4.GeometricModelResource;         //IfcShellBasedSurfaceModel or IfcGeometricCurveSet

namespace BIMGISInteropLibs.IFC.Ifc4
{
    /// <summary>
    /// Class to provide methods to work with IfcTriangulatedFaceSet
    /// </summary>
    public class TriangulatedFaceSet
    {
        /// <summary>
        /// will be removed
        /// </summary>
        /// <param name="model"></param>
        /// <param name="origin"></param>
        /// <param name="mesh"></param>
        /// <param name="representationType"></param>
        /// <param name="representationIdentifier"></param>
        /// <returns></returns>
        public static IfcTriangulatedFaceSet CreateViaMesh(IfcStore model, Vector3 origin, Mesh mesh,
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {
            if (mesh.MaxFaceCorners != 3 || mesh.MinFaceCorners != 3)
            { throw new Exception("Mesh is not Triangular"); }
            using (var txn = model.BeginTransaction("Create TIN"))
            {
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

                var tfs = model.Instances.New<IfcTriangulatedFaceSet>(t =>
                {
                    t.Closed = false; // nur bei Volumenkörpern
                    t.Coordinates = cpl;
                    int cnt = 0;
                    foreach (int fe in mesh.FaceEdges)
                    {
                        var fi = t.CoordIndex.GetAt(cnt++);
                        fi.Add(vmap[mesh.EdgeVertices[fe]]);
                        fi.Add(vmap[mesh.EdgeVertices[mesh.EdgeNexts[fe]]]);
                        fi.Add(vmap[mesh.EdgeVertices[mesh.EdgeNexts[mesh.EdgeNexts[fe]]]]);
                    }
                });

                txn.Commit();
                representationIdentifier = RepresentationIdentifier.Body;
                representationType = RepresentationType.Tessellation;

                return tfs;
            }
        }


        /// <summary>
        /// Creating DTM via IfcTFS (processing a TIN)
        /// </summary>
        /// <param name="model">Location for all information that will be inserted into the IFC file</param>
        /// <param name="origin">Provided by IfcLocalPlacement</param>
        /// <param name="tin">Provided by File-Reader</param>
        /// <param name="representationType">Output - do not change</param>
        /// <param name="representationIdentifier">Output - do not change</param>
        /// <returns>Shape which is written in the IFC file</returns>
        public static IfcTriangulatedFaceSet CreateViaTin(IfcStore model, Vector3 origin, Tin tin,
            //double? breakDist, //Ist dies jetzt noch relevant?
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {
            //init logger [TODO]
            //Logger logger = LogManager.GetCurrentClassLogger();

            //start transaction (ACID) - need to be commited
            using (var txn = model.BeginTransaction("Create TIN"))
            {
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
                        //add point to coordlist wit x y z values
                        var coo = c.CoordList.GetAt(j++);
                        coo.Add(pt.X - origin.X);
                        coo.Add(pt.Y - origin.Y);
                        coo.Add(pt.Z - origin.Z);
                    }

                });

                //TFS writ --> need cpl
                var tfs = model.Instances.New<IfcTriangulatedFaceSet>(t =>
                {
                    //Attribute #2 - Normals

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

                    //Attribut #5 - Number of Triangels (wird ausgelesen)
                    //[TODO]Logging number of triangles so it can compared with the result of the respective reader
                    //logger.Debug("There were " + cpl.CoordList.Count + " Points; " + t.NumberOfTriangles + " Triangles processed.");
                    //logger.Info("Result (Points): " + cpl.CoordList.Count + " of " + tin.Points.Count + " processed");
                    //logger.Info("Result (Triangles): " + t.NumberOfTriangles + " of " + tin.NumTriangles + " processed");
                });

                //Create tin commit otherwise the changes will not be incorporated (ACID)
                txn.Commit();

                //describe the representation
                representationIdentifier = RepresentationIdentifier.Body;
                representationType = RepresentationType.Tessellation;
                return tfs;
            }

        }
    }
}
