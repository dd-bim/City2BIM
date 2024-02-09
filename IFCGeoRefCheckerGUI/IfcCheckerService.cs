using System;
using System.Resources;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xbim.Ifc;

using IFCGeorefShared;


namespace IFCGeoRefCheckerGUI
{    
    class IfcCheckerService
    {
        public GeoRefChecker CheckIFC(string filePath) 
        {
            using (var model = IfcStore.Open(filePath))
            {
                var checker = new GeoRefChecker(model);
                return checker;
            }
        }
    }
}
