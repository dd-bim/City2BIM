using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using static System.FormattableString;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Serilog;

using IFCGeorefShared.Levels;
using Xbim.Ifc4.ProductExtension;
using System.Text;


namespace IFCGeorefShared
{
    public class GeoRefChecker
    {
        public string? TimeCheckedFileCreated { get; set; }
        public string? TimeChecked { get; set; }

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
        IList<Level50> LoGeoRef50 { get; set; } = new List<Level50>();

        private IfcStore model { get; set; }
        private List<IIfcSpatialStructureElement> BuildingsAndSites = new List<IIfcSpatialStructureElement>(); 

        public GeoRefChecker(IfcStore model) {
            this.model = model;
            BuildingsAndSites = new IIfcSpatialStructureElement[0]
                .Concat(model.Instances.OfType<IIfcSite>())
                .Concat(model.Instances.OfType<IIfcBuilding>()).ToList();

            checkForLevel10();
            checkForLevel20();
            checkForLevel30();
            checkForLevel40();

            this.TimeChecked = DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss");
        }

        private void checkForLevel10()
        {

            foreach (var entity in BuildingsAndSites)
            {
                IIfcPostalAddress address = null;
                if (entity is IIfcSite)
                {
                    address = (entity as IIfcSite).SiteAddress;
                }
                else if (entity is IIfcBuilding)
                {
                    address = (entity as IIfcBuilding).BuildingAddress;
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
                    lvl20.IsFullFilled  = true;
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

        private void checkForLevel40()
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

                //lvl50.mapConversion = this.model.Instances.OfType<IIfcMapConversion>().ToList();

                var wcsPlcmt = context.WorldCoordinateSystem;
                if (wcsPlcmt != null)
                {
                    
                    var wcs = (IIfcAxis2Placement3D)wcsPlcmt;
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
                            lvl50.mapConversion = mapConv;
                            lvl50.IsFullFilled = true;
                        }
                    }
                }
                this.LoGeoRef50.Add(lvl50);
            }
        }
    
