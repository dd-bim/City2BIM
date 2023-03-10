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

        public UpdateViewModel(GeoRefChecker geoRefChecker)
        {
            this.geoRefChecker = geoRefChecker;
            var level10UIs = new List<Level10UI>(geoRefChecker.LoGeoRef10.Count);
            foreach (var lvl10 in geoRefChecker.LoGeoRef10)
            {
                level10UIs.Add(LevelsConverter.convertToLevel10UI(lvl10));
            }
            
            this.Level10s = new ObservableCollection<Level10UI>(level10UIs);
            this.Level20s = new ObservableCollection<Level20>(geoRefChecker.LoGeoRef20);
        }

        public UpdateViewModel()
        {

        }
    }
}
