using System;
using System.Collections.Generic;
using System.Text;

namespace IFCGeorefShared.Levels
{
    internal class Level50 : Level00, IEquatable<Level50>
    {
        public bool GeoRef50 { get; set; }

        public IList<string> Reference_Object { get; set; } = new List<string>();

        public IList<string> Instance_Object { get; set; } = new List<string>();

        public double Translation_Eastings { get; set; }

        public double Translation_Northings { get; set; }

        public double Translation_Orth_Height { get; set; }

        public IList<double> RotationXY { get; set; } /*= new List<double>() { 0, 1 };*/

        public double Scale { get; set; }

        public string CRS_Name { get; set; } = "n/a";

        public string CRS_Description { get; set; } = "n/a";

        public string CRS_Geodetic_Datum { get; set; } = "n/a";

        public string CRS_Vertical_Datum { get; set; } = "n/a";

        public string CRS_Projection_Name { get; set; } = "n/a";

        public string CRS_Projection_Zone { get; set; } = "n/a";

        public bool Equals(Level50 other)
        {
            if (other == null)
                return false;
            if (Translation_Eastings == other.Translation_Eastings &&
                Translation_Northings == other.Translation_Northings &&
                Translation_Orth_Height == other.Translation_Orth_Height &&
                Scale == other.Scale &&
                string.Equals(CRS_Name, other.CRS_Name) &&
                string.Equals(CRS_Description, other.CRS_Description) &&
                string.Equals(CRS_Geodetic_Datum, other.CRS_Geodetic_Datum) &&
                string.Equals(CRS_Vertical_Datum, other.CRS_Vertical_Datum) &&
                string.Equals(CRS_Projection_Name, other.CRS_Projection_Name) &&
                string.Equals(CRS_Projection_Zone, other.CRS_Projection_Zone))
            {
                if (RotationXY == null && other.RotationXY == null)      //in beiden Rotation nicht gesetzt --> Rotation ist gleich (null) --> true
                {
                    return true;
                }
                else if (RotationXY == null || other.RotationXY == null) //in einem der beiden Rotation nicht gesetzt --> Rotation ist ungleich (null) --> false
                {
                    return false;
                }
                else if (RotationXY[0] == other.RotationXY[0] && RotationXY[1] == other.RotationXY[1])
                {                                                       //NULL-prüfung bestanden --> Untersuchung der Werte --> beide gleich --> true
                    return true;
                }
                else
                    return false;                                       //wenn Werte nicht gleich --> false
            }
            else
            {
                return false;
            }
        }
    }
}
