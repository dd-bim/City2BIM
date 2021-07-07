using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using NetTopologySuite.Triangulate;
using Newtonsoft.Json;

namespace BIMGISInteropLibs.NTS
{
    class IfcTerrainTriangulator
    {
        private GeometryFactory geomFactory { get; set; }
        private ConformingDelaunayTriangulationBuilder triangulationBuilder { get; set; }

        public IfcTerrainTriangulator(string jsonPointData, string jsonLineData)
        {
            this.geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
            this.triangulationBuilder = new ConformingDelaunayTriangulationBuilder();
            Triangulate(jsonPointData);
        }

        public string MakeJsonString()
        {
            List<string> jsonTriangleDataList = new List<string>();
            GeometryCollection triangles = this.triangulationBuilder.GetTriangles(this.geomFactory);
            foreach (NetTopologySuite.Geometries.Geometry geom in triangles.Geometries)
            {
                List<string> jsonCoordList = new List<string>();
                Coordinate[] geomCoords = geom.Coordinates;
                foreach (CoordinateZ coordZ in geomCoords)
                {
                    Dictionary<string, double> point = new Dictionary<string, double> { { "x", coordZ.X }, { "y", coordZ.Y }, { "z", coordZ.Z } };
                    jsonCoordList.Add(JsonConvert.SerializeObject(point));
                }
                jsonTriangleDataList.Add(JsonConvert.SerializeObject(jsonCoordList));
            }
            return JsonConvert.SerializeObject(jsonTriangleDataList);
        }

        private void Triangulate(string jsonPointData)
        {
            this.triangulationBuilder.SetSites(this.geomFactory.CreateMultiPoint(MakeCoordinateSequence(jsonPointData)));   
        }

        private CoordinateSequence MakeCoordinateSequence(string jsonPointData)
        {
            return this.geomFactory.CoordinateSequenceFactory.Create(MakeCoordinateArr(jsonPointData));
        }
        
        private Coordinate[] MakeCoordinateArr(string jsonPointData)
        {
            List<CoordinateZ> coordList = new List<CoordinateZ>();
            List<string> jsonPointDataList = JsonConvert.DeserializeObject<List<string>>(jsonPointData);
            foreach (string jsonPoint in jsonPointDataList)
            {
                coordList.Add(JsonConvert.DeserializeObject<CoordinateZ>(jsonPoint));
            }
            return coordList.ToArray();
        }
    }
}
