using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry.Elementary; //provides vector

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc2x3.GeometryResource;             //IfcAxis2Placement3D
using Xbim.Ifc2x3.GeometricModelResource;       //IfcShellBasedSurfaceModel or IfcGeometricCurveSet

//Transfer class for the reader (IFCTerrain)
using BIMGISInteropLibs.IfcTerrain;

//embed logging
using BIMGISInteropLibs.Logging; //access to log writer
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

//NTS - geometry types
using NetTopologySuite.Geometries;

namespace BIMGISInteropLibs.IFC.Ifc2x3
{
    /// <summary>
    /// Classes for creating IfcGCS via TIN / MESH
    /// </summary>
    public class GeometricCurveSet
    {
        //IfcGCS using TIN [Status: TODO - breakDist is missing]
        /// <summary>
        /// Creates a DTM via IfcGCS (processing a tin)
        /// </summary>
        /// <param name="model">Location for all information that will be inserted into the IFC file</param>
        /// <param name="origin">Provided by IfcLocalPlacement</param>
        /// <param name="tin">Provided by the different terrain readers</param>
        /// <param name="breakDist"></param>
        /// <param name="representationType">Output - do not change</param>
        /// <param name="representationIdentifier">Output - do not change</param>
        /// <returns>Shape which is written in the IFC file</returns>
        public static IfcGeometricCurveSet Create(IfcStore model, Vector3 origin, Result result,
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
                //IfcCartesianPoints create //TODO: filter points that are not included in the DTM
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

                //write to remaining output parameter
                representationIdentifier = RepresentationIdentifier.SurveyPoints;
                representationType = RepresentationType.GeometricCurveSet;

                //logging
                LogWriter.Add(LogType.verbose, "IfcGCS shape representation created.");

                //commit transaction
                txn.Commit();
                return dtm;
            }
        }

    }
}
