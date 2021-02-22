using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIMGISInteropLibs.IfcTerrain
{
    /// <summary>
    /// supported data types for processing with IFCTerrain
    /// </summary>
    public enum IfcTerrainFileType
    {
        /// <summary>
        /// Drawing Interchange File Format
        /// </summary>
        Dxf,
        /// <summary>
        /// 
        /// </summary>
        LandXml,
        CityGml,
        /// <summary>
        /// GEOgraf A^3 project exchange file format (.out)
        /// </summary>
        Grafbat,
        PostGis,
        Grid,
        Reb,
    }

    
    /// <summary>
    /// Establishes the connection between Reader, Writers, GUI and Command
    /// </summary>
    public class JsonSettings
    {
        #region unspecific file attributes

        #region file handling
        //Read
        /// <summary>
        /// storage location of the file to be converted
        /// </summary>
        public string filePath { get; set; }

        /// <summary>
        /// filetype of the file to be converted
        /// </summary>
        public IfcTerrainFileType fileType { get; set; }

        //Write (IFC)
        /// <summary>
        /// Destination path for IFC file
        /// </summary>
        public string destFileName { get; set; }

        /// <summary>
        /// Sets the IFC version of the output file (IFC2x3; IFC4; ~IFC4dot3~)
        /// </summary>
        public string outIFCType { get; set; }

        /// <summary>
        /// Sets the file format of the output file (Step/XML)
        /// </summary>
        public string outFileType { get; set; }

        /// <summary>
        /// [TODO] Destination location for the log file
        /// </summary>
        public string logFilePath { get; set; }
        
        /// <summary>
        /// Setting of user defined verbosityLevel (TRACE, INFO, DEBUG, ...)
        /// </summary>
        public string verbosityLevel { get; set; }
        #endregion

        #region metadata (mainly for storage in the IFC file).
        /// <summary>
        /// the project name in the output IFC file (IfcProject)
        /// </summary>
        public string projectName { get; set; }

        /// <summary>
        /// The editors family name in the output IFC file
        /// </summary>
        public string editorsFamilyName { get; set; }

        /// <summary>
        /// The editors given name in the output IFC file
        /// </summary>
        public string editorsGivenName { get; set; }

        /// <summary>
        /// The editors organization name in the output IFC file
        /// </summary>
        public string editorsOrganisationName { get; set; }
        #endregion

        #region only used for processing [Settings]
        /// <summary>
        /// Decides whether 2D (false) or 3D (true)
        /// </summary>
        public bool is3D { get; set; }

        /// <summary>
        /// minimum distance (used for IFC writer processing) [TODO].
        /// </summary>
        public double minDist { get; set; }

        /// <summary> 
        ///Sets the terrain model type for the output IFC file: GCS=GeometricCurveSet; SBSM=ShellBasesSurfaceModel; TFS=TriangulatedFaceSet
        /// </summary>
        public string surfaceType { get; set; }

        /// <summary>
        /// Setting, that decides whether the output IFC file should contain an IfcGeographicElement of the terrain or not
        /// </summary>
        public bool geoElement { get; set; }


        #endregion

        //below the required attributes to process the georeferencing
        #region GeoRef - Attributes

        /// <summary>
        /// Describes whether the project coordinate origin should be set to the user defined position or not.
        /// </summary>
        public bool customOrigin { get; set; }

        /// <summary>
        /// x - value of a user defined georeferencing
        /// </summary>
        public double xOrigin { get; set; }

        /// <summary>
        /// y - value of a user defined georeferencing
        /// </summary>
        public double yOrigin { get; set; }

        /// <summary>
        /// z - value of a user defined georeferencing
        /// </summary>
        public double zOrigin { get; set; }
        #endregion
        #endregion

        #region file specific attributes
        
        //for dxf processing
        #region DXF
        /// <summary>
        /// Name of the layer that contains terrain information in an input DXF file
        /// </summary>
        public string layer { get; set; }

        /// <summary>
        /// Describes whether an input DXF file contains tin information (faces) or not
        /// </summary>
        public bool isTin { get; set; }
        #endregion

        //for reb processing
        #region REB
        /// <summary>
        /// Number of the horizon that contains terrain information
        /// </summary>
        public int horizon { get; set; }
        #endregion

        //for elevation grid processing
        #region GRID
        /// <summary>
        /// The distance between points in the grid input file
        /// </summary>
        public int gridSize { get; set; }

        #region BoundingBox
        /// <summary>
        /// Decision whether BoundingBox should be processed (yes = true)
        /// </summary>
        public bool bBox { get; set; }

        /// <summary>
        /// NORTH value of the bounding box
        /// </summary>
        public double bbNorth { get; set; }

        /// <summary>
        /// EAST value of the bounding box
        /// </summary>
        public double bbEast { get; set; }

        /// <summary>
        /// SOUTH value of the bounding box
        /// </summary>
        public double bbSouth { get; set; }

        /// <summary>
        /// WEST value of the bounding box
        /// </summary>
        public double bbWest { get; set; }


        #endregion
        #endregion

        //for grafbat processing
        #region GEOgraf OUT
        /// <summary>
        /// Decides whether all horizons (=false) or only selected ones (=true) are to be used. If filtering is to be used, the entry must be made via "horizonFilter".
        /// </summary>
        public bool onlyHorizon { get; set; }

        /// <summary>
        /// Input only if "onlyHorizon" is true. Designation of specific horizons. Separation via: "/" ";" "," permissible
        /// </summary>
        public int horizonFilter { get; set; }

        /// <summary>
        /// Decides whether all types(=false) or only selected ones(=true) are to be used.If filtering is to be used, the entry must be made via "layer".
        /// </summary>
        public bool onlyTypes { get; set; }

        /// <summary>
        /// Decides whether the status code for the location position should be ignored (=true).
        /// </summary>
        public bool ignPos { get; set; }

        /// <summary>
        /// Decides whether the status code for the height position should be ignored (=true).
        /// </summary>
        public bool ignHeight { get; set; }

        /// <summary>
        /// Name of the layer that contains the breakline. (only one layer is allowed)
        /// </summary>
        public string breakline_layer { get; set; }

        /// <summary>
        /// Decides whether break edges are to be processed(true).
        /// </summary>
        public bool breakline { get; set; }
        #endregion

        //for postgis processing
        #region PostGIS
        /// <summary>
        /// Link to the host database
        /// </summary>
        public string host { get; set; }

        /// <summary>
        /// Specifying the port for the database connection
        /// </summary>
        public int port { get; set; }

        /// <summary>
        /// Specification of the user name for authentication with the database
        /// </summary>
        public string user { get; set; }

        /// <summary>
        /// Specification of the password for authentication with the database
        /// </summary>
        public string password { get; set; }

        /// <summary>
        /// target database
        /// </summary>
        public string database { get; set; }

        /// <summary>
        /// target shema
        /// </summary>
        public string schema { get; set; }

        /// <summary>
        /// Specify the table that contains the TIN
        /// </summary>
        public string tin_table { get; set; }

        /// <summary>
        /// Specify the column that contains the geometry of the TIN
        /// </summary>
        public string tin_column { get; set; }

        /// <summary>
        /// Specify the column that contains the ID of the TIN
        /// </summary>
        public string tinid_column { get; set; }

        /// <summary>
        /// Specification of a TIN ID to be read out
        /// </summary>
        public int tin_id { get; set; }

        /// <summary>
        /// Specify the table that contains the geometry of the break lines
        /// </summary>
        public string breakline_table { get; set; }

        /// <summary>
        /// Specify the column that contains the geometry of the break lines
        /// </summary>
        public string breakline_column { get; set; }

        /// <summary>
        /// Specify the column that contains the TIN ID 
        /// </summary>
        public string breakline_tin_id { get; set; }
        #endregion
        #endregion
    }
}