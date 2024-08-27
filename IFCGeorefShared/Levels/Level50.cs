using System;
using System.Collections.Generic;
using System.Text;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4x3.RepresentationResource;

namespace IFCGeorefShared.Levels
{
    public class Level50 : Level40
    {
        public IIfcMapConversion? MapConversion;
        public IfcProjectedCRS? ProjectedCRS4x3;
    }
}
