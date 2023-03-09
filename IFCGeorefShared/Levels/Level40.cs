using System;
using System.Collections.Generic;
using System.Text;

using Xbim.Ifc4.Interfaces;

namespace IFCGeorefShared.Levels
{
    internal class Level40
    {
        public bool IsFullFilled { get; set; }
        public IIfcGeometricRepresentationContext? context;
        public IIfcProject? project;
        public IIfcDirection? trueNorth;
        public IIfcAxis2Placement3D? wcs;
    }
}
