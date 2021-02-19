using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;
using BimGisCad.Representation.Geometry.Elementary;
using BimGisCad.Representation.Geometry.Composed;
using BimGisCad.Collections;                    //provides MESH --> will be removed

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc4.GeometricConstraintResource;    //IfcLocalPlacement
using Xbim.Ifc4.GeometryResource;               //IfcAxis2Placement3D
using Xbim.Ifc4.Kernel;                         //IfcProject
using Xbim.Common.Step21;                       //Enumeration to XbimShemaVersion
using Xbim.IO;                                  //Enumeration to XbimStoreType
using Xbim.Common;                              //ProjectUnits (Hint: support imperial (TODO: check if required)
using Xbim.Ifc4.MeasureResource;                //Enumeration for Unit
using Xbim.Ifc4.ProductExtension;               //IfcSite
using Xbim.Ifc4.GeometricModelResource;         //IfcShellBasedSurfaceModel or IfcGeometricCurveSet
using Xbim.Ifc4.TopologyResource;               //IfcOpenShell
using Xbim.Ifc4.RepresentationResource;         //IfcShapeRepresentation

namespace BIMGISInteropLibs.IFC.Ifc4
{
    public class ShellBasedSurfaceModel
    {
        //MESH
        private static IfcShellBasedSurfaceModel CreateViaMesh(IfcStore model, Vector3 origin, Mesh mesh,
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {
            if (mesh.MaxFaceCorners < 3)
            { throw new Exception("Mesh has no Faces"); }
            using (var txn = model.BeginTransaction("Create Mesh"))
            {
                var vmap = new Dictionary<int, int>();
                var cpl = new List<IfcCartesianPoint>();
                for (int i = 0, j = 0; i < mesh.Points.Count; i++)
                {
                    if (mesh.VertexEdges[i] < 0)
                    { continue; }
                    vmap.Add(i, j);
                    var pt = mesh.Points[i];
                    cpl.Add(model.Instances.New<IfcCartesianPoint>(c => c.SetXYZ(pt.X - origin.X, pt.Y - origin.Y, pt.Z - origin.Z)));
                    j++;
                }

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
                                b.Orientation = true;
                            }))))))));

                txn.Commit();
                representationIdentifier = RepresentationIdentifier.Body;
                representationType = RepresentationType.SurfaceModel;

                return sbsm;
            }
        }

        //TIN
        /// <summary>
        /// Creates a DTM via IfcSBSM (processing a tin)
        /// </summary>
        /// <param name="model">Location for all information that will be inserted into the IFC file</param>
        /// <param name="origin">Provided by IfcLocalPlacement</param>
        /// <param name="tin">Provided by the different terrain readers</param>
        /// <param name="breakDist"></param>
        /// <param name="representationType">Output - do not change</param>
        /// <param name="representationIdentifier">Output - do not change</param>
        /// <returns>Shape which is written in the IFC file</returns>
        public static IfcShellBasedSurfaceModel CreateViaTin(IfcStore model, Vector3 origin, Tin tin,
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {
            //TODO: add logging
            //start with the transaction (cf. ACID)
            using (var txn = model.BeginTransaction("Create Tin"))
            {
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
                                //Clockwise orientation (true)
                                b.Orientation = true;
                            }))))))));

                //logger.Debug("Processed: " + );
                //Import transaction (according to ACID)
                txn.Commit();
                //write two remaining output parameter
                representationIdentifier = RepresentationIdentifier.Body;
                representationType = RepresentationType.SurfaceModel;

                //TRASHLÖSUNG below: //used only for Logging
                long numTri = ((sbsm.Model.Instances.Count - vmap.Count) / 3) - 10;
                //logger.Debug("Processed: " + vmap.Count + " points; " + numTri + " triangels)");
                return sbsm;
            }
        }
    }
}