        private void updateAddress()
        {
            
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

            string header = $"IFCGeoRefChecker protocoll for file {this.model.FileName} \n" +
                            $"IfcVersion: {this.model.SchemaVersion} \n" +
                            $"Checked on {this.TimeChecked}";

            sb.AppendLine(header);
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
            var result = (lvl10Result.HasValue && lvl10Result.Value) ? $"LoGeoRef 10 is fullfilled" : $"LoGeoRef 10 is not fullfilled";
            sb.AppendLine(result);
            sb.AppendLine();
            sb.AppendLine(dashLine);
            sb.AppendLine();
            
            foreach (var lvl10 in this.LoGeoRef10)
            {
                if (lvl10.IsFullFilled)
                {
                    string header = $"Postal address referenced by Entity: {lvl10.ReferencedEntity!.GetType().Name} with GUID {lvl10.ReferencedEntity.GlobalId}";
                    sb.AppendLine(header);

                    var PostalAddress = lvl10.PostalAddress;

                    string info = $"Country: {(PostalAddress!.Country != "" ? PostalAddress!.Country : "not specified!")} \t\tRegion: {(PostalAddress!.Region != "" ? PostalAddress!.Region : "not specified!")}";
                    info += $"\nTown: {(PostalAddress!.Town != "" ? PostalAddress!.Town : "not specified!")} \t\tPostal Code: {(PostalAddress!.PostalCode != "" ? PostalAddress!.PostalCode : "not specified!")}";
                    foreach(var line in PostalAddress.AddressLines)
                    {
                        info += $"\nAddress: {(line.ToString() != "" ? line.ToString() : "not specified!")}";
                    }
                    sb.AppendLine(info);

                    sb.AppendLine();
                    sb.AppendLine($"LoGeoRef10 is fullfilled");
                }
                else
                {
                    sb.AppendLine($"No postal address found for Entity: {lvl10.ReferencedEntity!.GetType().Name} with GUID {lvl10.ReferencedEntity.GlobalId}");
                    sb.AppendLine();
                    sb.AppendLine($"LoGeoRef10 is not fullfilled");
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
            var result = (lvl20Result.HasValue && lvl20Result.Value) ? $"LoGeoRef 20 is fullfilled" : $"LoGeoRef 20 is not fullfilled";

            sb.AppendLine(result);
            sb.AppendLine();
            sb.AppendLine(dashLine);
            sb.AppendLine();

            foreach (var lvl20 in this.LoGeoRef20)
            {
                if (lvl20.IsFullFilled)
                {
                    sb.AppendLine($"Geographic location specified by Entity: {lvl20.ReferencedEntity!.GetType().Name} with GUID {lvl20.ReferencedEntity.GlobalId}");
                    sb.AppendLine(Invariant($"Latitude: {(lvl20!.Latitude != null ? lvl20!.Latitude : "not specified!")} \t\tLongitude: {(lvl20!.Longitude != null ? lvl20!.Longitude : "not specified!")}"));
                    sb.AppendLine(Invariant($"Elevation: {(lvl20!.Elevation != null ? lvl20!.Elevation : "not specified")}"));
                    sb.AppendLine();
                    sb.AppendLine($"LoGeoRef20 is fullfilled \u2713");
                }
                else
                {
                    sb.AppendLine($"No geographic location found for Entity: {lvl20.ReferencedEntity!.GetType().Name} with GUID {lvl20.ReferencedEntity.GlobalId}");
                    sb.AppendLine();
                    sb.AppendLine($"LoGeoRef20 is not fullfilled");
                }
            }

            if (this.LoGeoRef20.Count < 1)
            {
                sb.AppendLine($"Checked file does not contain any site!");
            }

            sb.AppendLine();
            sb.AppendLine(dashLine);
            sb.AppendLine();
            sb.AppendLine(starLine);
            sb.AppendLine();

            return sb.ToString();
        }

        private string WriteResultLvl30()
        {
            var sb = new StringBuilder();

            var lvl30Result = this.getCheckResults().level30Fulfilled;
            var result = (lvl30Result.HasValue && lvl30Result.Value) ? $"LoGeoRef30 is fullfilled \u2713" : $"LoGeoRef30 is not fullfilled";

            sb.AppendLine(result);
            sb.AppendLine();
            sb.AppendLine(dashLine);
            sb.AppendLine();

            foreach (var lvl30 in this.LoGeoRef30)
            {
                if (lvl30.plcmt != null)
                {
                    sb.AppendLine($"Upper most entity of spatial structures is: {lvl30.ReferencedEntity!.GetType().Name} with GUID: {lvl30.ReferencedEntity.GlobalId}");
                    sb.AppendLine((lvl30.IsFullFilled) ? $"Local placement of this entity has geographic context" : $"Local placement of this entity has no geographic context");
                    sb.AppendLine(Invariant($"Coordinates of the location are:\nX: {lvl30.plcmt.Location.X} \nY: {lvl30.plcmt.Location.Y} \nZ: {lvl30.plcmt.Location.Z}"));
                        
                    if (lvl30.plcmt.GetType().Name == "IfcAxis2Placement3D")
                    {
                        var plcmt = (IIfcAxis2Placement3D)lvl30.plcmt;
                        sb.AppendLine(Invariant($"Direction of X-axis is {(plcmt.RefDirection == null ? "1 / 0 / 0" : $"{plcmt.RefDirection.X} / {plcmt.RefDirection.Y} / {plcmt.RefDirection.Z}" )}"));
                        sb.AppendLine(Invariant($"Direction of Z-axis is {(plcmt.Axis == null ? "0 / 0 / 1" : $"{plcmt.Axis.X} / {plcmt.Axis.Y} / {plcmt.Axis.Z}")}"));
                    }
                    else if (lvl30.plcmt.GetType().Name == "IfcAxis2Placement2D") 
                    {
                        var plcmt = (IIfcAxis2Placement2D)lvl30.plcmt;
                        sb.AppendLine(Invariant($"Direction of X-axis is  {plcmt.RefDirection.X} / {plcmt.RefDirection.Y}"));
                    }
                    sb.AppendLine();
                    
                    sb.AppendLine((lvl30.IsFullFilled) ? $"LoGeoRef30 is fullfilled \u2713" : $"LoGeoRef30 is not fullfilled");

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
            var result = (lvl40Result.HasValue && lvl40Result.Value) ? $"LoGeoRef40 is fullfilled \u2713" : $"LoGeoRef 40 is not fullfilled";

            sb.AppendLine(result);
            sb.AppendLine();
            sb.AppendLine(dashLine);
            sb.AppendLine();

            foreach (var lvl40 in this.LoGeoRef40)
            {
                if (lvl40.context != null)
                {
                    sb.AppendLine($"IfcProject ({lvl40.project!.GlobalId}) references IfcGeometricRepresentationContext (Identifier: {lvl40.context.EntityLabel}) and type: {lvl40.context.ContextType}");

                    if (lvl40.IsFullFilled)
                    {
                        sb.AppendLine($"Parameters of the World Coordinate System:");
                        sb.AppendLine($"Coordinates of the location are:");
                        sb.AppendLine($"X: {lvl40.wcs!.Location.X}");
                        sb.AppendLine($"Y: {lvl40.wcs.Location.Y}");
                        sb.AppendLine($"Z: {lvl40.wcs.Location.Z}");
                        sb.AppendLine();
                        sb.AppendLine($"True North is: {lvl40.trueNorth!.X} / {lvl40.trueNorth.Y} / {lvl40.trueNorth.Z}");
                    }
                    else 
                    {
                        sb.AppendLine($"World Coordinate System is not properly specified");
                        sb.AppendLine();
                        sb.AppendLine("LoGeoref40 is not fullfilled");
                    }
                }

                sb.AppendLine();
                sb.AppendLine(dashLine);
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
            var result = (lvl50Result.HasValue && lvl50Result.Value) ? $"LoGeoRef50 is fullfilled \u2713" : $"LoGeoRef 50 is not fullfilled"; 

            sb.AppendLine(result);
            sb.AppendLine();
            sb.AppendLine(dashLine);
            sb.AppendLine();

            foreach (var lvl50 in this.LoGeoRef50)
            {
                if (lvl50.mapConversion != null)
                {
                    var targetCRS = lvl50.mapConversion.TargetCRS;
                    var eastings = lvl50.mapConversion.Eastings;
                    var northings = lvl50.mapConversion.Northings;
                    var height = lvl50.mapConversion.OrthogonalHeight;
                    var xAxisAbscissa = lvl50.mapConversion.XAxisAbscissa;
                    var XaxisOrdinate = lvl50.mapConversion.XAxisOrdinate;

                    sb.AppendLine($"IfcMapConversion defined in #{lvl50.mapConversion.EntityLabel}");
                    sb.AppendLine($"Translation Eastings: {lvl50.mapConversion.Eastings}");
                    sb.AppendLine($"Translation Northings: {lvl50.mapConversion.Northings}");
                    sb.AppendLine($"Translation Height: {lvl50.mapConversion.OrthogonalHeight}");
                    sb.AppendLine($"Rotation X-Axis Abscissa: {lvl50.mapConversion.XAxisAbscissa}");
                    sb.AppendLine($"Rotation X-Axis Ordinate: {lvl50.mapConversion.XAxisOrdinate}");
                    sb.AppendLine($"Scale: {lvl50.mapConversion.Scale}");

                    sb.AppendLine();

                    sb.AppendLine($"Target CRS is: {lvl50.mapConversion.TargetCRS.Name}");
                    sb.AppendLine($"Description: {(lvl50.mapConversion.TargetCRS.Description.HasValue ? lvl50.mapConversion.TargetCRS.Description : "not specified")}");
                    sb.AppendLine($"Geodetic Datum: {(lvl50.mapConversion.TargetCRS.GeodeticDatum.HasValue ? lvl50.mapConversion.TargetCRS.GeodeticDatum : "not specified")}");
                    sb.AppendLine($"Vertical Datum: {(lvl50.mapConversion.TargetCRS.VerticalDatum.HasValue ? lvl50.mapConversion.TargetCRS.VerticalDatum : "not specified")}");

                }
                else
                {
                    sb.AppendLine($"{(lvl50.context != null ? $"No IfcMapConversion specified by IfcGeometricRepresentationContext {lvl50.context.ContextType}" : "Found no IfcGeometricRepresentationContext")}");
                    sb.AppendLine();
                    sb.AppendLine("LoGeoRef50 is not fullfilled");
                }
            }

            if (this.LoGeoRef50.Count < 1)
            {
                sb.AppendLine("No IfcMapConversion specified in file");
            }

            sb.AppendLine();
            sb.AppendLine(dashLine);
            sb.AppendLine();
            sb.AppendLine(starLine);

            return sb.ToString();
        }
        
        private string dashLine = "-------------------------------------------------------------------------------------------";
        private string starLine = "*******************************************************************************************";
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
