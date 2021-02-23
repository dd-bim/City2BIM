using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//integration of the BimGisCad library
using BimGisCad.Collections;                        //MESH --> will be removed
using BimGisCad.Representation.Geometry.Elementary; //LINE
using BimGisCad.Representation.Geometry.Composed;   //TIN

using IxMilia.Dxf; //for DXF processing

namespace BIMGISInteropLibs.IfcTerrain
{
    /// <summary>
    /// Exchange class for processing tins, break edges and error messages
    /// </summary>
    public class Result
    {
        /// <summary>
        /// transferclass for error
        /// </summary>
        public string Error { get; set; } = null;

        /// <summary>
        /// transfer class of an MESH (WILL BE REMOVED)
        /// </summary>
        public Mesh Mesh { get; set; } = null;

        /// <summary>
        /// transfer class of break edges
        /// </summary>
        public Dictionary<int, Line3> Breaklines { get; set; } = null;

        /// <summary>
        /// transfer class for tin
        /// </summary>
        public Tin Tin { get; set; }
    }
}
