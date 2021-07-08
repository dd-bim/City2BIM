using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;            //handle placement
using BimGisCad.Representation.Geometry.Elementary; //need to calc rotation

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc4.GeometryResource;               //IfcAxis2Placement3D
using Xbim.Ifc4.RepresentationResource;         //IfcGeometricRepresentationContext

//embed logging
using BIMGISInteropLibs.Logging;                                 //need for LogPair
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace BIMGISInteropLibs.IFC.Ifc4.LoGeoRef
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
        public static IfcGeometricRepresentationContext Create(IfcStore model, Axis2Placement3D placement, double inputTrueNorth)
        {
            //start transaction
            using (var txn = model.BeginTransaction("Create Geometric Representation Context [LoGeoRef40]"))
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
                    LogWriter.Add(LogType.verbose, "IfcCartesianPoint (Easting: " + p.Location.X + "; Northing: " + p.Location.Y + "; Height: " + p.Location.Z + ") set!");
                   
                    //create IfcDirection - Axis
                    p.Axis = model.Instances.New<IfcDirection>(a => a.SetXYZ(placement.Axis.X, placement.Axis.Y, placement.Axis.Z));
                    LogWriter.Add(LogType.verbose, "IfcDirection - Axis (Easting: " + p.Axis.X + "; Northing: " + p.Axis.Y + "; Height: " + p.Axis.Z + ") set!");

                    //create IfcDirection - RefDir
                    p.RefDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(placement.RefDirection.X, placement.RefDirection.Y, placement.RefDirection.Z));
                    LogWriter.Add(LogType.verbose, "IfcDirection - refDirection (Easting: " + p.RefDirection.X + "; Northing: " + p.RefDirection.Y + "; Height: " + p.RefDirection.Z + ") set!");
                });

                //set true north prepared for ifc file
                grc.TrueNorth = model.Instances.OfType<IfcDirection>().LastOrDefault();

                //commit otherwise need to roll back
                txn.Commit();
                return grc;
            }
        } 
    }
}
