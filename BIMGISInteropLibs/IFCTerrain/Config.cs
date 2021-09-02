﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BIMGISInteropLibs.Logging; //include for verbosity level

namespace BIMGISInteropLibs.IfcTerrain
{
    /// <summary>
    /// supported data types for processing with IFCTerrain
    /// </summary>
    public enum IfcTerrainFileType
    {
        /// <summary>
        /// Drawing Interchange File Format (CAD)
        /// </summary>
        DXF,
        /// <summary>
        /// XML based to exchange of georeferenced objects
        /// </summary>
        LandXML,
        /// <summary>
        /// City Geography Markup Language (OGC)
        /// </summary>
        CityGML,
        /// <summary>
        /// GEOgraf A^3 project exchange file format (.out) [Surveyor]
        /// </summary>
        Grafbat,
        /// <summary>
        /// Database connection to a PostgreSQL databank with a PostGIS essay
        /// </summary>
        PostGIS,
        /// <summary>
        /// Elevation Grid (raster dataset)
        /// </summary>
        Grid,
        /// <summary>
        /// daten arten (raster dataset)
        /// </summary>
        REB,
        /// <summary>
        /// GeoJSON
        /// </summary>
        GeoJSON
    }


    /// <summary>
    /// Establishes the connection between Reader, Writers, GUI and Command
    /// </summary>
    public class Config
    {
        #region unspecific file attributes
        #region file handling
        //Read
        /// <summary>
        /// storage location of the file to be converted
        /// </summary>
        public string filePath { get; set; }

        /// <summary>
        /// name of the file to be converted (without path)
        /// </summary>
        public string fileName { get; set; }

        /// <summary>
        /// filetype of the file to be converted <para/>
        /// [TODO] detect through reader
        /// </summary>
        public IfcTerrainFileType fileType { get; set; }

        //Write (IFC)
        /// <summary>
        /// Destination path for IFC file
        /// </summary>
        public string destFileName { get; set; }

        /// <summary>
        /// Destination location for the log file
        /// </summary>
        public string logFilePath { get; set; }

        /// <summary>
        /// Setting of user defined verbosityLevel
        /// </summary>
        public LogType verbosityLevel { get; set; }

        /// <summary>
        /// Sets the IFC version of the output file (IFC2x3; IFC4; ~IFC4dot3~)
        /// </summary>
        public IFC.IfcVersion outIFCType { get; set; }

        /// <summary> 
        ///Sets the terrain model type for the output IFC file: GCS=GeometricCurveSet; SBSM=ShellBasesSurfaceModel; TFS=TriangulatedFaceSet
        /// </summary>
        public IFC.SurfaceType outSurfaceType { get; set; }

        /// <summary>
        /// Sets the file format of the output file (Step/XML/IfcZip)
        /// </summary>
        public IFC.IfcFileType outFileType { get; set; }
        #endregion

        #region only used for processing [Settings]
        /// <summary>
        /// minimum distance (used for IFC writer processing) [TODO].
        /// </summary>
        public double minDist { get; set; }

        /// <summary>
        /// Setting, that decides whether the output IFC file should contain an IfcGeographicElement of the terrain or not
        /// </summary>
        public bool? geoElement { get; set; }
        #endregion

        #region metadata (mainly for storage in the IFC file).
        /// <summary>
        /// the site name in the out put IFC file (IfcSite)
        /// </summary>
        public string siteName { get; set; }

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

        /// <summary>
        /// Decide whether metadata should be exported as a separate JSON file<para/>
        /// TODO: support of IFCTerrain Command
        /// </summary>
        public bool? exportMetadataFile { get; set; }

        /// <summary>
        /// Decide whehter metadata should be stored as IfcPropertySets
        /// </summary>
        public bool? outIfcPropertySet { get; set; }
        #endregion

        /// <summary>
        /// Decide whether metadata (according to DIN SPEC 91391-2) should be exported
        /// </summary>
        public bool? exportMetadataDin91391 { get; set; }

