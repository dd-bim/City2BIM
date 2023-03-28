using System;
using System.Collections.Generic;
using System.Text;

namespace IFCGeorefShared.Levels
{
    public class Level20 : Level00, IEquatable<Level20>
    {
        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public double? Elevation { get; set; }

        public bool Equals(Level20? other)
        {
            if (other == null || this == null)
                return false;
            if (Latitude == other.Latitude &&
               Longitude == other.Longitude &&
               Elevation == other.Elevation)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
