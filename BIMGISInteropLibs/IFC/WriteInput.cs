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
        /// <summary>
        /// Placeholder for future implementation
        /// </summary>
        IFC4dot3
    }
    /// <summary>
    /// Enumeration for IFC-File-Types
    /// </summary>
    public enum IfcFileType { 
        Step, 
        ifcXML, 
        ifcZip,
        //only cmd export options
        wkt = 17,
        wkb = 18,
        gml2 = 19,
        gml3 = 20
    }

    /// <summary>
    /// Enumeration for shape representation
    /// </summary>
    public enum SurfaceType
    {
        GCS, 
        SBSM, 
        TFS,
        TIN
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
    /// Enumeration to identify Level of GeoRef
    /// </summary>
    public enum LoGeoRef
    {
        /// <summary>
        /// Level of Georeferencing 30 - mainly use of a coordinate <para/>
        /// Need: IfcCartesianPoint; IfcDirection (x,y); IfcAxis2Placement3D; IfcLocalPlacement <para/>
        /// Stored in: IfcSite
        /// </summary>
        LoGeoRef30 = 30,

        /// <summary>
        /// Level of Georefencing 40 - mainly use a coordinate and rotation <para/>
        /// Need: IfcCartesianPoint; IfcDirection (x,y); IfcAxis2Placement3D; IfcGeometricRepresentationContext <para/>
        /// Stored in: IfcProject
        /// </summary>
        LoGeoRef40 = 40,

        /// <summary>
        /// Level of Georefencing 50 - mainly use a coordinate and rotation <para/>
        /// Need: IfcGeometricRepresentationContext, IfcProjectedCRS, IfcSIUnit <para/>
        /// Stored in: IfcProject IfcMapConversion
        /// </summary>
        LoGeoRef50 = 50
    }


    /// <summary>
    /// Connection Interface for IFC-Writer (used in IFCTerrain)
    /// </summary>
    public class WriteInput
    {
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
    }
}
