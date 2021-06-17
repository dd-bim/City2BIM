using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BIMGISInteropLibs.IfcTerrain;

namespace BIMGISInteropLibs.RvtTerrain
{
    public class ConnectionInterface
    {
        public static BIMGISInteropLibs.IfcTerrain.Result mapProcess(JsonSettings config)
        {
            //set grid size (as default value)
            config.gridSize = 1;

            //init transfer class
            var resTerrain = new BIMGISInteropLibs.IfcTerrain.Result();

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





            return resTerrain;
        }

    }
}
