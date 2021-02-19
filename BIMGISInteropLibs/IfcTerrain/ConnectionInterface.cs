using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//BimGisCad
using BimGisCad.Collections;                        //MESH - will be removed
using BimGisCad.Representation.Geometry.Composed;   //TIN
using BimGisCad.Representation.Geometry.Elementary; //Points, Lines, ...


namespace BIMGISInteropLibs.IFC.IfcTerrain
{
    public class ConnectionInterface
    {
        //initialize TIN / MESH / Breaklines
        public Mesh Mesh { get; private set; } = null; //[TODO] remove, when no longer needed
        public Tin Tin { get; private set; }
        public Dictionary<int, Line3> Breaklines { get; private set; } = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jSettings"></param>
        /// <param name="breakDist"></param>
        /// <param name="refLatitude"></param>
        /// <param name="refLongitude"></param>
        /// <param name="refElevation"></param>
        public void mapProcess(BIMGISInteropLibs.IfcTerrain.JsonSettings jSettings, double? breakDist = null, double? refLatitude = null, double? refLongitude = null, double? refElevation = null)
        {

        }
        
    }
}
