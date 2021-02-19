using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc4.MeasureResource;                //Enumeration for Unit
using Xbim.Ifc4.ProductExtension;               //IfcSite
using Xbim.Ifc4.Interfaces;                     //IfcElementComposition (ENUM)


namespace BIMGISInteropLibs.IFC.Ifc4
{
    public static class Site
    {
        // <summary>
        // creates site in project
        // </summary>
        // <param name="model">Location for all information that will be inserted into the IFC file</param>
        // <param name="name">Terrain designation</param>
        // <param name="placement">Parameter provided by "createLocalPlacement"</param>
        // <param name="refLatitude">Latitude</param>
        // <param name="refLongitude">Longitude</param>
        // <param name="refElevation">Height</param>
        // <param name="compositionType">DO NOT CHANGE</param>
        // <returns>IfcSite</returns>
         

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="name"></param>
        /// <param name="placement"></param>
        /// <param name="refLatitude"></param>
        /// <param name="refLongitude"></param>
        /// <param name="refElevation"></param>
        /// <param name="compositionType"></param>
        /// <returns></returns>
        public static IfcSite Create(IfcStore model,
             string name,
             Axis2Placement3D placement = null,
             double? refLatitude = null,
             double? refLongitude = null,
             double? refElevation = null,
             IfcElementCompositionEnum compositionType = IfcElementCompositionEnum.ELEMENT)
        {
            using (var txn = model.BeginTransaction("Create Site"))
            {
                var site = model.Instances.New<IfcSite>(s =>
                {
                    s.Name = name;
                    s.CompositionType = compositionType;
                    if (refLatitude.HasValue)
                    {
                        s.RefLatitude = IfcCompoundPlaneAngleMeasure.FromDouble(refLatitude.Value);
                    }
                    if (refLongitude.HasValue)
                    {
                        s.RefLongitude = IfcCompoundPlaneAngleMeasure.FromDouble(refLongitude.Value);
                    }
                    s.RefElevation = refElevation;

                    placement = placement ?? Axis2Placement3D.Standard;
                    s.ObjectPlacement = LocalPlacement.Create(model, placement);
                });

                txn.Commit();
                return site;
            }
        }
    }
}
