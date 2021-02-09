using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//integration of the BimGisCad library
using BimGisCad.Collections;                        //MESH --> will be removed
using BimGisCad.Representation.Geometry.Elementary; //LINE
using BimGisCad.Representation.Geometry.Composed;   //TIN

namespace BIMGISInteropLibs.IFCTerrain
{
    /// <summary>
    /// Exchange class for processing tins, break edges and error messages
    /// </summary>
    public class Result
    {
        //Error - [TODO] check if necessary
        public string Error { get; set; } = null;

        //MESH is used to pass a MESH - will be removed in the future
        public Mesh Mesh { get; set; } = null;

        //Added for transfer of break edges
        public Dictionary<int, Line3> Breaklines { get; set; } = null;
        
        //Added for transfer of tin
        public Tin Tin { get; set; }
    }
}
