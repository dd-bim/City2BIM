using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry;
using BimGisCad.Representation.Geometry.Elementary;
using BimGisCad.Representation.Geometry.Composed;
using BimGisCad.Collections;                         //provides MESH --> will be removed

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc2x3.GeometricConstraintResource;  //IfcLocalPlacement
using Xbim.Ifc2x3.GeometryResource;             //IfcAxis2Placement3D
using Xbim.Ifc2x3.Kernel;                       //IfcProject
using Xbim.Common.Step21;                       //Enumeration to XbimShemaVersion
using Xbim.IO;                                  //Enumeration to XbimStoreType
using Xbim.Common;                              //ProjectUnits (Hint: support imperial (TODO: check if required)
using Xbim.Ifc2x3.MeasureResource;              //Enumeration for Unit
using Xbim.Ifc2x3.ProductExtension;             //IfcSite
using Xbim.Ifc2x3.GeometricModelResource;       //IfcShellBasedSurfaceModel or IfcGeometricCurveSet
using Xbim.Ifc2x3.TopologyResource;             //IfcOpenShell
using Xbim.Ifc2x3.RepresentationResource;       //IfcShapeRepresentation

namespace BIMGISInteropLibs.IFC.Ifc2x3
{
    public class ShapeRepresentation
    {
        /// <summary>
        /// Passes the created shape to Model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="item"></param>
        /// <param name="identifier"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IfcShapeRepresentation Create(IfcStore model, IfcGeometricRepresentationItem item, RepresentationIdentifier identifier, RepresentationType type)
        {
            //
            //begin a transaction
            using (var txn = model.BeginTransaction("Create Shaperepresentation"))
            {
                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>(s =>
                {
                    s.ContextOfItems = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                    s.RepresentationType = type.ToString();
                    s.RepresentationIdentifier = identifier.ToString();
                    s.Items.Add(item);
                });

                txn.Commit();
                return shape;
            }
        }

    }
}
