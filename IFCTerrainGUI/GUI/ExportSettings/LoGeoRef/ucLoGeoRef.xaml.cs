using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;

namespace IFCTerrainGUI.GUI.ExportSettings.LoGeoRef
{
    /// <summary>
    /// Interaktionslogik für ucLoGeoRef.xaml
    /// </summary>
    public partial class ucLoGeoRef : UserControl
    {
        public ucLoGeoRef()
        {
            InitializeComponent();
        }

        //query epsg code
        private void btnEpsgQuery_Click(object sender, RoutedEventArgs e)
        {
            //get data context
            var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;

            //try to parse input of textbox to epsg code
            int.TryParse(tbEpsg.Text, out int epsgCode);

            //send request
            var projCRS = BIMGISInteropLibs.ProjCRS.Request.get(epsgCode, out bool isValid);

            if (isValid)
            {
                //set crs description
                config.crsDescription = projCRS.Name;
                
                //split for getting name and zone
                string[] projection = projCRS.Name.Split('/');

                //projection name
                config.projectionName = projection[0];

                //projection zone
                config.projectionZone = projection[1].Remove(0, 1);

                //get code
                int code = projCRS.BaseCoordRefSystem.Code;

                //send request for geodetic coord ref
                var geoCRS = BIMGISInteropLibs.GeodeticCRS.GeodeticCRS.get(code);

                //geodetic datum
                config.geodeticDatum = geoCRS.Datum.Name;

                //get geoCRS EPSG Code
                code = geoCRS.Datum.Code;

                //send datum request
                var datum = BIMGISInteropLibs.Datum.Datum.get(code);

                //vertical datum (need other request)
                config.verticalDatum = datum.Ellipsoid.Name;
            }
            else
            {
                //TODO --> user feedback

                //set crs name to null
                config.crsName = null;
            }

            
        }

        /// <summary>
        /// open window to show & edit metadata
        /// </summary>
        private void btnEditMetadata_Click(object sender, RoutedEventArgs e)
        {
            //create new instance of metadata window
            LoGeoRef50_CRS_Metadata windowCrsMetadata = new LoGeoRef50_CRS_Metadata();

            //set data context
            windowCrsMetadata.DataContext = DataContext;

            //open window
            windowCrsMetadata.ShowDialog();
        }
    }


    #region error handling GUI
    /// <summary>
    /// error handling to check if textbox has values
    /// </summary>
    public class HasAllTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool res = true;

            foreach (object val in values)
            {
                if (string.IsNullOrEmpty(val as string))
                {
                    res = false;
                }
            }

            return res;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class textRule : ValidationRule
    {
        public textRule()
        {

        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (string.IsNullOrEmpty((string)value))
            {
                return new ValidationResult(false, $"Please enter a number!");
            }
            return ValidationResult.ValidResult;
        }
    }
    /// <summary>
    /// class to validate scale input
    /// </summary>
    public class tbRule : ValidationRule
    {
        public string validationName { get; set; }
        public double min { get; set; }
        public double max { get; set; }

        public tbRule()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double scale = 0;

            try
            {
                if (((string)value).Length > 0)
                    scale = double.Parse((String)value);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, $"Illegal characters or {e.Message}");
            }

            if ((scale < min) || (scale > max))
            {
                return new ValidationResult(false,
                  $"Please enter a {validationName} in the range: {min}-{max}.");
            }
            return ValidationResult.ValidResult;
        }
    }
    #endregion error handling GUI
}
