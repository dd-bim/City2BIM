using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed Xbim                                    //below selected examples that show why these are included
using Xbim.Ifc4.GeometryResource;               //IfcGeometricRepresentationItem
using Xbim.Ifc4.RepresentationResource;         //IfcShapeRepresentation

//Logging
using BIMGISInteropLibs.Logging;                                 //need for LogPair
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

namespace BIMGISInteropLibs.IFC.Ifc4
{
    /// <summary>
    /// class to create shape representation (IFC4)
    /// </summary>
    public class ShapeRepresentation
    {
        /// <summary>
        /// Passes the created shape to model
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
                //Create a definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>(s =>
                {
                    //get instance of the shape
                    s.ContextOfItems = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                    
                    //write reprs type (REQUIRED)
                    s.RepresentationType = type.ToString();
                    
                    //write identifier (REQUIRED)
                    s.RepresentationIdentifier = identifier.ToString();

                    //add repres items (TFS / GCS / SBSM) to shape
                    s.Items.Add(item);
                });

                //commit transaction
                txn.Commit();
                LogWriter.Add(LogType.debug, "IFC shape representation commited.");
                //return shape
                return shape;
            }
        }
    }
}
