using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc2x3.GeometryResource;               //IfcAxis2Placement3D
using Xbim.Ifc2x3.RepresentationResource;         //IfcGeometricRepresentationContext

//embed logging
using BIMGISInteropLibs.Logging;                                 //need for LogPair
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace BIMGISInteropLibs.IFC.Ifc2x3.LoGeoRef
{
    /// <summary>
    /// Class to create LoGeoRef40
    /// </summary>
    public static class Level40
    {
        /// <summary>
        /// Create IfcGeometricRepresContext
        /// </summary>
        /// <param name="model">store of whole ifc model</param>
        /// <param name="placement">input placement</param>
        public static IfcGeometricRepresentationContext Create(IfcStore model, Axis2Placement3D placement)
        {
            //start transaction
            using (var txn = model.BeginTransaction("Create Geometric Representation Context"))
            {
                //get entity
                var grc = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();

                //set World Coordinate System
                grc.WorldCoordinateSystem =

                //create IfcAxis2Placement3D
                model.Instances.New<IfcAxis2Placement3D>(p =>
                {
                    //create IfcCartesianPoint by setting x y z
                    p.Location = model.Instances.New<IfcCartesianPoint>(c => c.SetXYZ(placement.Location.X, placement.Location.Y, placement.Location.Z));
                    LogWriter.Entries.Add(new LogPair(LogType.verbose, "IfcCartesianPoint (Easting: " + p.Location.X + "; Northing: " + p.Location.Y + "; Height: " + p.Location.Z + ") set!"));

                    //create IfcDirection - RefDir
                    p.RefDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(placement.RefDirection.X, placement.RefDirection.Y, placement.RefDirection.Z));
                    LogWriter.Entries.Add(new LogPair(LogType.verbose, "IfcDirection - refDirection (Easting: " + p.RefDirection.X + "; Northing: " + p.RefDirection.Y + "; Height: " + p.RefDirection.Z + ") set!"));

                    //create IfcDirection - Axis
                    p.Axis = model.Instances.New<IfcDirection>(a => a.SetXYZ(placement.Axis.X, placement.Axis.Y, placement.Axis.Z));
                    LogWriter.Entries.Add(new LogPair(LogType.verbose, "IfcDirection - Axis (Easting: " + p.Axis.X + "; Northing: " + p.Axis.Y + "; Height: " + p.Axis.Z + ") set!"));
                });

                //set True North (via XY - Axis)
                grc.TrueNorth = model.Instances.New<IfcDirection>(a => a.SetXY(placement.Axis.X, placement.Axis.Y));
                LogWriter.Entries.Add(new LogPair(LogType.verbose, "True north set - Axis (Easting: " + grc.TrueNorth.X + "; Northing: " + grc.TrueNorth.Y + "; set!"));

                //commit otherwise need to roll back
                txn.Commit();
                return grc;
            }
        }
    }
}
