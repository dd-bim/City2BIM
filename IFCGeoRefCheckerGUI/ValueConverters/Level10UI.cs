using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc4.Interfaces;

namespace IFCGeoRefCheckerGUI.ValueConverters
{
    public class Level10UI
    {
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Town { get; set; }

        public string? GUID { get; set; }
        public string? referencedEntity { get; set; }

        
    }


}
