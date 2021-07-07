﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;            //handle placement
using BimGisCad.Representation.Geometry.Elementary; //need to calc rotation

//embed Xbim                                        //below selected examples that show why these are included
using Xbim.Ifc;                                     //IfcStore
using Xbim.Ifc4.GeometricConstraintResource;        //IfcLocalPlacement
using Xbim.Ifc4.GeometryResource;                   //IfcAxis2Placement3D
using Xbim.Ifc4.RepresentationResource;             //Geometric Representation Context
using Xbim.Ifc4.MeasureResource;                    //IfcNamedUnit

using BIMGISInteropLibs.IfcTerrain;                 //embed for json settings

//TODO logging

namespace BIMGISInteropLibs.IFC.Ifc4.LoGeoRef
{
    /// <summary>
    /// Class to create LoGeoRef50
    /// </summary>
    public static class Level50
    {
        public static IfcGeometricRepresentationContext Create(IfcStore model, Axis2Placement3D placement, JsonSettings jsonSettings)
        {
            //start transaction
            using(var txn = model.BeginTransaction("Create Geometric Representation Context[LoGeoRef50]"))
            {
                //get entity
                var geomRepContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();

                //create new instance for map conversion
                var mapConversion = model.Instances.New<IfcMapConversion>();

                //x value
                mapConversion.Eastings = placement.Location.X;

                //y value
                mapConversion.Northings = placement.Location.Y;

                //z value
                mapConversion.OrthogonalHeight = placement.Location.Z;

                //init dynamic rotation vector
                dynamic rotationVector;

                //error handling
                if (jsonSettings.trueNorth.Equals(0) | jsonSettings.trueNorth.Equals(null))
                {
                    //default value
                    rotationVector = AzimuthToVector(0);
                }
                else
                {
                    //calculate rotation vector from true north
                    rotationVector = AzimuthToVector(jsonSettings.trueNorth);
                }

                //x axis abscissa:
                mapConversion.XAxisAbscissa = rotationVector[1];

                //y axis ordinate: 
                mapConversion.XAxisOrdinate = rotationVector[0];

                //scale
                mapConversion.Scale = jsonSettings.scale;
                
                //pass map conversion to geom rep context
                mapConversion.SourceCRS = geomRepContext;

                //create instance to write projected crs
                var projCRS = model.Instances.New<IfcProjectedCRS>();
                
                //crs name passing EPSG Code
                projCRS.Name = "EPSG:"+ jsonSettings.crsName;

                //crs description
                projCRS.Description = jsonSettings.crsDescription;

                //pass geodetic datum
                projCRS.GeodeticDatum = jsonSettings.geodeticDatum;

                //vertical datum
                projCRS.VerticalDatum = jsonSettings.verticalDatum;

                //map projection
                projCRS.MapProjection = jsonSettings.projectionName;

                //zones  identifier (e.g. 33)
                projCRS.MapZone = jsonSettings.projectionZone;

                //ref to unit entity
                projCRS.MapUnit = model.Instances.FirstOrDefault<IfcNamedUnit>();

                //pass to target crs
                mapConversion.TargetCRS = projCRS;
                
                //commit TODO add tests which do a roll back
                txn.Commit();

                return geomRepContext;
            }
        }

        /// <summary>
        /// support function to calclue azimuth to vector
        /// </summary>
        private static double[] AzimuthToVector(double azi)
        {
            var razi = DegToRad(azi);
            return new[] { Math.Cos(razi), Math.Sin(razi) };
        }

        /// <summary>
        /// support to calc rho
        /// </summary>
        private static readonly double RevRho = Math.PI / 180.0;

        /// <summary>
        /// calc deg to rad
        /// </summary>
        private static double DegToRad(double deg) => deg * RevRho;
    }
}