        /// <summary>
        /// Decide whether metadata (according to DIN 18740-6) should be exported
        /// </summary>
        public bool? exportMetadataDin18740 { get; set; }

        /// <summary>
        /// Decides whether break edges are to be processed(true).
        /// </summary>
        public bool? breakline { get; set; }

        //below the required attributes to process the georeferencing
        #region GeoRef - Attributes

        /// <summary>
        /// Decides which level of georef to use
        /// </summary>
        public IFC.LoGeoRef logeoref { get; set; }

        /// <summary>
        /// Describes whether the project coordinate origin should be set to the user defined position or not.
        /// </summary>
        public bool? customOrigin { get; set; }

        /// <summary>
        /// x - value of a user defined georeferencing
        /// </summary>
        public double? xOrigin { get; set; }

        /// <summary>
        /// y - value of a user defined georeferencing
        /// </summary>
        public double? yOrigin { get; set; }

        /// <summary>
        /// z - value of a user defined georeferencing
        /// </summary>
        public double? zOrigin { get; set; }

        /// <summary>
        /// [LoGeoRef40] true north rotation
        /// </summary>
        public double? trueNorth { get; set; }

        #region LoGeoRef50
        /// <summary>
        /// [LoGeoRef50] scale
        /// </summary>
        public double? scale { get; set; }

        /// <summary>
        /// [LoGeoRef50] Name of the CRS (EPSG-Code)
        /// </summary>
        public int? crsName { get; set; }

        /// <summary>
        /// [LoGeoRef50] Description to the CRS
        /// </summary>
        public string crsDescription { get; set; }

        /// <summary>
        /// [LoGeoRef50] name of the geodetic datum
        /// </summary>
        public string geodeticDatum { get; set; }

        /// <summary>
        /// [LoGeoRef50] name of the vertical datum
        /// </summary>
        public string verticalDatum { get; set; }

        /// <summary>
        /// [LoGeoRef50] name of the projection name
        /// </summary>
        public string projectionName { get; set; }

        /// <summary>
        /// [LoGeoRef50] projection zone
        /// </summary>
        public string projectionZone { get; set; }
        #endregion LoGeoRef50
        #endregion

        #endregion unspecific file attributes
        #region file specific attributes

        //for dxf processing
        #region DXF
        /// <summary>
        /// Name of the layer that contains terrain information in an input DXF file
        /// </summary>
        public string layer { get; set; }

        /// <summary>
        /// read only point data from dxf file
        /// </summary>
        public bool? readPoints { get; set; }
        #endregion

        //for reb processing
        #region REB
        /// <summary>
        /// Number of the horizon that contains terrain information
        /// </summary>
        public int? horizon { get; set; }
        #endregion

        //for elevation grid processing
        #region GRID
        /// <summary>
        /// Decision whether BoundingBox should be processed (yes = true)
        /// </summary>
        public bool? bBox { get; set; }

        /// <summary>
        /// NORTH value of the bounding box
        /// </summary>
        public double? bbNorth { get; set; }

        /// <summary>
        /// EAST value of the bounding box
        /// </summary>
        public double? bbEast { get; set; }

        /// <summary>
        /// SOUTH value of the bounding box
        /// </summary>
        public double? bbSouth { get; set; }

        /// <summary>
        /// WEST value of the bounding box
        /// </summary>
        public double? bbWest { get; set; }
        #endregion

        //for grafbat processing
        #region GEOgraf OUT
        /// <summary>
        /// Decides whether all horizons (=false) or only selected ones (=true) are to be used. If filtering is to be used, the entry must be made via "horizonFilter".
        /// </summary>
        public bool? onlyHorizon { get; set; }

        /// <summary>
        /// Name of the layer that contains the breakline. (only one layer is allowed)
        /// </summary>
        public string breakline_layer { get; set; }
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
        public int? port { get; set; }

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
        public dynamic tin_id { get; set; }

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

        /// <summary>
        /// String to query TIN data via user query
        /// </summary>
        public string queryString {get; set;}

        /// <summary>
        /// string to query breaklines via user query
        /// </summary>
        public string breaklineQueryString {get; set;}

