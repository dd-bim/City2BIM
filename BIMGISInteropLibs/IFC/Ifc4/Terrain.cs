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
using Xbim.Ifc4.Interfaces;                     //Enum for Units
using Xbim.Ifc4.RepresentationResource;         //IfcShapeRepresentation


namespace BIMGISInteropLibs.IFC.Ifc4
{
    /// <summary>
    /// 
    /// </summary>
    public class Terrain
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="name"></param>
        /// <param name="tag"></param>
        /// <param name="placement"></param>
        /// <param name="representation"></param>
        /// <returns></returns>
        public static IfcGeographicElement Create(IfcStore model, IfcLabel name, IfcIdentifier tag, Axis2Placement3D placement, IfcShapeRepresentation representation)
        {
            //begin a transaction
            using (var txn = model.BeginTransaction("Create Terrain"))
            {
                // Gelände
                var terrain = model.Instances.New<IfcGeographicElement>(s =>
                {
                    s.Name = name;
                    s.PredefinedType = IfcGeographicElementTypeEnum.TERRAIN;
                    s.Tag = tag;
                    placement = placement ?? Axis2Placement3D.Standard;
                    s.ObjectPlacement = LocalPlacement.Create(model, placement);
                    s.Representation = model.Instances.New<IfcProductDefinitionShape>(r => r.Representations.Add(representation));
                });

                txn.Commit();

                return terrain;
            }
        }
    }
}
