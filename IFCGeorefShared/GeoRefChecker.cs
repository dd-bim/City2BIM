using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using static System.FormattableString;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Serilog;

using IFCGeorefShared.Levels;
using Xbim.Ifc4.ProductExtension;
using OSGeo.OGR;
using System.ComponentModel;
using Xbim.Ifc.Extensions;
using System.Resources;
using Xbim.Ifc4x3.RepresentationResource;
using static IFCGeorefShared.Utils;


namespace IFCGeorefShared
{
    public class GeoRefChecker : IDisposable
    {
        private static TranslationService? _translationService;
        public static TranslationService TranslationService
        {
            get
            {
                if (_translationService == null)
                {
                    throw new InvalidOperationException("TranslationService has not been initialized.");
                }
                return _translationService;
            }
        }
        public string? TimeCheckedFileCreated { get; set; }
        public string? TimeChecked { get; set; }

        private readonly Xbim.Common.Step21.XbimSchemaVersion ifcVersion;
        public Xbim.Common.Step21.XbimSchemaVersion IFCVersion
        {
            get => ifcVersion;
        }

        private string? protocollPath { get; set; }
        public string? ProtocollPath
        {
            get => protocollPath;
            set
            {
                if (protocollPath != value)
                {
                    protocollPath = value;
                }
            }
        }

        private IList<Level10> loGeoRef10 = new List<Level10>();
        public IList<Level10> LoGeoRef10
        {
            get => loGeoRef10;
            set
            {
                if (loGeoRef10 != value)
                {
                    loGeoRef10 = value;
                }
            }
        }
        private IList<Level20> loGeoRef20 = new List<Level20>();
        public IList<Level20> LoGeoRef20
        {
            get => loGeoRef20;
            set
            {
                if (loGeoRef20 != value)
                {
                    loGeoRef20 = value;
                }
            }
        }
        private IList<Level30> loGeoRef30 = new List<Level30>();
        public IList<Level30> LoGeoRef30
        {
            get => loGeoRef30;
            set
            {
                if (loGeoRef30 != value)
                {
                    loGeoRef30 = value;
                }
            }
        }
        private IList<Level40> loGeoRef40 = new List<Level40>();
        public IList<Level40> LoGeoRef40
        {
            get => loGeoRef40;
            set
            {
                if (loGeoRef40 != value)
                {
                    loGeoRef40 = value;
                }
            }
        }

        private IList<Level50> loGeoRef50 = new List<Level50>();
        public IList<Level50> LoGeoRef50
        {
            get => loGeoRef50;
            set
            {
                if (loGeoRef50 != value)
                {
                    loGeoRef50 = value;
                }
            }
        }
        public IList<T> LoGeoRef<T>() where T : Level00
        {
            var t = typeof(T); 
            if (t == typeof(Level10)) return (IList<T>)LoGeoRef10;
            if (t == typeof(Level20)) return (IList<T>)LoGeoRef20;
            if (t == typeof(Level30)) return (IList<T>)LoGeoRef30;
            if (t == typeof(Level40)) return (IList<T>)LoGeoRef40;
            if (t == typeof(Level50)) return (IList<T>)LoGeoRef50;
            return new List<T>();
        }
        public string FilePath { get; }
        private readonly bool ownsModel;
        private bool disposed = false;
        public GeneralProperties? GenProps { get; set; }
        private IfcStore model { get; set; }
        internal IfcStore Model => this.model;
        private List<IIfcSpatialStructureElement> BuildingsAndSites = new List<IIfcSpatialStructureElement>(); 

        public GeoRefChecker(IfcStore model, ITranslator translator) {
            this.model = model;
            this.ifcVersion = this.model.SchemaVersion;
            BuildingsAndSites = new IIfcSpatialStructureElement[0]
                .Concat(model.Instances.OfType<IIfcSite>())
                .Concat(model.Instances.OfType<IIfcBuilding>()).ToList();

            Level10.CheckForLevel(this);
            Level20.CheckForLevel(this);
            Level20.CheckGeoLocation(this.LoGeoRef20);
            Level30.CheckForLevel(this);
            Level40.CheckForLevel(this);
            Level50.CheckForLevel(this);
            checkGeneralProps();

            this.TimeChecked = DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss");
            this.FilePath = this.model.FileName;

            if (translator == null)
            {
                throw new ArgumentNullException(nameof(translator));
            }
            _translationService = new TranslationService(translator);
        }
        /// <summary>
        /// Create a new instance of <see cref="GeoRefChecker"/> with the specified <paramref name="model"/> and <paramref name="translator"/>, and specify whether the checker should take ownership of the model. If <paramref name="ownsModel"/> is set to <see langword="true"/>, the checker will dispose of the model when it is disposed. If set to <see langword="false"/>, the caller retains ownership and is responsible for disposing of the model. By default, ownership is not taken (<see langword="false"/>).
        /// </summary>
        /// <param name="model"></param>
        /// <param name="translator"></param>
        /// <param name="ownsModel">Option to keep model ownership for further processing (do not use inside a "using" of the model)</param>
        public GeoRefChecker(IfcStore model, ITranslator translator, bool ownsModel = false) : this(model, translator)
        {
            // Keep model ownership
            this.ownsModel = ownsModel;
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (ownsModel)
            {
                try
                {
                    model?.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error disposing IfcStore in GeoRefChecker.");
                }
            }
        }

