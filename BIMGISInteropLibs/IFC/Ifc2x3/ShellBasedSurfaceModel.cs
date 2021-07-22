using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;
using BimGisCad.Representation.Geometry.Elementary;
using BimGisCad.Representation.Geometry.Composed;
using BimGisCad.Collections;                         //provides MESH --> will be removed

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc2x3.GeometricConstraintResource;  //IfcLocalPlacement
using Xbim.Ifc2x3.GeometryResource;             //IfcAxis2Placement3D
using Xbim.Ifc2x3.Kernel;                       //IfcProject
using Xbim.Common.Step21;                       //Enumeration to XbimShemaVersion
using Xbim.IO;                                  //Enumeration to XbimStoreType
using Xbim.Common;                              //ProjectUnits (Hint: support imperial (TODO: check if required)
using Xbim.Ifc2x3.MeasureResource;              //Enumeration for Unit
using Xbim.Ifc2x3.ProductExtension;             //IfcSite
using Xbim.Ifc2x3.GeometricModelResource;       //IfcShellBasedSurfaceModel or IfcGeometricCurveSet
using Xbim.Ifc2x3.TopologyResource;             //IfcOpenShell
using Xbim.Ifc2x3.RepresentationResource;       //IfcShapeRepresentation

//Transfer class for the reader (IFCTerrain)
using BIMGISInteropLibs.IfcTerrain;

//embed logging
using BIMGISInteropLibs.Logging; //access to log writer
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace BIMGISInteropLibs.IFC.Ifc2x3
{
    /// <summary>
    /// shape representation class <para/>
    /// Shell Based Surface Model (SBSM)
    /// </summary>
    public static class ShellBasedSurfaceModel
    {
        /// <summary>
        /// Creates a DTM via IfcSBSM (processing a mesh)
        /// </summary>
        public static IfcShellBasedSurfaceModel CreateViaMesh(IfcStore model, Vector3 origin, Result result,
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {
            //get MESH from result class
            Mesh mesh = result.Mesh;

            //error handling
            if (mesh.MaxFaceCorners < 3)
            {
                //log error
                LogWriter.Add(LogType.error, "[IFC-Writer] MESH has no Faces!");

                //output error (TODO: MESSAGE window?)
                throw new Exception("Mesh has no Faces");
            }

            //logging
            LogWriter.Add(LogType.verbose, "IfcSBSM shape representation creation started...");

            //start transaction
            using (var txn = model.BeginTransaction("Create Mesh"))
            {
                //creating the libraries
                var vmap = new Dictionary<int, int>();
                var cpl = new List<IfcCartesianPoint>();

                //loop to read all mesh points and create IfcCartesianPoints
                for (int i = 0, j = 0; i < mesh.Points.Count; i++)
                {
                    if (mesh.VertexEdges[i] < 0)
                    { continue; }
                    //add to dictionary
                    vmap.Add(i, j);

                    //get point from mesh and match it to local var
                    var pt = mesh.Points[i];
                    
                    //add to ifc entity
                    cpl.Add(model.Instances.New<IfcCartesianPoint>(c => c.SetXYZ(pt.X - origin.X, pt.Y - origin.Y, pt.Z - origin.Z)));
                    j++;
                }

                //input for stats (will be logged / gui logged)
                result.wPoints = cpl.Count;

                //init numFaces for processing results (logging)
                int numFaces = 0;

                //create shape model
                var sbsm = model.Instances.New<IfcShellBasedSurfaceModel>(s =>
                    s.SbsmBoundary.Add(model.Instances.New<IfcOpenShell>(o => o.CfsFaces
                        .AddRange(mesh.FaceEdges.Select(fe => model.Instances.New<IfcFace>(x => x.Bounds
                            .Add(model.Instances.New<IfcFaceOuterBound>(b =>
                            {
                                b.Bound = model.Instances.New<IfcPolyLoop>(p =>
                                {
                                    int curr = fe;
                                    do
                                    {
                                        p.Polygon.Add(cpl[vmap[mesh.EdgeVertices[curr]]]);
                                        curr = mesh.EdgeNexts[curr];
                                    } while (curr != fe && p.Polygon.Count < mesh.MaxFaceCorners);
                                });

                                //count up
                                numFaces++;

                                //clockwise orientation (set to true)
                                b.Orientation = true;
                            }))))))));

                //pass processed number of faces to result
                result.wFaces = numFaces;

                //write identifier (otherwise shape is not valid)
                representationIdentifier = RepresentationIdentifier.Body;
                representationType = RepresentationType.SurfaceModel;
                
                //finish transaction
                txn.Commit();

                //logging
                LogWriter.Add(LogType.verbose, "IfcSBSM shape representation created.");

                return sbsm;
            }
        }

        /// <summary>
        /// Creates a DTM via IfcSBSM (processing a tin)
        /// </summary>
        /// <returns>Shape which is written in the IFC file</returns>
        public static IfcShellBasedSurfaceModel CreateViaTin(IfcStore model, Vector3 origin, Result result,
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {
            //get TIN from result class
            Tin tin = result.Tin;

            //start with the transaction (cf. ACID)
            using (var txn = model.BeginTransaction("Create Tin"))
            {
                //logging
                LogWriter.Add(LogType.verbose, "IfcSBSM shape representation creation started...");

                //Point indexing 
                var vmap = new Dictionary<int, int>();

                //storage for all points in dtm (will be commit with "sbsm")
                var cpl = new List<IfcCartesianPoint>();

                //Loop to add all points in cpl and vmap
                for (int i = 0, j = 0; i < tin.Points.Count; i++)
                {
                    vmap.Add(i, j);
                    var pt = tin.Points[i];
                    cpl.Add(model.Instances.New<IfcCartesianPoint>(c => c.SetXYZ(pt.X - origin.X, pt.Y - origin.Y, pt.Z - origin.Z)));
                    j++;
                }

                //input for stats (will be logged / gui logged)
                result.wPoints = cpl.Count;

                //logging
                LogWriter.Add(LogType.debug, "CoordList created.");

                //init numFaces for processing results (logging)
                int numFaces = 0;

                //write Shape Representation
                var sbsm = model.Instances.New<IfcShellBasedSurfaceModel>(s =>
                    s.SbsmBoundary.Add(model.Instances.New<IfcOpenShell>(o => o.CfsFaces
                        .AddRange(tin.TriangleVertexPointIndizes().Select(tri => model.Instances.New<IfcFace>(x => x.Bounds
                            .Add(model.Instances.New<IfcFaceOuterBound>(b =>
                            {
                                //Adding an IfcPolyLoop for each triangle (referenced to the respective point number).
                                b.Bound = model.Instances.New<IfcPolyLoop>(p =>
                                {
                                    p.Polygon.Add(cpl[vmap[tri[0]]]);
                                    p.Polygon.Add(cpl[vmap[tri[1]]]);
                                    p.Polygon.Add(cpl[vmap[tri[2]]]);
                                });
                                
                                //count up
                                numFaces++;

                                //Clockwise orientation (true)
                                b.Orientation = true;
                            }))))))));

                //pass processed number of faces to result
                result.wFaces = numFaces;

                //write two remaining output parameter
                representationIdentifier = RepresentationIdentifier.Body;
                representationType = RepresentationType.SurfaceModel;

                //finish transaction
                txn.Commit();

                //logging
                LogWriter.Add(LogType.verbose, "IfcSBSM shape representation created.");

                return sbsm;
            }
        }
    }
}
