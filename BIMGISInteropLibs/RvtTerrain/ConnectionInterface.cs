using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//BimGisCad
using BimGisCad.Collections;                        //MESH - will be removed
using BimGisCad.Representation.Geometry;            //Axis2Placement3D
using BimGisCad.Representation.Geometry.Composed;   //TIN
using BimGisCad.Representation.Geometry.Elementary; //Points, Lines, ...

using BIMGISInteropLibs.IfcTerrain;

using C2BPoint = BIMGISInteropLibs.Geometry.C2BPoint;

namespace BIMGISInteropLibs.RvtTerrain
{
    public class ConnectionInterface
    {
        /// <summary>
        /// TIN (result of the specific file reader)
        /// </summary>
        public Tin Tin { get; private set; }

        public static List<C2BPoint> mapProcess(JsonSettings config)
        {
            //set grid size (as default value)
            config.gridSize = 1;

            //init transfer class
            BIMGISInteropLibs.IfcTerrain.Result resTerrain = new BIMGISInteropLibs.IfcTerrain.Result();

            //mapping on basis of data type
            switch (config.fileType)
            {
                //grid
                case IfcTerrainFileType.Grid:
                    resTerrain = BIMGISInteropLibs.ElevationGrid.ReaderTerrain.ReadGrid(config);
                    break;

                case IfcTerrainFileType.DXF:
                    //resultTerrain = BIMGISInteropLibs.DXF.ReaderTerrain.ReadFile(config.filePath, out DxfFile dxfFile);
                    break;


            }

            dynamic dgmPtList = new List<C2BPoint>();
            if (config.isTin)
            {
                foreach (Point3 p in resTerrain.Tin.Points)
                {
                    dgmPtList.Add(new C2BPoint(p.X, p.Y, p.Z));
                }
            }
            else
            {
                foreach (Point3 p in resTerrain.Mesh.Points)
                {
                    dgmPtList.Add(new C2BPoint(p.X, p.Y, p.Z));
                }
            }
            


            return dgmPtList;
        }

    }
}
