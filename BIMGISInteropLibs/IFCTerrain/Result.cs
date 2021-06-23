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
        /// transfer class of an MESH
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

        /// <summary>
        /// Number of points read
        /// </summary>
        public int rPoints { get; set; }

        /// <summary>
        /// Number of points processed
        /// </summary>
        public int wPoints { get; set; }

        /// <summary>
        /// Number of lines read
        /// </summary>
        public int rLines { get; set; }

        /// <summary>
        /// Number of lines processed
        /// </summary>
        public int wLines { get; set; }
        
        /// <summary>
        /// Number of lines read
        /// </summary>
        public int rFaces { get; set; }

        /// <summary>
        /// Number of lines processed
        /// </summary>
        public int wFaces { get; set; }
    }
}
