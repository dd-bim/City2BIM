using System;
using System.Collections.Generic;
using System.Text;

namespace IFCGeorefShared.Levels
{
    internal class Level40 : Level00, IEquatable<Level40>
    {
        public bool GeoRef40 { get; set; }

        public IList<string> Reference_Object { get; set; } = new List<string>();

        public IList<string> Instance_Object { get; set; } = new List<string>();

        public IList<double> ProjectLocation { get; set; } = new List<double>();

        public IList<double> ProjectRotationX { get; set; } = new List<double>();

        public IList<double> ProjectRotationZ { get; set; } = new List<double>();

        public IList<double> TrueNorthXY { get; set; } = new List<double>();

        public bool Equals(Level40 other)
        {
            if (other == null)
                return false;
            if (ProjectLocation[0] == other.ProjectLocation[0] &&
                ProjectLocation[1] == other.ProjectLocation[1] &&
                ProjectLocation[2] == other.ProjectLocation[2] &&
                ProjectRotationX[0] == other.ProjectRotationX[0] &&
                ProjectRotationX[1] == other.ProjectRotationX[1] &&
                ProjectRotationX[2] == other.ProjectRotationX[2] &&
                ProjectRotationZ[0] == other.ProjectRotationZ[0] &&
                ProjectRotationZ[1] == other.ProjectRotationZ[1] &&
                ProjectRotationZ[2] == other.ProjectRotationZ[2] &&
                TrueNorthXY[0] == other.TrueNorthXY[0] &&
                TrueNorthXY[1] == other.TrueNorthXY[1])

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
