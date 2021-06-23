using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//BimGisCad
using BimGisCad.Representation.Geometry.Elementary; //Points, Lines, ...

//used for result class (may update to seperate class for rvtTerrain)
using BIMGISInteropLibs.IfcTerrain;

//include for data exchange (revit)
using C2BPoint = BIMGISInteropLibs.Geometry.C2BPoint;

//IxMilia: for processing dxf files
using IxMilia.Dxf;

namespace BIMGISInteropLibs.RvtTerrain
{
    /// <summary>
    /// connection between file reader & DTM2BIM writer
    /// </summary>
    public class ConnectionInterface
    {
        /// <summary>
        /// file reading / tin build process
        /// </summary>
        /// <param name="config">setting to config file processing and conversion process</param>
        /// <returns></returns>
        public static Result mapProcess(JsonSettings config, bool useFaces)
        {
            //set grid size (as default value)
            config.gridSize = 1;

            //init transfer class (DTM2BIM)
            Result res = new Result();
            
            //init transfer class (mainly used in ifc terrain)
            IfcTerrain.Result resTerrain = new IfcTerrain.Result();

            #region file reading
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
                   
                    //TODO - check all reader 

                    //Mesh Reader (if dxf file contains 3dfaces)
                    resTerrain = DXF.ReaderTerrain.ReadDXFMESH(dxfFile, config);
                    break;

                case IfcTerrainFileType.REB:
                    //REB file reader
                    REB.RebDaData rebData = REB.ReaderTerrain.ReadReb(config.filePath);

                    //use REB data via processing with converter
                    resTerrain = REB.ReaderTerrain.ConvertRebToTin(rebData, config);
                    break;

                case IfcTerrainFileType.Grafbat:
                    resTerrain = GEOgraf.ReadOUT.ReadOutData(config, out IReadOnlyDictionary<int, int> pointIndex2NumberMap, out IReadOnlyDictionary<int, int> triangleIndex2NumerMap);
                    break;

                //XML
                case IfcTerrainFileType.LandXML:
                    resTerrain = LandXML.ReaderTerrain.ReadTin(config);
                    break;

                case IfcTerrainFileType.CityGML:
                    resTerrain = CityGML.CityGMLReaderTerrain.ReadTin(config);
                    break;
            }
            #endregion file reading

            #region point list
            //init empty point list
            dynamic dgmPtList = new List<C2BPoint>();

            if (resTerrain.Tin.Points == null)
            {
                foreach (Point3 p in resTerrain.Mesh.Points)
                {
                    dgmPtList.Add(new C2BPoint(p.X, p.Y, p.Z));
                }
            }
            else if (resTerrain.Mesh == null)
            {
                foreach (Point3 p in resTerrain.Tin.Points)
                {
                    {
                        dgmPtList.Add(new C2BPoint(p.X, p.Y, p.Z));
                    }
                }
            }
            else
            {
                //TODO error catcher
                return null;
            }

            //set to result point list
            res.dtmPoints = dgmPtList;
            #endregion point list

            //init face list
            dynamic dgmFaceList = new List<DtmFace>();

            if (useFaces)
            {
                if (resTerrain.Tin.Points == null)
                {
                    //set to result facet list
                    foreach (int fe in resTerrain.Mesh.FaceEdges)
                    {
                        int p1 = resTerrain.Mesh.EdgeVertices[fe];
                        int p2 = resTerrain.Mesh.EdgeVertices[resTerrain.Mesh.EdgeNexts[fe]];
                        int p3 = resTerrain.Mesh.EdgeVertices[resTerrain.Mesh.EdgeNexts[resTerrain.Mesh.EdgeNexts[fe]]];

                        //add face index to list
                        dgmFaceList.Add(new DtmFace(p1, p2, p3));
                    }
                }
                else
                {
                    //
                    foreach(var tri in resTerrain.Tin.TriangleVertexPointIndizes())
                    {
                        dgmFaceList.Add(new DtmFace(tri[0], tri[1], tri[2]));
                    }
                }
            }

            res.terrainFaces = dgmFaceList;

            return res;
        }
    }
}
