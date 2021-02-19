using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc2x3.MeasureResource;              //Enumeration for Unit
using Xbim.Ifc2x3.ProductExtension;             //IfcSite

namespace BIMGISInteropLibs.IFC.Ifc2x3
{
    /// <summary>
    /// provides method to create IfcSite
    /// </summary>
    public static class Site
    {
        /// <summary>
        /// creates site in project
        /// </summary>
        /// <param name="model">Location for all information that will be inserted into the IFC file</param>
        /// <param name="name">Terrain designation</param>
        /// <param name="placement">Parameter provided by "createLocalPlacement"</param>
        /// <param name="refLatitude">Latitude</param>
        /// <param name="refLongitude">Longitude</param>
        /// <param name="refElevation">Height</param>
        /// <param name="compositionType">DO NOT CHANGE</param>
        /// <returns>IfcSite</returns>
        public static IfcSite Create(IfcStore model,
             string name,
             Axis2Placement3D placement = null,
             double? refLatitude = null,
             double? refLongitude = null,
             double? refElevation = null,
             IfcElementCompositionEnum compositionType = IfcElementCompositionEnum.ELEMENT)
        {
            //start transaction (according to ACID)
            using (var txn = model.BeginTransaction("Create Site"))
            {
                //Create new instance
                var site = model.Instances.New<IfcSite>(s =>
                {
                    //set terrain designation
                    s.Name = name;
                    s.CompositionType = compositionType; //DO NOT CHANGE

                    //set latitude and longitude
                    if (refLatitude.HasValue)
                    {
                        s.RefLatitude = IfcCompoundPlaneAngleMeasure.FromDouble(refLatitude.Value);
                    }
                    if (refLongitude.HasValue)
                    {
                        s.RefLongitude = IfcCompoundPlaneAngleMeasure.FromDouble(refLongitude.Value);
                    }
                    s.RefElevation = refElevation;

                    //get placement
                    placement = placement ?? Axis2Placement3D.Standard;
                    //set placement to site
                    s.ObjectPlacement = LocalPlacement.Create(model, placement);
                });
                //commit transaction (acccording to ACID) otherwise the site would not be provided
                txn.Commit();
                //TODO: LOGGING
                //Log.Information("IfcSite created");
                return site;
            }
        }
    }
}