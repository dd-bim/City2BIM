using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//BimGisCad
using BimGisCad.Collections;                        //MESH
using BimGisCad.Representation.Geometry.Composed;   //TIN
using BimGisCad.Representation.Geometry.Elementary; //Points, Lines, ...

//used for result class (may update to seperate class for rvtTerrain)
using BIMGISInteropLibs.IfcTerrain;

//include for data exchange (revit)
using C2BPoint = BIMGISInteropLibs.Geometry.C2BPoint;

//IxMilia: for processing dxf files
using IxMilia.Dxf;

namespace BIMGISInteropLibs.RvtTerrain
{
    public class ConnectionInterface
    {
        /// <summary>
        /// TIN (result of the specific file reader)
        /// </summary>
        public Tin Tin { get; private set; }

        /// <summary>
        /// MESH (result of the specific file reader)
        /// </summary>
        public Mesh Mesh { get; private set; }

        /// <summary>
        /// file reading / tin build process
        /// </summary>
        /// <param name="config">setting to config file processing and conversion process</param>
        /// <returns></returns>
        public static List<C2BPoint> mapProcess(JsonSettings config)
        {
            //set grid size (as default value)
            config.gridSize = 1;

            //init transfer class
            BIMGISInteropLibs.IfcTerrain.Result resTerrain = new BIMGISInteropLibs.IfcTerrain.Result();

            //mapping on basis of data type
            switch (config.fileType)
            {
                //grid reader
                case IfcTerrainFileType.Grid:
                    resTerrain = BIMGISInteropLibs.ElevationGrid.ReaderTerrain.ReadGrid(config);
                    break;

                case IfcTerrainFileType.DXF:
                    //dxf file reader (output used for process terrain information)
                    BIMGISInteropLibs.DXF.ReaderTerrain.ReadFile(config.filePath, out DxfFile dxfFile);

                    //loop for distinguishing whether it is a tin or not (processing via points and lines)
                   
                    //set min dist to default value (TODO)
                    config.minDist = 1;

                    //Mesh Reader (if dxf file contains 3dfaces)
                    resTerrain = DXF.ReaderTerrain.ReadDXFMESH(dxfFile, config);
                    break;

                case IfcTerrainFileType.REB:
                    //REB file reader
                    REB.RebDaData rebData = REB.ReaderTerrain.ReadReb(config.filePath);

                    //use REB data via processing with converter
                    resTerrain = REB.ReaderTerrain.ConvertRebToTin(rebData, config);
                    break;
            }


            //init empty point list
            dynamic dgmPtList = new List<C2BPoint>();

            bool asMesh = true;

            if (resTerrain.Mesh == null)
            {
                asMesh = false;
            }

            if (asMesh)
            {
                foreach (Point3 p in resTerrain.Mesh.Points)
                {
                    dgmPtList.Add(new C2BPoint(p.X, p.Y, p.Z));
                }
            }
            else
            {
                foreach (Point3 p in resTerrain.Tin.Points)
                {
                    {
                        dgmPtList.Add(new C2BPoint(p.X, p.Y, p.Z));
                    }
                }
            }
            


            return dgmPtList;
        }

    }

    
}
