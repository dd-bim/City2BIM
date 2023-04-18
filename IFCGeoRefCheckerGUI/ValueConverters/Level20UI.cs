using IFCGeoRefCheckerGUI.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCGeoRefCheckerGUI.ValueConverters
{
    public class Level20UI : BaseViewModel
    {
        public double? Latitude { get; set; }
        private double? longitude;
        public double? Longitude
        {
            get => longitude;
            set
            {
                if (longitude != value)
                {
                    ValHelper.ClearError();
                    
                    if (value < -180.0 || value > 180.0)
                    {
                        ValHelper.AddError("Longitude must be between -180.0 and 180.0 degrees");
                    }
                    longitude = value;
                    this.RaisePropertyChanged(nameof(Longitude));
                    
                }
            }
        }
        public double? Elevation { get; set; }

        public string? GUID { get; set; }

        public ValidationHelper ValHelper { get; } = new ValidationHelper();
        public bool HasErrors => ValHelper.HasErrors;

    }
}
