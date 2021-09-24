using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BIMGISInteropLibs.Logging; //include for verbosity level
using System.ComponentModel;//interface property changed

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
        /// <summary>
        /// event needed for interface (DO NOT RENAME)
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// function to check if property has 'really' been changed 
        /// </summary>
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion data binding

        #region file handling
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

        private string _logFilePath { get; set; }


        /// <summary>
        /// Destination location for the log file
        /// </summary>
        public string logFilePath 
        { 
            get { return _logFilePath; }
            set
            {
                _logFilePath = value;
                NotifyPropertyChanged(nameof(logFilePath));
            }
        }

        /// <summary>
        /// init value is set to info
        /// </summary>
        
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
        private bool? _geoElement { get; set; } = false;
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


        private bool? _exportMetadataFile { get; set; } = false;

        [Newtonsoft.Json.JsonIgnore]
        /// <summary>
        /// Decide whether metadata should be exported as a separate JSON file
        /// </summary>
        public bool? exportMetadataFile
        {
            get { return _exportMetadataFile; }
            set
            {
                _exportMetadataFile = value;
                NotifyPropertyChanged(nameof(exportMetadataFile));
            }
        }

        private bool? _outIfcPropertySet { get; set; } = false;

        /// <summary>
        /// Decide whehter metadata should be stored as IfcPropertySets
        /// </summary>
        public bool? outIfcPropertySet
        {
            get { return _outIfcPropertySet; }
            set
            {
                _outIfcPropertySet = value;
                NotifyPropertyChanged(nameof(outIfcPropertySet));
            }
        }

        private bool? _exportMetadataDin91391 { get; set; } = false;

        /// <summary>
        /// Decide whether metadata (according to DIN SPEC 91391-2) should be exported
        /// </summary>
        public bool? exportMetadataDin91391
        {
            get { return _exportMetadataDin91391; }
            set
            {
                _exportMetadataDin91391 = value;
                NotifyPropertyChanged(nameof(exportMetadataDin91391));
            }
        }

        private bool? _exportMetadataDin18740 { get; set; } = false;

        /// <summary>
        /// Decide whether metadata (according to DIN 18740-6) should be exported
        /// </summary>
        public bool? exportMetadataDin18740
        {
            get { return _exportMetadataDin18740; }
            set
            {
                _exportMetadataDin18740 = value;
                NotifyPropertyChanged(nameof(exportMetadataDin18740));
            }
        }

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

        private bool? _invertedCRS { get; set; } = false;

        /// <summary>
        /// [FALSE]: right handed; [TRUE]: left handed
        /// </summary>
        public bool? invertedCRS
        {
            get
            {
                return _invertedCRS;
            }
            set
            {
                _invertedCRS = value;
                NotifyPropertyChanged(nameof(invertedCRS));
            }
        }

        //below the required attributes to process the georeferencing
        #region GeoRef - Attributes


        /// <summary>
        /// set to default value
        /// </summary>

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
        
        private string _layer { get; set; }

        /// <summary>
        /// Name of the layer that contains terrain information in an input DXF file
        /// </summary>
        public string layer 
        { 
            get { return _layer; }
            set
            {
                _layer = value;
                NotifyPropertyChanged(nameof(layer));
            }
        }


        /// <summary>
        /// init with false value (so faces are set by default)
        /// </summary>
        private bool? _readPoints { get; set; } = false;

        /// <summary>
        /// read only point data from dxf file
        /// </summary>
        public bool? readPoints
        {
            get { return _readPoints; }
            set
            {
                _readPoints = value;
                NotifyPropertyChanged(nameof(readPoints));
            }
        }
        
        
        private bool? _rvtReadPoints { get; set; } = false;

        [Newtonsoft.Json.JsonIgnore]
        /// <summary>
        /// read only point data from dxf file
        /// </summary>
        public bool? rvtReadPoints
        {
            get { return _rvtReadPoints; }
            set
            {
                _rvtReadPoints = value;
                NotifyPropertyChanged(nameof(rvtReadPoints));
            }
        }
        #endregion

        //for reb processing
        #region REB
        private int? _horizon { get; set; }


        /// <summary>
        /// Number of the horizon that contains terrain information
        /// </summary>
        public int? horizon 
        {
            get { return _horizon; }
            set
            {
                _horizon = value;
                NotifyPropertyChanged(nameof(horizon));
            }
        }


        #endregion

        //for elevation grid processing
        #region GRID
        private bool? _bBox { get; set; } = false;
        /// <summary>
        /// Decision whether BoundingBox should be processed (yes = true)
        /// </summary>
        public bool? bBox 
        {
            get { return _bBox; }
            set
            {
                _bBox = value;
                NotifyPropertyChanged(nameof(bBox));
            }
        }

        private double? _bbP1X { get; set; }

        /// <summary>
        ///  P1 x-value of the bounding box
        /// </summary>
        public double? bbP1X 
        {
            get { return _bbP1X; }
            set
            {
                _bbP1X = value;
                NotifyPropertyChanged(nameof(bbP1X));
            }
        }

        private double? _bbP1Y { get; set; }

        /// <summary>
        /// P1 y-value of the bounding box
        /// </summary>
        public double? bbP1Y
        {
            get { return _bbP1Y; }
            set
            {
                _bbP1Y = value;
                NotifyPropertyChanged(nameof(bbP1Y));
            }
        }

        private double? _bbP2X { get; set; }

        /// <summary>
        ///  P2 x-value of the bounding box
        /// </summary>
        public double? bbP2X
        {
            get { return _bbP2X; }
            set
            {
                _bbP2X = value;
                NotifyPropertyChanged(nameof(bbP2X));
            }
        }

        private double? _bbP2Y { get; set; }

        /// <summary>
        /// P2 y-value of the bounding box
        /// </summary>
        public double? bbP2Y
        {
            get { return _bbP2Y; }
            set
            {
                _bbP2Y = value;
                NotifyPropertyChanged(nameof(bbP2Y));
            }
        }
        #endregion

        
        #region GEOgraf OUT
        private bool? _onlyHorizon { get; set; } = false;

        /// <summary>
        /// Decides whether all horizons (=false) or only selected ones (=true) are to be used. If filtering is to be used, the entry must be made via "horizonFilter".
        /// </summary>
        public bool? onlyHorizon 
        { 
            get { return _onlyHorizon; }
            set
            {
                _onlyHorizon = value;
                NotifyPropertyChanged(nameof(onlyHorizon));
            }
        }

        private bool? _filterPoints { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        public bool? filterPoints 
        {
            get { return _filterPoints; }
            set
            {
                _filterPoints = value;
                NotifyPropertyChanged(nameof(filterPoints));
            }
        }


        private string _horizonFilter { get; set; }

        /// <summary>
        /// filtering point types
        /// </summary>
        public string horizonFilter 
        {
            get { return _horizonFilter; }
            set
            {
                _horizonFilter = value;
                NotifyPropertyChanged(nameof(horizonFilter));
            }
        }
        #endregion

        //for postgis processing
        #region PostGIS
        private string _host { get; set; }

        /// <summary>
        /// Link to the host database
        /// </summary>
        public string host 
        {
            get { return _host; }
            set
            {
                _host = value;
                NotifyPropertyChanged(nameof(host));
            }
        }

        private int? _port { get; set; }
        
        /// <summary>
        /// Specifying the port for the database connection
        /// </summary>
        public int? port 
        { 
            get { return _port; }
            set
            {
                _port = value;
                NotifyPropertyChanged(nameof(_port));
            }
        }

        private string _user { get; set; }

        /// <summary>
        /// username 
        /// </summary>
        public string user
        {
            get { return _user; }
            set
            {
                _user = value;
                NotifyPropertyChanged(nameof(user));
            }
        }

        private string _pwd { get; set; }

        /// <summary>
        /// Specification of the password for authentication with the database
        /// </summary>
        public string pwd
        {
            get { return _pwd; }
            set
            {
                _pwd = value;
                NotifyPropertyChanged(nameof(pwd));
            }
        }

        private string _database { get; set; }

        /// <summary>
        /// target database
        /// </summary>
        public string database
        {
            get { return _database; }
            set
            {
                _database = value;
                NotifyPropertyChanged(nameof(database));
            }
        }

        private string _schema { get; set; }

        /// <summary>
        /// target database
        /// </summary>
        public string schema
        {
            get { return _schema; }
            set
            {
                _schema = value;
                NotifyPropertyChanged(nameof(schema));
            }
        }

        private string _tin_table { get; set; }

        /// <summary>
        /// Specify the table that contains the TIN
        /// </summary>
        public string tin_table
        {
            get { return _tin_table; }
            set
            {
                _tin_table = value;
                NotifyPropertyChanged(nameof(tin_table));
            }
        }

        private string _tin_column { get; set; }

        /// <summary>
        ///Specify the column that contains the geometry of the TIN
        /// </summary>
        public string tin_column
        {
            get { return _tin_column; }
            set
            {
                _tin_column = value;
                NotifyPropertyChanged(nameof(tin_column));
            }
        }

        private string _tinid_column { get; set; }

    /// <summary>
    /// Specify the column that contains the ID of the TIN
    /// </summary>
    public string  tinid_column
        {
            get { return _tinid_column; }
            set
            {
                _tinid_column = value;
                NotifyPropertyChanged(nameof(tinid_column));
            }
        }
        private string _tin_id { get; set; }

        /// <summary>
        ///  Specification of a TIN ID to be read out
        /// </summary>
        public string tin_id
        {
            get { return _tin_id; }
            set
            {
                _tin_id = value;
                NotifyPropertyChanged(nameof(tin_id));
            }
        }

        private string _breakline_table { get; set; }

        /// <summary>
        /// Specify the table that contains the geometry of the break lines
        /// </summary>
        public string breakline_table
        {
            get { return _breakline_table; }
            set
            {
                _breakline_table = value;
                NotifyPropertyChanged(nameof(breakline_table));
            }
        }

        private string _breakline_column { get; set; }

        /// <summary>
        /// Specify the column that contains the geometry of the break lines
        /// </summary>
        public string breakline_column
        {
            get { return _breakline_column; }
            set
            {
                _breakline_column = value;
                NotifyPropertyChanged(nameof(breakline_column));
            }
        }

        private string _breakline_tin_id { get; set; }

        /// <summary>
        /// Specify the column that contains the TIN ID 
        /// </summary>
        public string breakline_tin_id
        {
            get { return _breakline_tin_id; }
            set
            {
                _breakline_tin_id = value;
                NotifyPropertyChanged(nameof(breakline_tin_id));
            }
        }
        private string _queryString { get; set; }

        /// <summary>
        /// String to query TIN data via user query
        /// </summary>
        public string queryString
        {
            get { return _queryString; }
            set
            {
                _queryString = value;
                NotifyPropertyChanged(nameof(queryString));
            }
        }

        private string _breaklineQueryString { get; set; }

        /// <summary>
        /// string to query breaklines via user query
        /// </summary>
        public string breaklineQueryString
        {
            get { return _breaklineQueryString; }
            set
            {
                _breaklineQueryString = value;
                NotifyPropertyChanged(nameof(breaklineQueryString));
            }
        }
        #endregion

        //for geojson
        #region GeoJSON
        private GeoJSON.GeometryType? _geometryType { get; set; } = GeoJSON.GeometryType.MultiPoint;
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
        #endregion GeoJSON

        #endregion
    }



    
}