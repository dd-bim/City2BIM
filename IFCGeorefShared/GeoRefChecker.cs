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


namespace IFCGeorefShared
{
    public class GeoRefChecker
    {
        private readonly TranslationService _translationService;

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
        IList<Level30> LoGeoRef30 { get; set; } = new List<Level30>();
        IList<Level40> LoGeoRef40 { get; set; } = new List<Level40>();

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

        public string FilePath { get; }
        public GeneralProperties? GenProps { get; set; }
        private IfcStore model { get; set; }
        private List<IIfcSpatialStructureElement> BuildingsAndSites = new List<IIfcSpatialStructureElement>(); 

        public GeoRefChecker(IfcStore model, ITranslator translator) {
            this.model = model;
            BuildingsAndSites = new IIfcSpatialStructureElement[0]
                .Concat(model.Instances.OfType<IIfcSite>())
                .Concat(model.Instances.OfType<IIfcBuilding>()).ToList();

            checkForLevel10();
            checkForLevel20();
            checkGeoLocation(this.LoGeoRef20);
            checkForLevel30();
            checkForLevel40And50();
            checkGeneralProps();

            this.TimeChecked = DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss");
            this.ifcVersion = this.model.SchemaVersion;
            this.FilePath = this.model.FileName;

            if (translator == null)
            {
                throw new ArgumentNullException(nameof(translator));
            }
                _translationService = new TranslationService(translator);
        }

