using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//BimGisCad
using BimGisCad.Representation.Geometry.Elementary; //Points, Lines, ...

//used for result class (may update to seperate class for rvtTerrain)
using BIMGISInteropLibs.IfcTerrain;

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
        public static Result mapProcess(Config config, Result.conversionEnum processingEnum)
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
                    throw new NotImplementedException();

                case IfcTerrainFileType.DXF:
                    throw new NotImplementedException();

                case IfcTerrainFileType.REB:
                    throw new NotImplementedException();

                case IfcTerrainFileType.Grafbat:
                    throw new NotImplementedException();

                case IfcTerrainFileType.LandXML:
                    throw new NotImplementedException();

                case IfcTerrainFileType.CityGML:
                    throw new NotImplementedException();

                case IfcTerrainFileType.PostGIS:
                    throw new NotImplementedException();
            }
            #endregion file reading
            /*
            #region point list
            //init empty point list
            dynamic dgmPtList = new List<Point3>();

            if (resTerrain.Tin.Points == null)
            {
                foreach (Point3 p in resTerrain.Mesh.Points)
                {
                    dgmPtList.Add(p);
                }
            }
            else if (resTerrain.Mesh == null)
            {
                foreach (Point3 p in resTerrain.Tin.Points)
                {
                    {
                        dgmPtList.Add(p);
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
            
            if (processingEnum == Result.conversionEnum.ConversionViaFaces)
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
            */
            return res;
        }
    }
}