        private void checkGeneralProps()
        {

            var allCartPoints = model.Instances.OfType<IIfcCartesianPoint>().ToList();
            var allCartPointList2D = model.Instances.OfType<IIfcCartesianPointList2D>().ToList();
            var allCartPointList3D = model.Instances.OfType<IIfcCartesianPointList3D>().ToList();
            
            double maxX = 0, maxY = 0, maxZ = 0;

            var allPlacementLocations = new List<IIfcCartesianPoint>();
            allPlacementLocations.AddRange(this.LoGeoRef30
                .Where(x => x.IsFullFilled && x.plcmt != null)
                .Select(x => x.plcmt.Location!));
            allPlacementLocations.AddRange(this.LoGeoRef40
                .Where(x => x.IsFullFilled && x.wcs != null)
                .Select(x => x.wcs.Location!));

            foreach (var pnt in allCartPoints)
            {
                if (allPlacementLocations.Contains(pnt)) continue;
                if (Math.Abs(pnt.X) > maxX) maxX = Math.Abs(pnt.X);
                if (Math.Abs(pnt.Y) > maxY) maxY = Math.Abs(pnt.Y);
                if (Math.Abs(pnt.Z) > maxZ) maxZ = Math.Abs(pnt.Z);
            }

            foreach (var pntList in  allCartPointList3D)
            {
                foreach(var coords in pntList.CoordList)
                {
                    if (Math.Abs(coords[0]) > maxX) maxX = Math.Abs(coords[0]);
                    if (Math.Abs(coords[1]) > maxY) maxY = Math.Abs(coords[1]);
                    if (Math.Abs(coords[2]) > maxZ) maxZ = Math.Abs(coords[2]);
                }
            }

            foreach (var pntList in allCartPointList2D)
            {
                foreach (var coords in pntList.CoordList)
                {
                    if (Math.Abs(coords[0]) > maxX) maxX = Math.Abs(coords[0]);
                    if (Math.Abs(coords[1]) > maxY) maxY = Math.Abs(coords[1]);
                }
            }

            if (maxX < 1000 && maxY < 1000 && maxZ < 1000) { Log.Information($"Maximum absolute coordinates are: {maxX} | {maxY} | {maxZ}"); }
            else { Log.Warning($"Found big coordinate values! X: {maxX} Y: {maxY} Z {maxZ}"); }

            var genProps = new GeneralProperties();
            genProps.X = maxX;
            genProps.Y = maxY;
            genProps.Z = maxZ;
            this.GenProps = genProps;
            this.checkElevationConsistency();

        }

