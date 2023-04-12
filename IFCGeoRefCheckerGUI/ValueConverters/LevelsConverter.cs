using IFCGeorefShared.Levels;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc4.Interfaces;

namespace IFCGeoRefCheckerGUI.ValueConverters
{
    class LevelsConverter
    {
        public static Level10UI convertToLevel10UI(Level10 lvl10)
        {
            var address = lvl10.PostalAddress;
            var lvl10UI = new Level10UI();

            if (address != null)
            {
                var nrOfAddresses = address.AddressLines.Count;

                lvl10UI.AddressLine1 = (nrOfAddresses > 0 && !string.IsNullOrEmpty(address.AddressLines[0])) ? address.AddressLines[0] : null;
                lvl10UI.AddressLine2 = (nrOfAddresses > 1 && !string.IsNullOrEmpty(address.AddressLines[1])) ? address.AddressLines[1] : null;
                lvl10UI.PostalCode = string.IsNullOrEmpty(address.PostalCode) ? null : address.PostalCode;
                lvl10UI.Country = string.IsNullOrEmpty(address.Country) ? null : address.Country;
                lvl10UI.Town = string.IsNullOrEmpty(address.Town) ? null : address.Town;
                lvl10UI.Region = string.IsNullOrEmpty(address.Region) ? null : address.Region;
            }

            lvl10UI.GUID = lvl10.ReferencedEntity!.GlobalId;
            lvl10UI.ReferencedEntity = lvl10.ReferencedEntity.GetType().Name;

            return lvl10UI;
        }

        public static Level20UI convertToLevel20UI(Level20 lvl20)
        {
            var lvl20UI = new Level20UI();

            lvl20UI.Latitude = lvl20.Latitude;
            lvl20UI.Longitude = lvl20.Longitude;
            lvl20UI.Elevation = lvl20.Elevation;
            lvl20UI.GUID = lvl20.ReferencedEntity?.GlobalId;

            return lvl20UI;
        }

        public static Level50UI convertToLevel50UI(Level50 lvl50)
        {
            var lvl50UI = new Level50UI();

            if (lvl50.MapConversion != null)
            {
                lvl50UI.Eastings = lvl50.MapConversion.Eastings;
                lvl50UI.Northings = lvl50.MapConversion.Northings;
                lvl50UI.Scale = lvl50.MapConversion.Scale;
                lvl50UI.XAxisAbscissa = lvl50.MapConversion.XAxisAbscissa;
                lvl50UI.XAxisOrdinate = lvl50.MapConversion.XAxisOrdinate;
                lvl50UI.OrhtogonalHeight = lvl50.MapConversion.OrthogonalHeight;

                lvl50UI.GeodeticDatum = lvl50.MapConversion.TargetCRS.GeodeticDatum;
                lvl50UI.VerticalDatum = lvl50.MapConversion.TargetCRS.VerticalDatum;
                lvl50UI.Name = lvl50.MapConversion.TargetCRS.Name;
            }

            return lvl50UI;
        }

    }

    
}