        private void checkGeneralProps()
        {
            //Es werden immer Koordinaten als Warnung ausgegeben, die zur Georef genutzt werden (ab Level 30) -> Irritation beim Anwender (evtl. fachfremd)
            //Vergleich zur jeweiligen Checkmethode notwendig -> keine Warnung, wenn Koordinaten in Checkmethode aufgerufen werden?

            var allCartPoints = model.Instances.OfType<IIfcCartesianPoint>().ToList();
            var allCartPointList2D = model.Instances.OfType<IIfcCartesianPointList2D>().ToList();
            var allCartPointList3D = model.Instances.OfType<IIfcCartesianPointList3D>().ToList();

            double maxX = 0, maxY = 0, maxZ = 0;

            foreach (var pnt in allCartPoints)
            {
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

        private void checkGeoLocation(IList<Level20> lvl20s) 
        {
            var hasResults = lvl20s.Any(x => x.IsFullFilled);
            if (!hasResults) return;

            using (DataSource ds = Ogr.Open(Settings.GetSettings().RegionPath, 0))
            {
                if (ds == null)
                {
                    Log.Error("Reading of region file failed! \nCanceling operation");
                    return;
                }

                var countries = ds.GetLayerByName("ne_10m_admin_1_states_provinces");
                Feature? lastFoundFeature = null;
                var feature = countries.GetNextFeature();
                var nameIdx = feature.GetFieldIndex("name");
                var adminIdx = feature.GetFieldIndex("admin");

                foreach (var lvl20 in lvl20s)
                {
                    if (!lvl20.Latitude.HasValue ||  !lvl20.Longitude.HasValue)
                    {
                        Log.Information($"IfcSite {lvl20.ReferencedEntity!.GlobalId} has either no latitude or longitude");
                        continue;
                    }

                    countries.ResetReading();

                    var pointToTest = new OSGeo.OGR.Geometry(wkbGeometryType.wkbPoint);
                    pointToTest.AddPoint_2D((double)lvl20.Longitude, (double)lvl20.Latitude);

                    if (lastFoundFeature != null)
                    {
                        if (pointToTest.Within(lastFoundFeature.GetGeometryRef()))
                        {
                            lvl20.GeographicDescription = $"{_translationService.Translate("SiteLocated", CultureInfo.CurrentCulture)} {lastFoundFeature.GetFieldAsString(adminIdx)} in {_translationService.Translate("Region", CultureInfo.CurrentCulture)} {lastFoundFeature.GetFieldAsString(nameIdx)}";
                            continue;
                        }
                    }

                    while(feature != null)
                    {
                        if (pointToTest.Within(feature.GetGeometryRef()))
                        {
                            lastFoundFeature = feature;
                            lvl20.GeographicDescription = $"{_translationService.Translate("SiteLocated", CultureInfo.CurrentCulture)} {feature.GetFieldAsString(adminIdx)} in {_translationService.Translate("Region", CultureInfo.CurrentCulture)} {feature.GetFieldAsString(nameIdx)}";
                        }
                        feature = countries.GetNextFeature();
                    }
                }
            }
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
                    siteElevDict.Add(site.GlobalId, elevation.Value);
                }

                var plcmt = site.ObjectPlacement;
                if (plcmt.Z().HasValue)
                {
                    sitePlcmtZ.Add(site.GlobalId, plcmt.Z()!.Value);
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

            if (this.GenProps != null)
            {
                this.GenProps.SiteElevDict = siteElevDict;
                this.GenProps.SitePlcmtZDict = sitePlcmtZ;
                this.GenProps.ContextPlcmtElev = contextPlcmtElev;
                this.GenProps.mapConvHeight = mapConvHeight;
            }
            else
            {
                this.GenProps = new GeneralProperties();
                this.GenProps.SiteElevDict = siteElevDict;
                this.GenProps.SitePlcmtZDict = sitePlcmtZ;
                this.GenProps.ContextPlcmtElev = contextPlcmtElev;
                this.GenProps.mapConvHeight = mapConvHeight; 
            }
        }
        
        private void checkForLevel10()
        {

            foreach (var entity in BuildingsAndSites)
            {
                IIfcPostalAddress address = null!;
                if (entity is IIfcSite)
                {
                    address = ((IIfcSite)entity).SiteAddress;
                }
                else if (entity is IIfcBuilding)
                {
                    address = ((IIfcBuilding)entity).BuildingAddress;
                }

                var level10 = new Level10();

                if (address != null)
                {
                    level10.IsFullFilled = true;
                    level10.PostalAddress = address;
                    level10.ReferencedEntity = entity;
                }
                else
                {
                    level10.IsFullFilled = false;
                    level10.PostalAddress = address;
                    level10.ReferencedEntity = entity;
                }
                this.LoGeoRef10.Add(level10);
            }   
        }

        private void checkForLevel20()
        {
            var sites = BuildingsAndSites.OfType<IIfcSite>().ToList();

            foreach (var site in sites)
            {
                var lvl20 = new Level20();

                if (site.RefLatitude.HasValue && site.RefLongitude.HasValue)
                {
                    lvl20.Latitude = site.RefLatitude.Value.AsDouble;
                    lvl20.Longitude = site.RefLongitude.Value.AsDouble;
                    lvl20.Elevation =  site.RefElevation.HasValue ? site.RefElevation.Value: null;
                    lvl20.ReferencedEntity = site;
                    lvl20.IsFullFilled = true;

                    if (!(-90 < lvl20.Latitude && lvl20.Latitude < 90 ))
                    {
                        Log.Error($"Latitude not in range of -90 - 90 Degree. Latitude is: {lvl20.Latitude}");
                        lvl20.IsFullFilled = false;
                    }
                    if (!(-180 < lvl20.Longitude && lvl20.Longitude < 180))
                    {
                        Log.Error($"Longitude not in range of -180 - 180 Degree. Longitude is: {lvl20.Longitude}");
                        lvl20.IsFullFilled = false;
                    }

                    if (lvl20.IsFullFilled)
                    {

                    }

                    if (lvl20.Elevation == 0)
                    {
                        Log.Warning($"Elevation might not be properly specified");
                    }
                    
                }
                else
                {
                    lvl20.IsFullFilled = false;
                    lvl20.ReferencedEntity = site;
                }
                
                this.LoGeoRef20.Add(lvl20);

            }
        }

        private void checkForLevel30() 
        {
            foreach (var entity in BuildingsAndSites)
            {
                var localPlcm = (IIfcLocalPlacement)entity.ObjectPlacement;

                var level30 = new Level30();
                level30.ReferencedEntity = entity;

                if (localPlcm.PlacementRelTo == null)
                {
                    
                    level30.plcmt = (IIfcPlacement)localPlcm.RelativePlacement;

                    var location = level30.plcmt.Location;
                    if (location.X > 0.0 || location.Y > 0.0 || location.Z > 0.0) 
                    {
                        level30.IsFullFilled = true;
                    }
                    else
                    {
                        level30.IsFullFilled = false;
                    }
                }
                this.LoGeoRef30.Add(level30);
            }
        }

        private void checkForLevel40And50()
        {
            var projects = this.model.Instances.OfType<IIfcProject>().ToList();

            if (projects.Count != 1)
            {
                Log.Information("Ifc file does not contain an IfcProject. Invalid file!");
                return;
            }

            var proj = projects.FirstOrDefault();

            if (proj == null)
            {
                this.LoGeoRef40.Add(new Level40()
                {
                    IsFullFilled = false,
                    project = null
                }) ;
                return;
            }

            var allCtx = proj.RepresentationContexts.OfType<IIfcGeometricRepresentationContext>();  //includes also inherited SubContexts (not necessary for this application)
            var noSubCtx = allCtx.Where(ctx => ctx.ExpressType.ToString() != "IfcGeometricRepresentationSubContext").ToList(); //avoid subs (unneccessary overhead)

            foreach (var context in noSubCtx)
            {
                var lvl40 = new Level40();
                lvl40.IsFullFilled = false;
                lvl40.project = proj;
                lvl40.context = context;
                lvl40.trueNorth = context.TrueNorth;

                //lvl50.mapConversion = this.model.Instances.OfType<IIfcMapConversion>().ToList();

                var wcsPlcmt = context.WorldCoordinateSystem;
                if (wcsPlcmt != null)
                {
                    
                    var wcs = (IIfcAxis2Placement3D)wcsPlcmt;
                    lvl40.wcs = wcs;
                    var location = wcs.Location;
                    

                    if (location.X > 0 || location.Y > 0 || location.Z > 0) 
                    {
                        lvl40.IsFullFilled = true;    
                    }
                    
                    this.LoGeoRef40.Add(lvl40);
                }

                var lvl50 = new Level50();
                lvl50.IsFullFilled = false;
                lvl50.context = context;
                lvl50.project = proj;

                foreach (var oper in lvl50.context.HasCoordinateOperation)
                {
                    if (oper != null)
                    {
                        var mapConv = (IIfcMapConversion)oper;

                        if (mapConv != null)
                        {
                            lvl50.MapConversion = mapConv;
                            lvl50.IsFullFilled = true;
                            if (0.9 < mapConv.Scale && mapConv.Scale < 1.1)
                            {
                                Log.Warning("Scale of map conversion is between 0.9 and 1.1. This might not be used for conversion of units.");
                            }
                            if (mapConv.XAxisAbscissa != null || mapConv.XAxisOrdinate != null)
                            {
                                if (context.TrueNorth != null)
                                {
                                    var angleTrueNorth = Math.Atan2(context.TrueNorth.X, context.TrueNorth.Y) * (180 / Math.PI);
                                    var angleMapConv = Math.Atan2((double)mapConv.XAxisOrdinate!, (double)mapConv.XAxisAbscissa!) * (180/Math.PI);
                                    Log.Warning("Ifc file contains both true north from the geometric representation context and a rotation angle from the map conversion");
                                    Log.Warning($"True north is: {angleTrueNorth}° and map conversion rotation is: {angleMapConv}°");
                                }
                            }
                        }
                    }
                }
                this.LoGeoRef50.Add(lvl50);
            }
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

        public void WriteProtocoll(string WorkingDirPath)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{_translationService.Translate("ProtocolHeader", CultureInfo.CurrentCulture)} {this.model.FileName}");
            sb.AppendLine($"{_translationService.Translate("IfcVersion", CultureInfo.CurrentCulture)}: {this.model.SchemaVersion}");
            sb.AppendLine($"{_translationService.Translate("CheckedOn", CultureInfo.CurrentCulture)}: {this.TimeChecked}");
            sb.AppendLine($"{_translationService.Translate("MaxCoordinates", CultureInfo.CurrentCulture)}: X: {this.GenProps!.X} Y: {this.GenProps.Y} Z: {this.GenProps.Z}");
            sb.AppendLine();
            sb.AppendLine($"{_translationService.Translate("RefElevationAndZ", CultureInfo.CurrentCulture)}: ");


            /*
            sb.AppendLine($"IFCGeoRefChecker protocoll for file {this.model.FileName}");
            sb.AppendLine($"IfcVersion: {this.model.SchemaVersion}");
            sb.AppendLine($"Checked on {this.TimeChecked}");
            sb.AppendLine($"Maximum coordinates are X: {this.GenProps!.X} Y: {this.GenProps.Y} Z: {this.GenProps.Z}");
            sb.AppendLine();
            sb.AppendLine($"Ref elevation and Placement Z-Values of site are: ");//+ String.Join(' ', this.GenProps.SiteElevDict!.Values.ToList()));
            */
            foreach (var site in this.GenProps.SitePlcmtZDict!)
            {
                var elevation = this.GenProps.SiteElevDict!.ContainsKey(site.Key) ? Invariant($"{GenProps.SiteElevDict[site.Key]}") : _translationService.Translate("NotSpecified", CultureInfo.CurrentCulture);
                sb.AppendLine($"GUID: {site.Key}\t\t RefElevation: {elevation}\t\t{_translationService.Translate("PlacementZCoordinates", CultureInfo.CurrentCulture)}: {site.Value}");
                //var elevation = this.GenProps.SiteElevDict!.ContainsKey(site.Key) ? Invariant($"{GenProps.SiteElevDict[site.Key]}") : "not specified";
                //sb.AppendLine($"GUID: {site.Key}\t\t RefElevation: {elevation}\t\tPlacement Z-coordinates: {site.Value}");
            }
            sb.AppendLine();

            sb.AppendLine($"IfcGeometricRepresentationContext {_translationService.Translate("PlacementZCoordinates", CultureInfo.CurrentCulture)}:");
            //sb.AppendLine($"IfcGeometricRepresentationContext placement z-coordinates are:"); // + String.Join(' ', this.GenProps.ContextPlcmtElev!.Values.ToList()));
            foreach (var context in this.GenProps.ContextPlcmtElev!)
            {
                sb.AppendLine($"Context {context.Key}\t\t {_translationService.Translate("PlacementZCoordinates", CultureInfo.CurrentCulture)}: {context.Value}");
                //sb.AppendLine($"Context {context.Key}\t\t Placement Z-coordinate: {context.Value}");
            }

            sb.AppendLine();
            if (this.GenProps.mapConvHeight != null) 
            {
                sb.AppendLine($"{_translationService.Translate("MapConversion", CultureInfo.CurrentCulture)}: {this.GenProps.mapConvHeight}");
                //sb.AppendLine($"Map Conversion orthoghonal height is: {this.GenProps.mapConvHeight}");
            }


            sb.AppendLine();
            sb.AppendLine(starLine);
            sb.AppendLine();
            
            sb.AppendLine(WriteResultLvl10());
            sb.AppendLine(WriteResultLvl20());
            sb.AppendLine(WriteResultLvl30());
            sb.AppendLine(WriteResultLvl40());
            sb.AppendLine(WriteResultLvl50());

            var protocoll = sb.ToString();

            string protocollFileName = Path.GetFileNameWithoutExtension(this.model.FileName) + "__CheckResult.txt";
            string protocollOutPath = Path.Combine(WorkingDirPath, protocollFileName);

            using (var file = File.CreateText(protocollOutPath))
            {
                file.WriteLine(protocoll);
            }

            this.ProtocollPath = protocollOutPath;

        }

        private string WriteResultLvl10()
        {
            var sb = new StringBuilder();

            var lvl10Result = this.getCheckResults().level10Fulfilled;
            var result = (lvl10Result.HasValue && lvl10Result.Value) ? $"LoGeoRef10 {_translationService.Translate("Fulfilled", CultureInfo.CurrentCulture)}" : $"LoGeoRef10 {_translationService.Translate("NotFulfilled", CultureInfo.CurrentCulture)}";
            //var result = (lvl10Result.HasValue && lvl10Result.Value) ? $"LoGeoRef 10 is fulfilled \u2713" : $"LoGeoRef 10 is not fulfilled";
            sb.AppendLine(result);
            sb.AppendLine();
            sb.AppendLine(dashLine);
            sb.AppendLine();
            
            foreach (var lvl10 in this.LoGeoRef10)
            {
                if (lvl10.IsFullFilled)
                {
                    string header = $"{_translationService.Translate("PostalAddress", CultureInfo.CurrentCulture)}{lvl10.ReferencedEntity!.EntityLabel} {lvl10.ReferencedEntity!.GetType().Name} {_translationService.Translate("With", CultureInfo.CurrentCulture)} GUID {lvl10.ReferencedEntity.GlobalId}";
                    //string header = $"Postal address referenced by Entity #{lvl10.ReferencedEntity!.EntityLabel} {lvl10.ReferencedEntity!.GetType().Name} with GUID {lvl10.ReferencedEntity.GlobalId}";
                    sb.AppendLine(header);

                    var PostalAddress = lvl10.PostalAddress;

                    string info = $"{_translationService.Translate("Country", CultureInfo.CurrentCulture)}: {(PostalAddress!.Country != "" ? PostalAddress!.Country : _translationService.Translate("NotSpecified", CultureInfo.CurrentCulture))} \t\t{_translationService.Translate("Region", CultureInfo.CurrentCulture)}: {(PostalAddress!.Region != "" ? PostalAddress!.Region : _translationService.Translate("NotSpecified", CultureInfo.CurrentCulture))}";
                    info += $"\n{_translationService.Translate("Town", CultureInfo.CurrentCulture)}: {(PostalAddress!.Town != "" ? PostalAddress!.Town : _translationService.Translate("NotSpecified", CultureInfo.CurrentCulture))} \t\t{_translationService.Translate("PostalCode", CultureInfo.CurrentCulture)}: {(PostalAddress!.PostalCode != "" ? PostalAddress!.PostalCode : _translationService.Translate("NotSpecified", CultureInfo.CurrentCulture))}";
                    foreach(var line in PostalAddress.AddressLines)
                    {
                        info += $"\n{_translationService.Translate("Address", CultureInfo.CurrentCulture)}: {(line.ToString() != "" ? line.ToString() : _translationService.Translate("NotSpecified", CultureInfo.CurrentCulture))}";
                    }
                    sb.AppendLine(info);

                    sb.AppendLine();
                    sb.AppendLine($"LoGeoRef10 {_translationService.Translate("Fulfilled", CultureInfo.CurrentCulture)}");
                }
                else
                {
                    sb.AppendLine($"{_translationService.Translate("NoPostalAddress", CultureInfo.CurrentCulture)}{lvl10.ReferencedEntity!.EntityLabel} {lvl10.ReferencedEntity!.GetType().Name} {_translationService.Translate("With", CultureInfo.CurrentCulture)} GUID {lvl10.ReferencedEntity.GlobalId}");
                    sb.AppendLine();
                    sb.AppendLine($"LoGeoRef10 {_translationService.Translate("NotFulfilled", CultureInfo.CurrentCulture)}");
                }

                sb.AppendLine();
                sb.AppendLine(dashLine);
                sb.AppendLine();
                
            }

            sb.AppendLine(starLine);
            sb.AppendLine();

            return sb.ToString();
        }

        private string WriteResultLvl20()
        {
            var sb = new StringBuilder();

            var lvl20Result = this.getCheckResults().level20Fulfilled;
            var result = (lvl20Result.HasValue && lvl20Result.Value) ? $"LoGeoRef20 {_translationService.Translate("Fulfilled", CultureInfo.CurrentCulture)}" : $"LoGeoRef20 {_translationService.Translate("NotFulfilled", CultureInfo.CurrentCulture)}";
            //var result = (lvl20Result.HasValue && lvl20Result.Value) ? $"LoGeoRef 20 is fulfilled \u2713" : $"LoGeoRef 20 is not fulfilled";

            sb.AppendLine(result);
            sb.AppendLine();
            sb.AppendLine(dashLine);
            sb.AppendLine();

            foreach (var lvl20 in this.LoGeoRef20)
            {
                if (lvl20.IsFullFilled)
                {
                    sb.AppendLine($"{_translationService.Translate("GeographicLocation", CultureInfo.CurrentCulture)}{lvl20.ReferencedEntity!.EntityLabel} {lvl20.ReferencedEntity!.GetType().Name} {_translationService.Translate("With", CultureInfo.CurrentCulture)} GUID {lvl20.ReferencedEntity.GlobalId}");
                    sb.AppendLine(Invariant($"Latitude: {(lvl20!.Latitude != null ? lvl20!.Latitude : _translationService.Translate("NotSpecified", CultureInfo.CurrentCulture))} \t\tLongitude: {(lvl20!.Longitude != null ? lvl20!.Longitude : _translationService.Translate("NotSpecified", CultureInfo.CurrentCulture))}"));
                    sb.AppendLine(Invariant($"Elevation: {(lvl20!.Elevation != null ? lvl20!.Elevation : _translationService.Translate("NotSpecified", CultureInfo.CurrentCulture))}"));
                    _ = lvl20.GeographicDescription != null ? sb.AppendLine($"{_translationService.Translate("AccordingCoordinates", CultureInfo.CurrentCulture)} {lvl20.GeographicDescription}") : null;
                    sb.AppendLine();
                    sb.AppendLine($"LoGeoRef20 {_translationService.Translate("Fulfilled", CultureInfo.CurrentCulture)}");
                }
                else
                {
                    sb.AppendLine($"{_translationService.Translate("NoGeographicLocation", CultureInfo.CurrentCulture)}{lvl20.ReferencedEntity!.EntityLabel} {lvl20.ReferencedEntity!.GetType().Name} {_translationService.Translate("With", CultureInfo.CurrentCulture)} GUID {lvl20.ReferencedEntity.GlobalId}");
                    sb.AppendLine(Invariant($"Latitude: {(lvl20!.Latitude != null ? lvl20!.Latitude : _translationService.Translate("NotSpecified", CultureInfo.CurrentCulture))} \t\tLongitude: {(lvl20!.Longitude != null ? lvl20!.Longitude : _translationService.Translate("NotSpecified", CultureInfo.CurrentCulture))}"));
                    sb.AppendLine(Invariant($"Elevation: {(lvl20!.Elevation != null ? lvl20!.Elevation : _translationService.Translate("NotSpecified", CultureInfo.CurrentCulture))}"));
                    sb.AppendLine();
                    sb.AppendLine($"LoGeoRef20 {_translationService.Translate("NotFulfilled", CultureInfo.CurrentCulture)}");
                }

                sb.AppendLine();
                sb.AppendLine(dashLine);
                sb.AppendLine();
            }

            if (this.LoGeoRef20.Count < 1)
            {
                sb.AppendLine($"{_translationService.Translate("NoSite", CultureInfo.CurrentCulture)}");
                sb.AppendLine();
                sb.AppendLine(dashLine);
                sb.AppendLine();
            }

            
            sb.AppendLine();
            sb.AppendLine(starLine);
            sb.AppendLine();

            return sb.ToString();
        }

        private string WriteResultLvl30()
        {
            var sb = new StringBuilder();

            var lvl30Result = this.getCheckResults().level30Fulfilled;
            var result = (lvl30Result.HasValue && lvl30Result.Value) ? $"LoGeoRef30 {_translationService.Translate("Fulfilled", CultureInfo.CurrentCulture)}" : $"LoGeoRef30 {_translationService.Translate("NotFulfilled", CultureInfo.CurrentCulture)}";
            //var result = (lvl30Result.HasValue && lvl30Result.Value) ? $"LoGeoRef30 is fulfilled \u2713" : $"LoGeoRef30 is not fulfilled";

            sb.AppendLine(result);
            sb.AppendLine();
            sb.AppendLine(dashLine);
            sb.AppendLine();

            foreach (var lvl30 in this.LoGeoRef30)
            {
                if (lvl30.plcmt != null)
                {
                    sb.AppendLine($"Upper most entity of spatial structures is: #{lvl30.ReferencedEntity!.EntityLabel} {lvl30.ReferencedEntity!.GetType().Name} {_translationService.Translate("With", CultureInfo.CurrentCulture)} GUID: {lvl30.ReferencedEntity.GlobalId}");
                    sb.AppendLine((lvl30.IsFullFilled) ? $"Local placement of this entity has geographic context" : $"Local placement of this entity has no geographic context");
                    sb.AppendLine(Invariant($"Coordinates of the location are:\nX: {lvl30.plcmt.Location.X} \nY: {lvl30.plcmt.Location.Y} \nZ: {lvl30.plcmt.Location.Z}"));
                        
                    if (lvl30.plcmt.GetType().Name == "IfcAxis2Placement3D")
                    {
                        var plcmt = (IIfcAxis2Placement3D)lvl30.plcmt;
                        sb.AppendLine(Invariant($"Direction of X-axis is {(plcmt.RefDirection == null ? "(1 | 0 | 0)" : $"({plcmt.RefDirection.X} | {plcmt.RefDirection.Y} | {plcmt.RefDirection.Z})" )}"));
                        sb.AppendLine(Invariant($"Direction of Z-axis is {(plcmt.Axis == null ? "(0 | 0 | 1)" : $"({plcmt.Axis.X} | {plcmt.Axis.Y} | {plcmt.Axis.Z}")})"));
                    }
                    else if (lvl30.plcmt.GetType().Name == "IfcAxis2Placement2D") 
                    {
                        var plcmt = (IIfcAxis2Placement2D)lvl30.plcmt;
                        sb.AppendLine(Invariant($"Direction of X-axis is  ({plcmt.RefDirection.X} | {plcmt.RefDirection.Y})"));
                    }
                    sb.AppendLine();

                    sb.AppendLine((lvl30.IsFullFilled) ? $"LoGeoRef30 {_translationService.Translate("Fulfilled", CultureInfo.CurrentCulture)}" : $"LoGeoRef30 {_translationService.Translate("NotFulfilled", CultureInfo.CurrentCulture)}");
                    //sb.AppendLine((lvl30.IsFullFilled) ? $"LoGeoRef30 is fulfilled \u2713" : $"LoGeoRef30 is not fulfilled");

                    sb.AppendLine();
                    sb.AppendLine(dashLine);
                    sb.AppendLine();
                }
            }

            sb.AppendLine(starLine);
            sb.AppendLine();

            return sb.ToString();
        }
        
        private string WriteResultLvl40()
        {
            var sb = new StringBuilder();

            var lvl40Result = this.getCheckResults().level40Fulfilled;
            var result = (lvl40Result.HasValue && lvl40Result.Value) ? $"LoGeoRef40 {_translationService.Translate("Fulfilled", CultureInfo.CurrentCulture)}" : $"LoGeoRef40 {_translationService.Translate("NotFulfilled", CultureInfo.CurrentCulture)}";
            //var result = (lvl40Result.HasValue && lvl40Result.Value) ? $"LoGeoRef40 is fulfilled \u2713" : $"LoGeoRef 40 is not fulfilled";

            sb.AppendLine(result);
            sb.AppendLine();
            sb.AppendLine(dashLine);
            sb.AppendLine();

            foreach (var lvl40 in this.LoGeoRef40)
            {
                if (lvl40.context != null)
                {
                    sb.AppendLine($"IfcProject (#{lvl40.project!.EntityLabel}, {lvl40.project!.GlobalId}) references IfcGeometricRepresentationContext (#{lvl40.context.EntityLabel}) of type: {lvl40.context.ContextType}");

                    if (lvl40.IsFullFilled)
                    {
                        sb.AppendLine($"Parameters of the World Coordinate System:");
                        sb.AppendLine($"Coordinates of the location are:");
                        sb.AppendLine($"X: {lvl40.wcs!.Location.X}");
                        sb.AppendLine($"Y: {lvl40.wcs.Location.Y}");
                        sb.AppendLine($"Z: {lvl40.wcs.Location.Z}");
                        sb.AppendLine();
                        sb.AppendLine($"True North is: {lvl40.trueNorth!.X} / {lvl40.trueNorth.Y}");
                        //sb.AppendLine($"True North is: {lvl40.trueNorth!.X} / {lvl40.trueNorth.Y} / {lvl40.trueNorth.Z}"); //trueNorth.Z existiert nicht

                        sb.AppendLine();
                        sb.AppendLine($"LoGeoRef40 {_translationService.Translate("Fulfilled", CultureInfo.CurrentCulture)}");
                    }
                    else 
                    {
                        sb.AppendLine($"Attribute World Coordinate System of IfcGeometricRepresentationContext is not used for georeferencing:");
                        sb.AppendLine($"Coordinates of the location are:");
                        sb.AppendLine($"X: {lvl40.wcs!.Location.X}");
                        sb.AppendLine($"Y: {lvl40.wcs.Location.Y}");
                        sb.AppendLine($"Z: {lvl40.wcs.Location.Z}");
                        sb.AppendLine();
                        if (lvl40.trueNorth != null)
                        {
                            sb.AppendLine($"True North is: ({lvl40.trueNorth.X} | {lvl40.trueNorth.Y})");
                        }
                        else
                        {
                            sb.AppendLine("True North is not specified. This defaults to (0 | 1)");
                        }
                        sb.AppendLine();
                        sb.AppendLine($"LoGeoref40 {_translationService.Translate("NotFulfilled", CultureInfo.CurrentCulture)}");
                    }
                }

                sb.AppendLine();
                sb.AppendLine(dashLine);
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine(starLine);
            sb.AppendLine();

            return sb.ToString();
        }
        
        private string WriteResultLvl50()
        {
            var sb = new StringBuilder();

            var lvl50Result = this.getCheckResults().level50Fulfilled;
            var result = (lvl50Result.HasValue && lvl50Result.Value) ? $"LoGeoRef50 {_translationService.Translate("Fulfilled", CultureInfo.CurrentCulture)}" : $"LoGeoRef50 {_translationService.Translate("NotFulfilled", CultureInfo.CurrentCulture)}";
            //var result = (lvl50Result.HasValue && lvl50Result.Value) ? $"LoGeoRef50 is fulfilled \u2713" : $"LoGeoRef 50 is not fulfilled"; 

            sb.AppendLine(result);
            sb.AppendLine();
            sb.AppendLine(dashLine);
            sb.AppendLine();

            foreach (var lvl50 in this.LoGeoRef50)
            {
                if (lvl50.MapConversion != null) //und Eastings > 0 und Northing > 0 ? -> in LoGeoRef Spezifikation nochmal Voraussetzungen prüfen
                {
                    var targetCRS = lvl50.MapConversion.TargetCRS;
                    var eastings = lvl50.MapConversion.Eastings;
                    var northings = lvl50.MapConversion.Northings;
                    var height = lvl50.MapConversion.OrthogonalHeight;
                    var xAxisAbscissa = lvl50.MapConversion.XAxisAbscissa;
                    var XaxisOrdinate = lvl50.MapConversion.XAxisOrdinate;

                    sb.AppendLine($"IfcMapConversion defined in #{lvl50.MapConversion.EntityLabel} for {lvl50.context!.GetType().Name} (#{lvl50.context.EntityLabel}) for ContextType {lvl50.context.ContextType}");
                    sb.AppendLine($"Translation Eastings: {lvl50.MapConversion.Eastings}");
                    sb.AppendLine($"Translation Northings: {lvl50.MapConversion.Northings}");
                    sb.AppendLine($"Translation Height: {lvl50.MapConversion.OrthogonalHeight}");
                    sb.AppendLine($"Rotation X-Axis Abscissa: {lvl50.MapConversion.XAxisAbscissa}");
                    sb.AppendLine($"Rotation X-Axis Ordinate: {lvl50.MapConversion.XAxisOrdinate}");
                    sb.AppendLine($"Scale: {lvl50.MapConversion.Scale}");

                    if (lvl50.MapConversion.Scale < 0.9 && lvl50.MapConversion.Scale < 1.1)
                    {
                        sb.AppendLine("Scale of map conversion is between 0.9 and 1.1. This might not be used for conversion of units.");
                    }

                    sb.AppendLine();

                    sb.AppendLine($"Target CRS is: {lvl50.MapConversion.TargetCRS.Name}");
                    sb.AppendLine($"Description: {(lvl50.MapConversion.TargetCRS.Description.HasValue ? lvl50.MapConversion.TargetCRS.Description : "not specified")}");
                    sb.AppendLine($"Geodetic Datum: {(lvl50.MapConversion.TargetCRS.GeodeticDatum.HasValue ? lvl50.MapConversion.TargetCRS.GeodeticDatum : "not specified")}");
                    sb.AppendLine($"Vertical Datum: {(lvl50.MapConversion.TargetCRS.VerticalDatum.HasValue ? lvl50.MapConversion.TargetCRS.VerticalDatum : "not specified")}");

                    sb.AppendLine();
                    sb.AppendLine($"LoGeoRef50 {_translationService.Translate("Fulfilled", CultureInfo.CurrentCulture)}");

                }
                else
                {
                    sb.AppendLine($"{(lvl50.context != null ? $"No IfcMapConversion specified by #{lvl50.context.EntityLabel} IfcGeometricRepresentationContext for ContextType {lvl50.context.ContextType}" : "Found no IfcGeometricRepresentationContext")}");
                    sb.AppendLine();
                    sb.AppendLine($"LoGeoRef50 {_translationService.Translate("NotFulfilled", CultureInfo.CurrentCulture)}");

                }

                sb.AppendLine();
                sb.AppendLine(dashLine);
                sb.AppendLine();

            }

            if (this.LoGeoRef50.Count < 1)
            {
                sb.AppendLine("No IfcMapConversion specified in file");
            }

            sb.AppendLine();
            sb.AppendLine(starLine);
            sb.AppendLine();

            return sb.ToString();
        }
        
        private string dashLine = "-------------------------------------------------------------------------------------------";
        private string starLine = "*******************************************************************************************";
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
