using IFCGeorefShared;
using IFCGeorefShared.Levels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IFCGeoRefCheckerGUI.ValueConverters;

namespace IFCGeoRefCheckerGUI.ViewModels
{
    public class UpdateViewModel : BaseViewModel
    {
        public GeoRefChecker? geoRefChecker;
        
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
        public UpdateViewModel(GeoRefChecker geoRefChecker)
        {
            this.geoRefChecker = geoRefChecker;
            var Level10UIs = new List<Level10UI>(geoRefChecker.LoGeoRef10.Count);
            foreach (var lvl10 in geoRefChecker.LoGeoRef10)
            {
                Level10UIs.Add(LevelsConverter.convertToLevel10UI(lvl10));
            }

            this.Level10s = new ObservableCollection<Level10UI>(Level10UIs);
            //this.Level10s = new ObservableCollection<Level10>(geoRefChecker.LoGeoRef10);
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

        }

        public UpdateViewModel()
        {

        }
    }
}
