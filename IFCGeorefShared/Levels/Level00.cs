using System;
using System.Collections.Generic;
using System.Text;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;

namespace IFCGeorefShared.Levels
{
    public abstract class Level00
    {
        public bool IsFullFilled { get; set; }
        public IIfcProduct? ReferencedEntity { get; set; }


        public string toString()
        {
            return "";
        }
    }
}