        private void checkElevationConsistency()
        {
            var siteElevDict = new Dictionary<string, double>();
            var sitePlcmtZ = new Dictionary<string, double>();
            var contextPlcmtElev = new Dictionary<string, double>();
            double? mapConvHeight = null;
            foreach (var site in this.model.Instances.OfType<IIfcSite>().ToList())
            {
                var elevation = site.RefElevation;
                if (elevation.HasValue)
                {
                    siteElevDict.Add($"#{site.EntityLabel.ToString()}", elevation.Value);
                }

                var plcmt = site.ObjectPlacement;
                if (plcmt.Z().HasValue)
                {
                    sitePlcmtZ.Add($"#{site.EntityLabel.ToString()}", plcmt.Z()!.Value);
                }
            }

            foreach (var context in this.model.Instances.OfType<IIfcGeometricRepresentationContext>().
                Where(ctx => ctx.ExpressType.ToString() != "IfcGeometricRepresentationSubContext").ToList())
            {
                var plcmt = context.WorldCoordinateSystem;
                if (plcmt is IIfcAxis2Placement3D)
                {
                    var plcmt3D = (IIfcAxis2Placement3D)plcmt;
                    //var contextType = context.ContextType != null ? (string)context.ContextType : "noTypeSpecified";
                    //if (contextPlcmtElev.ContainsKey(contextType)) { }
                    if (plcmt3D.Location == null) { contextPlcmtElev.Add($"#{context.EntityLabel}", 0); }
                    else { contextPlcmtElev.Add($"#{context.EntityLabel}", plcmt3D.Location.Z); }
                    
                }
            }

            if (this.model.SchemaVersion == Xbim.Common.Step21.XbimSchemaVersion.Ifc4 || this.model.SchemaVersion == Xbim.Common.Step21.XbimSchemaVersion.Ifc4x1)
            {
                foreach( var mapConv in this.model.Instances.OfType<IIfcMapConversion>().ToList())
                {
                    mapConvHeight = mapConv.OrthogonalHeight;
                }
            }

            if (this.GenProps == null) this.GenProps = new GeneralProperties();

            this.GenProps.SiteElevDict = siteElevDict;
            this.GenProps.SitePlcmtZDict = sitePlcmtZ;
            this.GenProps.ContextPlcmtElev = contextPlcmtElev;
            this.GenProps.mapConvHeight = mapConvHeight; 
        }

            
        public GeoRefCheckerResult getCheckResults()
        {
            var results = new GeoRefCheckerResult();

            results.level10Fulfilled = this.LoGeoRef10.Any(x => x.IsFullFilled);
            results.level20Fulfilled = this.LoGeoRef20.Any(x => x.IsFullFilled);
            results.level30Fulfilled = this.LoGeoRef30.Any(x => x.IsFullFilled);
            results.level40Fulfilled = this.LoGeoRef40.Any(x => x.IsFullFilled);
            results.level50Fulfilled = this.LoGeoRef50.Any(x => x.IsFullFilled);

            return results;
        }
        public bool getCheckResult<T>() where T : Level00
        {
            var t = typeof(T);

            if (t == typeof(Level10)) return this.LoGeoRef10.Any(x => x.IsFullFilled);
            if (t == typeof(Level20)) return this.LoGeoRef20.Any(x => x.IsFullFilled);
            if (t == typeof(Level30)) return this.LoGeoRef30.Any(x => x.IsFullFilled);
            if (t == typeof(Level40)) return this.LoGeoRef40.Any(x => x.IsFullFilled);
            if (t == typeof(Level50)) return this.LoGeoRef50.Any(x => x.IsFullFilled);

            throw new NotSupportedException($"getCheckResult<{t.Name}> is not supported.");
        }

