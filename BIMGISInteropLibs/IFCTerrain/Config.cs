using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BIMGISInteropLibs.Logging; //include for verbosity level
using System.ComponentModel;//interface property changed
using Newtonsoft.Json; //to ignore 'private' output fields

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
    public class Config : INotifyPropertyChanged
    {
        #region data binding
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion data binding

        #region unspecific file attributes
        #region file handling

        [JsonIgnore]
        private string _filePath { get; set; }

        /// <summary>
        /// storage location of the file to be converted
        /// </summary>
        public string filePath
        {
            get { return _filePath; }
            set
            {
                _filePath = value;
                NotifyPropertyChanged(nameof(filePath));
            }
        }

        [JsonIgnore]
        private string _fileName { get; set; }

        /// <summary>
        /// name of the file to be converted (without path)
        /// </summary>
        public string fileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                NotifyPropertyChanged(nameof(fileName));
            }
        }

        [JsonIgnore]
        private IfcTerrainFileType _fileType { get; set; }

        /// <summary>
        /// filetype of the file to be converted
        /// </summary>
        public IfcTerrainFileType fileType 
        {
            get { return _fileType; }
            set
            {
                _fileType = value;
                NotifyPropertyChanged(nameof(fileType));
            }
        }

        [JsonIgnore]
        private string _destFileName { get; set; }

        /// <summary>
        /// destination path for IFC file
        /// </summary>
        public string destFileName
        {
            get { return _destFileName; }
            set
            {
                _destFileName = value;
                NotifyPropertyChanged(nameof(destFileName));
            }
        }
        /// <summary>
        /// Destination location for the log file
        /// </summary>
        public string logFilePath { get; set; }

        /// <summary>
        /// init value is set to info
        /// </summary>
        [JsonIgnore]
        private LogType _verbosityLevel { get; set; } = LogType.info;

        /// <summary>
        /// Setting of user defined verbosityLevel
        /// </summary>
        public LogType verbosityLevel
        {
            get { return _verbosityLevel; }
            set
            {
                _verbosityLevel = value;
                NotifyPropertyChanged(nameof(verbosityLevel));
            }
        }

        /// <summary>
        /// init to IFC schema version 4
        /// </summary>
        [JsonIgnore]
        private IFC.IfcVersion _outIFCType { get; set; } = IFC.IfcVersion.IFC4;

        /// <summary>
        /// Sets the IFC version of the output file (IFC2x3; IFC4; ~IFC4dot3~)
        /// </summary>
        public IFC.IfcVersion outIFCType
        {
            get { return _outIFCType; }
            set
            {
                _outIFCType = value;
                NotifyPropertyChanged(nameof(outIFCType));
            }
        }

        /// <summary>
        /// init to SBSM (most bim authoring softwares can read that)
        /// </summary>
        [JsonIgnore]
        private IFC.SurfaceType _outSurfaceType { get; set; } = IFC.SurfaceType.SBSM;

        /// <summary> 
        ///Sets the terrain model type for the output IFC file: GCS=GeometricCurveSet; SBSM=ShellBasesSurfaceModel; TFS=TriangulatedFaceSet
        /// </summary>
        public IFC.SurfaceType outSurfaceType
        {
            get { return _outSurfaceType; }
            set
            {
                _outSurfaceType = value;
                NotifyPropertyChanged(nameof(outSurfaceType));
            }
        }

        [JsonIgnore]
        private IFC.IfcFileType _outFileType { get; set; }

        /// <summary>
        /// Sets the file format of the output file (Step/XML/IfcZip)
        /// </summary>
        public IFC.IfcFileType outFileType 
        { 
            get { return _outFileType; }
            set
            {
                _outFileType = value;
                NotifyPropertyChanged(nameof(outFileType));
            }
        }
        #endregion

        #region only used for processing [Settings]
        /// <summary>
        /// minimum distance (used for IFC writer processing) [TODO].
        /// </summary>
        public double minDist { get; set; }


        [JsonIgnore]
        public bool? _geoElement { get; set; } = false;
        /// <summary>
        /// Setting, that decides whether the output IFC file should contain an IfcGeographicElement of the terrain or not
        /// </summary>
        public bool? geoElement
        {
            get
            {
                return _geoElement;
            }
            set
            {
                _geoElement = value;
                NotifyPropertyChanged(nameof(geoElement));
            }
        }

        #endregion

        #region metadata (mainly for storage in the IFC file).
        [JsonIgnore]
        private string _siteName { get; set; } = "Terrain";
        
        /// <summary>
        /// the site name in the out put IFC file (IfcSite)
        /// </summary>
        public string siteName 
        {
            get { return _siteName; }
            set
            {
                _siteName = value;
                NotifyPropertyChanged(nameof(siteName));
            }
        }

        [JsonIgnore]
        private string _projectName { get; set; } = "Project title";

        /// <summary>
        /// the project name in the output IFC file (IfcProject)
        /// </summary>
        public string projectName
        {
            get { return _projectName; }
            set
            {
                _projectName = value;
                NotifyPropertyChanged(nameof(projectName));
            }
        }

        [JsonIgnore]
        private string _editorsOrganisationName { get; set; } = "HTW Dresden - DD BIM";

        /// <summary>        
        /// The editors organization name in the output IFC file
        /// </summary>
        public string editorsOrganisationName
        {
            get { return _editorsOrganisationName; }
            set
            {
                _editorsOrganisationName = value;
                NotifyPropertyChanged(nameof(editorsOrganisationName));
            }
        }

        [JsonIgnore]
        private string _editorsFamilyName { get; set; } = "...";

        /// <summary>        
        /// The editors family name in the output IFC file
        /// </summary>
        public string editorsFamilyName
        {
            get { return _editorsFamilyName; }
            set
            {
                _editorsFamilyName = value;
                NotifyPropertyChanged(nameof(editorsFamilyName));
            }
        }

        [JsonIgnore]
        private string _editorsGivenName { get; set; } = "...";

        /// <summary>        
        /// The editors given name in the output IFC file
        /// </summary>
        public string editorsGivenName
        {
            get { return _editorsGivenName; }
            set
            {
                _editorsGivenName = value;
                NotifyPropertyChanged(nameof(editorsGivenName));
            }
        }

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

        [JsonIgnore]
        private bool? _breakline { get; set; } = false;

        /// <summary>
        /// Decides whether break edges are to be processed(true).
        /// </summary>
        public bool? breakline
        {
            get
            {
                return _breakline;
            }
            set
            {
                _breakline = value;
                NotifyPropertyChanged(nameof(breakline));
            }
        }

        [JsonIgnore]
        private bool? _mathematicCRS { get; set; } = false;

        /// <summary>
        /// default (false) -> right handed crs (XYZ) <para/>
        /// (true) -> left handed crs (YXZ)
        /// </summary>
        public bool? mathematicCRS 
        { 
            get { return _mathematicCRS; }
            set
            {
                _mathematicCRS = value;
                NotifyPropertyChanged(nameof(mathematicCRS));
            }
        }

        //below the required attributes to process the georeferencing
        #region GeoRef - Attributes


        /// <summary>
        /// set to default value
        /// </summary>
        [JsonIgnore]
        private IFC.LoGeoRef _logeoref { get; set; } = IFC.LoGeoRef.LoGeoRef30;

        /// <summary>
        /// Decides which level of georef to use
        /// </summary>
        public IFC.LoGeoRef logeoref
        {
            get { return _logeoref; }
            set
            {
                _logeoref = value;
                NotifyPropertyChanged(nameof(logeoref));
            }
        }

        [JsonIgnore]
        private bool? _customOrigin { get; set; } = false;

        /// <summary>
        /// Describes whether the project coordinate origin should be set to the user defined position or not.
        /// </summary>
        public bool? customOrigin
        {
            get { return _customOrigin; }
            set
            {
                _customOrigin = value;
                NotifyPropertyChanged(nameof(customOrigin));
            }
        }
        [JsonIgnore]
        private double? _xOrigin { get; set; } = 0;

        /// <summary>
        /// x - value of a user defined georeferencing
        /// </summary>
        public double? xOrigin 
        {
            get { return _xOrigin; }
            set
            {
                _xOrigin = value;
                NotifyPropertyChanged(nameof(xOrigin));
            }
        }


        [JsonIgnore]
        private double? _yOrigin { get; set; } = 0;

        /// <summary>
        /// y - value of a user defined georeferencing
        /// </summary>
        public double? yOrigin
        {
            get { return _yOrigin; }
            set
            {
                _yOrigin = value;
                NotifyPropertyChanged(nameof(yOrigin));
            }
        }

        [JsonIgnore]
        private double? _zOrigin { get; set; } = 0;

        /// <summary>
        /// z - value of a user defined georeferencing
        /// </summary>
        public double? zOrigin
        {
            get { return _zOrigin; }
            set
            {
                _zOrigin = value;
                NotifyPropertyChanged(nameof(zOrigin));
            }
        }

        /// <summary>
        /// set default value to 0 (no rotation)
        /// </summary>
        [JsonIgnore]
        private double? _trueNorth { get; set; } = 0;

        /// <summary>
        /// rotation against true north
        /// </summary>
        public double? trueNorth
        {
            get { return _trueNorth; }
            set
            {
                _trueNorth = value;
                NotifyPropertyChanged(nameof(trueNorth));
            }
        }

        #region LoGeoRef50
        /// <summary>
        /// set default value to 1 (no scaling)
        /// </summary>
        [JsonIgnore]
        private double? _scale { get; set; } = 1;

        /// <summary>
        /// [LoGeoRef50] scaling 
        /// </summary>
        public double? scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                NotifyPropertyChanged(nameof(scale));
            }
        }

        [JsonIgnore]
        private int? _crsName { get; set; }

        /// <summary>
        /// [LoGeoRef50] Name of the CRS (EPSG-Code)
        /// </summary>
        public int? crsName 
        { 
            get { return _crsName; }
            set
            {
                _crsName = value;
                NotifyPropertyChanged(nameof(crsName));
            }
        }

        [JsonIgnore]
        private string _crsDescription { get; set; }

        /// <summary>
        /// [LoGeoRef50] Description to the CRS
        /// </summary>
        public string crsDescription 
        {
            get { return _crsDescription; }
            set
            {
                _crsDescription = value;
                NotifyPropertyChanged(nameof(crsDescription));
            }
        }

        [JsonIgnore]
        private string _geodeticDatum { get; set; }

        /// <summary>
        /// [LoGeoRef50] name of the geodetic datum
        /// </summary>
        public string geodeticDatum
        {
            get { return _geodeticDatum; }
            set
            {
                _geodeticDatum = value;
                NotifyPropertyChanged(nameof(geodeticDatum));
            }
        }

        [JsonIgnore]
        private string _verticalDatum { get; set; }

        /// <summary>
        /// [LoGeoRef50] name of the geodetic datum
        /// </summary>
        public string verticalDatum
        {
            get { return _verticalDatum; }
            set
            {
                _verticalDatum = value;
                NotifyPropertyChanged(nameof(verticalDatum));
            }
        }
        
        [JsonIgnore]
        private string _projectionName { get; set; }

        /// <summary>
        /// [LoGeoRef50] name of the projection name
        /// </summary>
        public string projectionName
        {
            get { return _projectionName; }
            set
            {
            _projectionName = value;
                NotifyPropertyChanged(nameof(projectionName));
            }
        }

        [JsonIgnore]
        private string _projectionZone { get; set; }

        /// <summary>
        /// [LoGeoRef50] name of the projection zone
        /// </summary>
        public string projectionZone
        {
            get { return _projectionZone; }
            set
            {
                _projectionZone = value;
                NotifyPropertyChanged(nameof(projectionZone));
            }
        }
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
        private bool? _readPoints { get; set; } = false;
        public bool? readPoints
        {
            get { return _readPoints; }
            set
            {
                _readPoints = value;
                NotifyPropertyChanged(nameof(readPoints));
            }
        }
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

        [JsonIgnore]
        private string _breakline_layer { get; set; }

        /// <summary>
        /// Name of the layer that contains the breakline. (only one layer is allowed)
        /// </summary>
        public string breakline_layer
        {
            get { return _breakline_layer; }
            set
            {
                _breakline_layer = value;
                NotifyPropertyChanged(nameof(breakline_layer));
            }
        }
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
        public string queryString { get; set; }

        /// <summary>
        /// string to query breaklines via user query
        /// </summary>
        public string breaklineQueryString { get; set; }

        #endregion

        [JsonIgnore]
        private GeoJSON.GeometryType? _geometryType { get; set; } = GeoJSON.GeometryType.MultiPoint;

        //for geojson
        #region GeoJSON
        /// <summary>
        /// geometry type to set the right reader
        /// </summary>
        public GeoJSON.GeometryType? geometryType
        {
            get { return _geometryType; }
            set
            {
                _geometryType = value;
                NotifyPropertyChanged(nameof(geometryType));
            }
        }

        /// <summary>
        /// file path to breakline file (JSON)
        /// </summary>
        public string breaklineFile { get; set; }

        /// <summary>
        /// breakline geometry type to specific reading
        /// </summary>
        public GeoJSON.GeometryType? breaklineGeometryType { get; set; }


        #endregion GeoJSON
        #endregion
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