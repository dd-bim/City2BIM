using System.Collections.Generic;
using System.Linq;

//embed BimGisCad
using BimGisCad.Representation.Geometry.Elementary; //provides Vector

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc4.GeometryResource;               //IfcAxis2Placement3D
using Xbim.Ifc4.GeometricModelResource;         //IfcShellBasedSurfaceModel or IfcGeometricCurveSet

//Transfer class for the reader (IFCTerrain)
using BIMGISInteropLibs.IfcTerrain;

//embed logging
using BIMGISInteropLibs.Logging; //access to log writer
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

//NTS - geometry types
using NetTopologySuite.Geometries;

namespace BIMGISInteropLibs.IFC.Ifc4
{
    /// <summary>
    /// Classes for creating IfcGCS via TIN / MESH
    /// </summary>
    public class GeometricCurveSet
    {
        public static IfcGeometricCurveSet Create(Xbim.Ifc.IfcStore model, Vector3 origin, Result result,
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {
            //get index map for processed triangles
            var triMap = result.triMap;

            //get unique coord list 
            CoordinateList coordinates = result.coordinateList;

            //logging
            LogWriter.Add(LogType.verbose, "IfcGCS shape representation creation started...");

            //begin a transaction
            using (var txn = model.BeginTransaction("Create DTM"))
            {
                //IfcCartesianPoints create
                var cps = coordinates.Select(
                    p => model.Instances.New<IfcCartesianPoint>(
                        c => c.SetXYZ(
                            p.X - origin.X, 
                            p.Y - origin.Y,
                            p.Z - origin.Z))).ToList();

                //create IfcGCS instance
                var dtm = model.Instances.New<IfcGeometricCurveSet>(g =>
                {
                    //read out each triangle
                    foreach (var triangle in triMap)
                    {
                        //first edge
                        g.Elements.Add(model.Instances.New<IfcPolyline>(
                            p => p.Points.AddRange(new[] { cps[triangle.triValues[0]], cps[triangle.triValues[1]] })));

                        //next edge
                        g.Elements.Add(model.Instances.New<IfcPolyline>(
                            p => p.Points.AddRange(new[] { cps[triangle.triValues[1]], cps[triangle.triValues[2]] })));

                        //last edge (closing triangle)
                        g.Elements.Add(model.Instances.New<IfcPolyline>(
                            p => p.Points.AddRange(new[] { cps[triangle.triValues[2]], cps[triangle.triValues[0]] })));
                    }
                });

                //write two remaining output parameter
                representationIdentifier = RepresentationIdentifier.SurveyPoints;
                representationType = RepresentationType.GeometricCurveSet;

                //logging
                LogWriter.Add(LogType.verbose, "IfcGCS shape representation created.");

                //commit transaction - ACID (otherwise rollback)
                txn.Commit();
                return dtm;
            }
        }
    }
}
