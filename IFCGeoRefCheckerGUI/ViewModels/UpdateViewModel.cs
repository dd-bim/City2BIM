using IFCGeorefShared;
using IFCGeorefShared.Levels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IFCGeoRefCheckerGUI.ValueConverters;
using Xbim.Ifc;
using System.Data;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using System.ComponentModel;

namespace IFCGeoRefCheckerGUI.ViewModels
{
    public class UpdateViewModel : BaseViewModel
    {
        private readonly GeoRefChecker? geoRefChecker;
        public GeoRefChecker? GeoRefChecker { get { return geoRefChecker; } }


        public DelegateCommand? StartExport { get; set; }

        private ObservableCollection<Level10UI>? level10s;
        public ObservableCollection<Level10UI>? Level10s
        {
            get => level10s;
            set
            {
                if (level10s != value)
                {
                    level10s = value;
                    this.RaisePropertyChanged();
                }
            }
        }
        

        private ObservableCollection<Level20>? level20s;
        public ObservableCollection<Level20>? Level20s
        {
            get => level20s;
            set
            {
                if (level20s != value)
                {
                    level20s = value;
                    this.RaisePropertyChanged();
                }
            }
        }
        
        private Level50UI? level50;
        public Level50UI? Level50
        {
            get => level50;
            set
            {
                if (level50 != value)
                {
                    level50 = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private readonly bool isIFC4;
        public bool IsIFC4
        {
            get => isIFC4;
        }

        private string? outIfcPath;
        public string? OutIfcPath
        {
            get => outIfcPath;
            set
            {
                if (outIfcPath != value)
                {
                    outIfcPath = value;
                }
            }
        }
        public UpdateViewModel(GeoRefChecker geoRefChecker)
        {
            this.geoRefChecker = geoRefChecker;
            var Level10UIs = new List<Level10UI>(geoRefChecker.LoGeoRef10.Count);
            foreach (var lvl10 in geoRefChecker.LoGeoRef10)
            {
                Level10UIs.Add(LevelsConverter.convertToLevel10UI(lvl10));
            }

            this.Level10s = new ObservableCollection<Level10UI>(Level10UIs);
            this.Level20s = new ObservableCollection<Level20>(geoRefChecker.LoGeoRef20);

            foreach (var lvl50 in geoRefChecker.LoGeoRef50)
            {
                if (lvl50.IsFullFilled)
                {
                    this.Level50 = LevelsConverter.convertToLevel50UI(lvl50);
                }
            }
            if (this.level50 == null)
            {
                this.Level50 = new Level50UI();
            }
            
            if (this.geoRefChecker.IFCVersion == Xbim.Common.Step21.XbimSchemaVersion.Ifc4)
            {
                this.isIFC4 = true;
            }

            var origPath = this.geoRefChecker.FilePath;
            this.OutIfcPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(origPath)!, System.IO.Path.GetFileNameWithoutExtension(origPath)) + "_mod.ifc";
            this.StartExport = new DelegateCommand((o) => ExecExport(o));
        }

        private void ExecExport(object o)
        {
            using (var model = IfcStore.Open(this.GeoRefChecker!.FilePath))
            {
                using (var trans = model.BeginTransaction())
                {
                    if (this.level10s != null && this.level10s.Count > 0)
                    {
                        foreach (var level10UI in level10s)
                        {
                            if (!level10UI.hasData()) { continue; }

                            var entity = model.Instances.FirstOrDefault<IIfcSpatialStructureElement>(elem => elem.GlobalId == level10UI.GUID);
                            var modelAddress = level10UI.ReferencedEntity == "IfcSite" ? ((IIfcSite)entity).SiteAddress : ((IIfcBuilding)entity).BuildingAddress;

                            if (modelAddress == null)
                            {
                                if (model.SchemaVersion == Xbim.Common.Step21.XbimSchemaVersion.Ifc2X3)
                                {
                                    modelAddress = model.Instances.New<Xbim.Ifc2x3.ActorResource.IfcPostalAddress>();
                                }
                                else
                                {
                                    modelAddress = model.Instances.New<Xbim.Ifc4.ActorResource.IfcPostalAddress>();
                                }
                            }

                            if (level10UI.ReferencedEntity == "IfcSite")
                            {
                                ((IIfcSite)entity).SiteAddress = modelAddress;
                            }
                            else
                            {
                                ((IIfcBuilding)entity).BuildingAddress = modelAddress;
                            }

                            modelAddress.Country = level10UI.Country;
                            modelAddress.Town = level10UI.Town;
                            modelAddress.Region = level10UI.Region;
                            modelAddress.PostalCode = level10UI.PostalCode;
                            if (!string.IsNullOrEmpty(level10UI.AddressLine1)) { modelAddress.AddressLines.Add(level10UI.AddressLine1); }
                            if (!string.IsNullOrEmpty(level10UI.AddressLine2)) { modelAddress.AddressLines.Add(level10UI.AddressLine2); }
                        }
                    }

                    if (this.level20s != null && this.level20s.Count > 0) 
                    {
                        foreach (var lvl20UI in level20s)
                        {
                            var site = model.Instances.FirstOrDefault<IIfcSite>(s => s.GlobalId == lvl20UI.ReferencedEntity!.GlobalId);
                            site.RefLatitude = lvl20UI.Latitude != null ? IfcCompoundPlaneAngleMeasure.FromDouble((double)lvl20UI.Latitude) : null;
                            site.RefLongitude = lvl20UI.Longitude != null ? IfcCompoundPlaneAngleMeasure.FromDouble((double)lvl20UI.Longitude) : null;
                            site.RefElevation = lvl20UI.Elevation != null ? lvl20UI.Elevation : null;
                        }
                    }

                    trans.Commit();

                }

                model.SaveAs(OutIfcPath);

            }
        }

        public UpdateViewModel()
        {

        }
    }
}