        public void WriteProtocoll(string? WorkingDirPath)
        {
            var culture = new CultureInfo("en-US");
            var sb = new StringBuilder();

            sb.AppendLine($"{_translationService.Translate("ProtocolHeader", culture)}");
            sb.AppendLine($"{this.model.FileName}");
            sb.AppendLine($"{_translationService.Translate("CheckedOn", culture)}: {this.TimeChecked}");
            sb.AppendLine($"{_translationService.Translate("IfcVersion", culture)}: {this.model.SchemaVersion}");
            sb.AppendLine();
            var result = this.getCheckResults();
            sb.AppendLine((result.level10Fulfilled == true) ? $"LoGeoRef10: ✔" : $"LoGeoRef10: ✖");
            sb.AppendLine((result.level20Fulfilled == true) ? $"LoGeoRef20: ✔" : $"LoGeoRef20: ✖");
            sb.AppendLine((result.level30Fulfilled == true) ? $"LoGeoRef30: ✔" : $"LoGeoRef30: ✖");
            sb.AppendLine((result.level40Fulfilled == true) ? $"LoGeoRef40: ✔" : $"LoGeoRef40: ✖");
            sb.AppendLine((result.level50Fulfilled == true) ? $"LoGeoRef50: ✔" : $"LoGeoRef50: ✖");
            sb.AppendLine();
            sb.AppendLine($"{_translationService.Translate("MaxExtend", culture)}: X: {this.GenProps!.X} Y: {this.GenProps.Y} Z: {this.GenProps.Z}");
            sb.AppendLine();
            sb.AppendLine($"{_translationService.Translate("RefElevationAndZ", culture)}: ");
            const string placeholder = "            ";
            string section;
            // Print Elevations for LoGeoRef30
            if (result.level30Fulfilled == true)
            {
                section = "LoGeoRef30: ";
                foreach (var item in this.LoGeoRef30.Where(x => x.IsFullFilled))
                {
                    string RefElevation = (item.ReferencedEntity is IIfcSite site) ? $"#{site.EntityLabel.ToString()} RefElevation: {(site.RefElevation.HasValue ? site.RefElevation.Value.ToString() : _translationService.Translate("NotSpecified", CultureInfo.CurrentCulture))} " : "";
                    string LocalPlacement = item.plcmt != null ? $"#{item.plcmt.EntityLabel.ToString()} LocalPlacement: {item.plcmt.Location.Z.ToString()}" : $"LocalPlacement: {_translationService.Translate("NotSpecified", CultureInfo.CurrentCulture)}";
                    sb.AppendLine($"{section}{RefElevation}{LocalPlacement}");
                    section = placeholder;
                }
            }
            // Print Elevations for LoGeoRef40
            if (result.level40Fulfilled == true)
            {
                section = "LoGeoRef40: ";
                foreach (var item in this.LoGeoRef40.Where(x => x.IsFullFilled))
                {
                    if (item.wcs != null && item.wcs is IIfcAxis2Placement3D wcs3D)
                    {
                        sb.AppendLine($"{section}#{item.wcs.EntityLabel.ToString()} GeometricRepresentationContext: {wcs3D.P[2]}");
                        section = placeholder;
                    }
                }
            }
            // Print Elevations for LoGeoRef50
            if (result.level50Fulfilled == true)
            {
                section = "LoGeoRef50: ";
                foreach (var item in this.LoGeoRef50.Where(x => x.IsFullFilled))
                {
                    string OrthogonalHeight = item.MapConversion != null ? $"#{item.MapConversion.EntityLabel.ToString()} MapConversion: {item.MapConversion.OrthogonalHeight.ToString()}" : $"MapConversion: {_translationService.Translate("NotSpecified", CultureInfo.CurrentCulture)}";
                    string Height = item.RigidOperation != null ? $"#{item.RigidOperation.EntityLabel.ToString()} RigidOperation: {item.RigidOperation.Height.ToString()}" : $"RigidOperation: {_translationService.Translate("NotSpecified", CultureInfo.CurrentCulture)}";
                    sb.AppendLine($"{section}{OrthogonalHeight} {Height}");
                    section = placeholder;
                }
            }

            sb.AppendLine();
            sb.AppendLine(starLine);
            sb.AppendLine();
            
            sb.AppendLine(Level10.WriteLevelResult(this,culture));
            sb.AppendLine(Level20.WriteLevelResult(this,culture));
            sb.AppendLine(Level30.WriteLevelResult(this,culture));
            sb.AppendLine(Level40.WriteLevelResult(this,culture));
            sb.AppendLine(Level50.WriteLevelResult(this,culture));

            var protocoll = sb.ToString();

            string protocollOutPath;
            if (string.IsNullOrEmpty(WorkingDirPath))
            {
                protocollOutPath = ProtocollPath ?? Path.Combine(Path.GetDirectoryName(this.model.FileName) ?? "", Path.GetFileNameWithoutExtension(this.model.FileName) + "__CheckResult.txt");
            }
            else
            {
                protocollOutPath = Path.Combine(WorkingDirPath, Path.GetFileNameWithoutExtension(this.model.FileName) + "__CheckResult.txt");
            }

            using (var file = File.CreateText(protocollOutPath))
            {
                file.WriteLine(protocoll);
            }

            this.ProtocollPath = protocollOutPath;

        }

        public void SaveModelAs(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (model == null) throw new InvalidOperationException("IfcStore is not available.");
            try
            {
                model.SaveAs(path);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save model to {Path}", path);
                throw;
            }
        }
    }

    public class GeneralProperties
    {
        public double? X { get; set; }
        public double? Y { get; set; }
        public double? Z { get; set; }

        public IDictionary<string, double>? SiteElevDict { get; set; }
        public IDictionary<string, double>? SitePlcmtZDict { get; set; }
        public IDictionary<string, double>? ContextPlcmtElev { get; set; }
        public double? mapConvHeight {get; set;} 

    }

    public class GeoRefCheckerResult
    {
        public bool? level10Fulfilled { get; set; }
        public bool? level20Fulfilled { get; set; }
        public bool? level30Fulfilled { get; set; }

        public bool? level40Fulfilled { get; set; }
        public bool? level50Fulfilled { get; set; }


    }
}
