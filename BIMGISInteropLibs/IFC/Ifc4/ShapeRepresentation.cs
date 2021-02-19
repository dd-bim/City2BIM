using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc;                                 //IfcStore
using Xbim.Ifc4.GeometryResource;               //IfcAxis2Placement3D
using Xbim.Ifc4.RepresentationResource;         //IfcShapeRepresentation

namespace BIMGISInteropLibs.IFC.Ifc4
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
        public static IfcShapeRepresentation Create(Xbim.Ifc.IfcStore model, IfcGeometricRepresentationItem item, RepresentationIdentifier identifier, RepresentationType type)
        {
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
