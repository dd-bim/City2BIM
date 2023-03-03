using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

using IFCGeorefShared.Levels;
using Xbim.Ifc4.ProductExtension;
using System.Text;

namespace IFCGeorefShared
{
    internal class GeoRefChecker
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

        public IList<Level10> LoGeoRef10 { get; set; } = new List<Level10>();
        public IList<Level20> LoGeoRef20 { get; set; } = new List<Level20>();
        public IList<Level30> LoGeoRef30 { get; set; } = new List<Level30>();
        public IList<Level40> LoGeoRef40 { get; set; } = new List<Level40>();
        public IList<Level50> LoGeoRef50 { get; set; } = new List<Level50>();

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
                return;
            }

            var proj = projects.FirstOrDefault();

            if (proj == null)
            {
                return;
            }

            var allCtx = proj.RepresentationContexts.OfType<IIfcGeometricRepresentationContext>();  //includes also inherited SubContexts (not necessary for this application)
            var noSubCtx = allCtx.Where(ctx => ctx.ExpressType.ToString() != "IfcGeometricRepresentationSubContext").ToList(); //avoid subs (unneccessary overhead)

            foreach (var context in noSubCtx)
            {
                var wcsPlcmt = context.WorldCoordinateSystem;
                if (wcsPlcmt != null)
                {
                   
                }
                Console.WriteLine("");
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

            string header = $"IFCGeoRefChecker protocoll for file {this.model.FileName} \n" +
                            $"Checked on {this.TimeChecked}";

            var resultLvl10 = WriteResultLvl10();

            sb.AppendLine(header);
            sb.AppendLine("");
            sb.AppendLine(starLine);
            sb.AppendLine("");
            sb.AppendLine(resultLvl10);

            sb.AppendLine(starLine);
            sb.AppendLine("");

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
            sb.AppendLine("");
            sb.AppendLine(dashLine);
            sb.AppendLine("");
            
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
                }
                else
                {
                    string header = $"No postal address found for Entity: {lvl10.ReferencedEntity!.GetType().Name} with GUID {lvl10.ReferencedEntity.GlobalId}";
                    sb.AppendLine(header);
                }

                sb.AppendLine("");
                sb.AppendLine(dashLine);
                sb.AppendLine("");
            }
            return sb.ToString();
        }

        private string WriteResultLvl20()
        {
            var sb = new StringBuilder();

            foreach(var lvl20 in this.LoGeoRef20)
            {
                if (lvl20.IsFullFilled)
                {

                }
            }
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
