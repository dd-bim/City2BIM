using IFCGeorefShared.Levels;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                lvl10UI.AddressLine1 = !(nrOfAddresses > 0 && string.IsNullOrEmpty(address.AddressLines[0])) ? null : address.AddressLines[0];
                lvl10UI.AddressLine2 = !(nrOfAddresses > 1 && string.IsNullOrEmpty(address.AddressLines[1])) ? null : address.AddressLines[1];
                lvl10UI.PostalCode = string.IsNullOrEmpty(address.PostalCode) ? null : address.PostalCode;
                lvl10UI.Country = string.IsNullOrEmpty(address.Country) ? null : address.Country;
                lvl10UI.Town = string.IsNullOrEmpty(address.Town) ? null : address.Town;
                lvl10UI.Region = string.IsNullOrEmpty(address.Region) ? null : address.Region;
            }

            lvl10UI.GUID = lvl10.ReferencedEntity!.GlobalId;
            lvl10UI.referencedEntity = lvl10.ReferencedEntity.GetType().Name;

            return lvl10UI;
        }
    }

    
}
