using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Placement requires Axis2Placment3D class provided by BimGisCad Bib
using BimGisCad.Representation.Geometry;

namespace BIMGISInteropLibs.IFC
{
    /// <summary>
    /// Enumeration for supported IfcVersions
    /// </summary>
    public enum IfcVersion{ 
        /// <summary>
        /// IfcVersion 2x3
        /// </summary>
        IFC2x3, 
        /// <summary>
        /// Ifc Version 4
        /// </summary>
        IFC4,
        IFC2x3Tin,
        IFC4Tin,
        /// <summary>
        /// Placeholder for future implementation
        /// </summary>
        IFC4dot3
    }
    /// <summary>
    /// Enumeration for IFC-File-Types
    /// </summary>
    public enum IfcFileType { Step, XML }

    /// <summary>
    /// Enumeration for shape representation
    /// </summary>
    public enum SurfaceType
    {
        GCS, SBSM, TFS, TIN
    }

    /// <summary>
    /// Enumeration for geometric representation
    /// </summary>
    public enum RepresentationType
    {
        SweptSolid, Brep, Clipping, CSG, Curve2D, GeometricCurveSet, Tessellation, SurfaceModel
    }

    /// <summary>
    /// Enumeration to identify representation
    /// </summary>
    public enum RepresentationIdentifier
    {
        Body, Axis, FootPrint, SurveyPoints, Surface
    }

    /// <summary>
    /// Connection Interface for IFC-Writer (used in IFCTerrain)
    /// </summary>
    public class WriteInput
    {
        /// <summary>
        /// Storage location
        /// </summary>
        public string Filename { get; set; } //TODO: rename in fileLocation
        
        /// <summary>
        /// getter and setter for file types
        /// </summary>
        public IfcFileType FileType { get; set; }

        /// <summary>
        /// getter and setter for placment
        /// </summary>
        public Axis2Placement3D Placement { get; set; }

        /// <summary>
        /// getter and setter for surfaceTypes
        /// </summary>
        public SurfaceType SurfaceType { get; set; }

        //TODO check the usage of the following getter and setter
        public double? BreakDist { get; set; }

        public bool WriteGeo { get; set; }
    }
}
