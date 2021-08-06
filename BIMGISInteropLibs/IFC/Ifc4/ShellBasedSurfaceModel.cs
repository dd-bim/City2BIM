using System.Collections.Generic;
using System.Linq;

//embed BimGisCad
using BimGisCad.Representation.Geometry.Elementary; //provides Vector

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc4.GeometryResource;               //IfcAxis2Placement3D
using Xbim.Ifc4.GeometricModelResource;         //IfcShellBasedSurfaceModel or IfcGeometricCurveSet
using Xbim.Ifc4.TopologyResource;               //IfcOpenShell

//Transfer class for the reader (IFCTerrain)
using BIMGISInteropLibs.IfcTerrain;

//embed logging
using BIMGISInteropLibs.Logging; //access to log writer
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

//NTS
using NetTopologySuite.Geometries;

namespace BIMGISInteropLibs.IFC.Ifc4
{
    /// <summary>
    /// shape representation class<para/>
    /// Shell Based Surface Model
    /// </summary>
    public class ShellBasedSurfaceModel
    {
        public static IfcShellBasedSurfaceModel Create(Xbim.Ifc.IfcStore model, Vector3 origin, Result result,
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {
            //get index map for processed triangles
            var triMap = result.triMap;

            //get unique coord list 
            CoordinateList coordinates = result.coordinateList;

            //start with the transaction (cf. ACID)
            using (var txn = model.BeginTransaction("Create Tin"))
            {
                //logging
                LogWriter.Add(LogType.verbose, "IfcSBSM shape representation creation started...");

                //Point indexing 
                var vmap = new Dictionary<int, int>();

                //storage for all points in dtm (will be commit with "sbsm")
                var cpl = new List<IfcCartesianPoint>();

                //loop to add all points in cpl and vmap
                for (int i = 0, j = 0; i < coordinates.Count; i++)
                {
                    vmap.Add(i, j);
                    var pt = coordinates[i];
                    cpl.Add(model.Instances.New<IfcCartesianPoint>(
                        c => c.SetXYZ(
                            pt.X - origin.X, 
                            pt.Y - origin.Y, 
                            pt.Z - origin.Z)
                        ));
                    j++;
                }

                //logging
                LogWriter.Add(LogType.debug, "CoordList created.");

                //write Shape Representation
                var sbsm = model.Instances.New<IfcShellBasedSurfaceModel>(s =>
                    s.SbsmBoundary.Add(model.Instances.New<IfcOpenShell>(o => o.CfsFaces
                        .AddRange(triMap.Select(tri => model.Instances.New<IfcFace>(x => x.Bounds
                            .Add(model.Instances.New<IfcFaceOuterBound>(b =>
                            {
                                //Adding an IfcPolyLoop for each triangle (referenced to the respective point number).
                                b.Bound = model.Instances.New<IfcPolyLoop>(p =>
                                {
                                    p.Polygon.Add(cpl[vmap[tri.triValues[0]]]);
                                    p.Polygon.Add(cpl[vmap[tri.triValues[1]]]);
                                    p.Polygon.Add(cpl[vmap[tri.triValues[2]]]);
                                });

                                //Clockwise orientation (true)
                                b.Orientation = true;
                            }))))))));

                //write two remaining output parameter
                representationIdentifier = RepresentationIdentifier.Body;
                representationType = RepresentationType.SurfaceModel;

                //finish transaction (according to ACID)
                txn.Commit();

                //logging
                LogWriter.Add(LogType.verbose, "IfcSBSM shape representation created.");

                return sbsm;
            }
        }
    }
}
