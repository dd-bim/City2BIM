using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed BimGisCad
using BimGisCad.Representation.Geometry.Elementary; //provides Vector

using Xbim.Ifc;
using Xbim.Ifc4.GeometricModelResource;

//Transfer class for the reader (IFCTerrain)
using BIMGISInteropLibs.IfcTerrain;

//NTS - geometry types
using NetTopologySuite.Geometries;

namespace BIMGISInteropLibs.IFC.Ifc4
{
    public class TriangulatedIrregularNetwork
    {
        public static IfcTriangulatedIrregularNetwork Create(
            IfcStore model, Vector3 origin, Result result,
            out RepresentationType representationType,
            out RepresentationIdentifier representationIdentifier)
        {
            using (var txn = model.BeginTransaction("Create TIN"))
            {
                //get unique coord list 
                CoordinateList coordinates = result.coordinateList;

                //init empty dictionray
                var vmap = new Dictionary<int, int>();

                //coordinate point list
                var cpl = model.Instances.New<IfcCartesianPointList3D>(c =>
                {
                    for (int i = 0, j = 0; i < coordinates.Count; i++)
                    {
                    //add point to map
                    vmap.Add(i, j + 1);

                    //get current point
                    var pt = coordinates[i];

                    //add point to coordList with xyz values
                    var coo = c.CoordList.GetAt(j++);
                        coo.Add(pt.X - origin.X);
                        coo.Add(pt.Y - origin.Y);
                        coo.Add(pt.Z - origin.Z);
                    }
                });

                //get index map for processed triangles
                var triMap = result.triMap;

                var tin = model.Instances.New<IfcTriangulatedIrregularNetwork>(t =>
                {
                    t.Closed = false;

                    t.Coordinates = cpl;

                    int pos = 0;

                    //loop through each triangle in triangle map to get int value of point (index)
                    foreach (var triangle in triMap)
                    {
                        //get posotion
                        var fi = t.CoordIndex.GetAt(pos++);

                        //get values & add them
                        fi.Add(vmap[triangle.triValues[0]]);
                        fi.Add(vmap[triangle.triValues[1]]);
                        fi.Add(vmap[triangle.triValues[2]]);
                    }
                 
                });
                
                //describe the representation
                representationIdentifier = RepresentationIdentifier.Body;
                representationType = RepresentationType.Tessellation;

                txn.Commit();

                return tin;
            }
        }

    }
}