        #endregion

        //for geojson
        #region GeoJSON
        /// <summary>
        /// geometry type to set the right reader
        /// </summary>
        public GeometryType geometryType { get; set; }

        /// <summary>
        /// file path to breakline file (JSON)
        /// </summary>
        public string breaklineFile { get; set; }

        /// <summary>
        /// breakline geometry type to specific reading
        /// </summary>
        public GeometryType breaklineGeometryType { get; set; }
        #endregion GeoJSON
        #endregion
    }

    public enum GeometryType
    {
        MultiPoint,

        MultiPolygon,

        GeometryCollection,

        MultiLineString,

        FeatureCollection

    }

    /// <summary>
    /// storage for json settings (according to standard: DIN SPEC 91391-2)
    /// </summary>
    public class JsonSettings_DIN_SPEC_91391_2
    {
        /// <summary>
        /// unique Identificator (obligatory)
        /// </summary>
        public Guid id { get; set; }

        /// <summary>
        /// file name (obligatory)
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// type (e.g.: DTM) (obligatory)
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// supplementary description (opitonal)
        /// </summary>
        public string description { get; set; }

        /// <summary>
        /// creation date (opitonal)
        /// </summary>
        public string created { get; set; }

        /// <summary>
        /// [leave emtpy] upload date (opitonal)
        /// </summary>
        public string uploaded { get; set; }

        /// <summary>
        /// refernz to creator of the container (optional)
        /// </summary>
        public string creator { get; set; }

        /// <summary>
        /// [leave empty!] refernz to sender of the container (optional)
        /// </summary>
        public string sender { get; set; }

        /// <summary>
        /// [leave empty]: refenz(es) to to receiver (multiple possible) (optional)
        /// </summary>
        public string[] recipients { get; set; }

        /// <summary>
        /// revision number (obligatory)
        /// </summary>
        public string revision { get; set; }

        /// <summary>
        /// version number (optional)
        /// </summary>
        public string version { get; set; }

        /// <summary>
        /// [leave empty]: status of container
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// [leave empty]: contents of the container
        /// </summary>
        public string content { get; set; }

        /// <summary>
        /// [leave empty]: reference to location (download address)
        /// </summary>
        public string location { get; set; }

        /// <summary>
        /// project id (obligatory)
        /// </summary>
        public string projectId { get; set; }

        /// <summary>
        /// reference to schema (for more metadata) (optional)
        /// </summary>
        public string metadataSchema { get; set; }

        /// <summary>
        /// mime type eg.: application/x-step
        /// </summary>
        public string mimeType { get; set; }
    }

    /// <summary>
    /// storage for json settings (according to standard: DIN 18740-6)
    /// </summary>
    public class JsonSettings_DIN_18740_6
    {
        /// <summary>
        /// model typ - here dtm (german: Modelltyp; nur DGM) [Verweis auf: 91391-2?]
        /// </summary>
        public string modelType { get; set; }

        /// <summary>
        /// data structure - here mesh (german: Datenstruktur; nur Dreiecksvermaschung)
        /// </summary>
        public string dataStructure { get; set; }

        /// <summary>
        /// date of the dataset (german: Erfassungsdatum)
        /// </summary>
        public string topicality { get; set; }

        /// <summary>
        /// position reference system (german: Lagereferenzsystem)
        /// </summary>
        public string positionReferenceSystem { get; set; }

        /// <summary>
        /// altitude reference system (german: Höhenreferenzsystem)
        /// </summary>
        public string altitudeReferenceSystem { get; set; }

        /// <summary>
        /// projection
        /// </summary>
        public string projection { get; set; }

        /// <summary>
        /// spatial expansion (german: Gebietsausdehnung)
        /// </summary>
        public string spatialExpansion { get; set; }

        /// <summary>
        /// deviation - location (german: Abweichung - Lage)
        /// </summary>
        public string deviationPosition { get; set; }

        /// <summary>
        /// deviation - height (german: Abweichung - Höhe)
        /// </summary>
        public string deviationAltitude { get; set; }

    }
}