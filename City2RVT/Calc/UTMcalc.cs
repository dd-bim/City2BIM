using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using NETGeographicLib;

namespace City2RVT.Calc
{
    public static class UTMcalc
    {
        public static readonly double UtmScale = 0.9996;
        public static readonly double UtmFalseEasting = 500000.0;
        public static readonly double UtmFalseNorthing = 10000000.0;

        public static readonly Ellipsoid Grs80 = new Ellipsoid(NETGeographicLib.Constants.GRS80.MajorRadius, NETGeographicLib.Constants.GRS80.Flattening);

        private static readonly TransverseMercator utmGrs80 = new TransverseMercator(Grs80.MajorRadius, Grs80.Flattening, UtmScale);

        private static readonly string egm2008Path =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace("\\", "/");

        private static readonly GravityModel egm2008 = new GravityModel("egm2008", egm2008Path);

        public static readonly double Rho = 180.0 / Math.PI;

        public static readonly double RevRho = Math.PI / 180.0;

        public static double AzimuthToRange(double azi) => ((azi % 360.0) + 360) % 360;

        public static int ZoneToLon0(int zone) => (6 * zone) - 183;

        public static int LonToZone(double lon) => ((int)Math.Floor(lon) + 186) / 6;

        public static double DegToRad(double deg) => deg * RevRho;

        public static double RadToDeg(double rad) => rad * Rho;

        public static string DegToString(double degreeAngle, bool dms)
        {
            if(dms)
            {
                var deg = (int)degreeAngle; //integer part
                var min = (int)((degreeAngle - deg) * 60);
                var sec = (int)((((degreeAngle - deg) * 60) - min) * 60);
                var frac = (int)((((((degreeAngle - deg) * 60) - min) * 60) - sec) * 1e6);
                return $"{deg,4:+###;-###;0}° {Math.Abs(min):00}\' {Math.Abs(sec):00}\" {Math.Abs(frac):000000}";
            }
            return degreeAngle.ToString("+0.00000000;-0.00000000;0", CultureInfo.InvariantCulture);
        }

        public static double StringToDeg(string value, bool dms)
        {
            var deg = double.NaN;
            if(dms)
            {
                value = value.Replace('°', ' ');
                value = value.Replace('\'', ' ');
                value = value.Replace('\"', ' ');
                value = value.Replace(',', ' ');
                value = value.Replace('.', ' ');
                var split = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if(split.Length > 0 && int.TryParse(split[0], out var x))
                {
                    deg = x;
                    var sig = (double)Math.Sign(x);
                    if(split.Length > 1 && int.TryParse(split[1], out x))
                    {
                        deg += sig * x / 60.0;
                        if(split.Length > 2 && int.TryParse(split[2], out x))
                        {
                            deg += sig * x / 3600.0;
                            if(split.Length > 3 && int.TryParse(split[3], out x))
                            {
                                var dig = split[3].Length;
                                var dx = sig * x / 3600.0 / Math.Pow(10, dig);
                                deg += dx;
                            }
                        }
                    }

                }
            }
            else
            {
                value = value.Replace(',', '.');
                if(double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                {
                    deg = x;
                }
            }
            return deg;

        }

        public static double ParseDouble(string value)
        {
            value = value.Replace(',', '.');
            if(double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
            {
                return x;
            }
            return double.NaN;
        }

        public static double[] AzimuthToVector(double azi)
        {
            var razi = DegToRad(azi);
            return new[] { Math.Cos(razi), Math.Sin(razi) };
        }

        public static double[] AzimuthToLocalVector(double azi)
        {
            var razi = DegToRad(90 - azi);
            return new[] { Math.Cos(razi), Math.Sin(razi) };
        }

        public static double VectorToAzimuth(double x, double y)
        {
            return AzimuthToRange(RadToDeg(Math.Atan2(y, x)));
        }

        public static double LocalVectorToAzimuth(double x, double y)
        {
            return AzimuthToRange(90 - RadToDeg(Math.Atan2(y, x)));
        }

        public static int ParseInt(string value)
        {
            if(int.TryParse(value, out var x))
            {
                return x;
            }
            return int.MaxValue;
        }

        public static string DoubleToString(double value, int dec)
        {
            var fmt = "f" + dec;
            return value.ToString(fmt, CultureInfo.InvariantCulture);
        }

        public static void UtmGrs80Forward(int zone, double lat, double lon, out double easting, out double northing, out double gamma, out double scale, out bool isSouth)
        {
            var lon0 = ZoneToLon0(zone);
            utmGrs80.Forward(lon0, lat, lon, out easting, out northing, out gamma, out scale);
            easting += UtmFalseEasting;
            isSouth = false;
            if(northing < 0.0)
            {
                northing += UtmFalseNorthing;
                isSouth = true;
            }
        }

        public static void UtmGrs80Reverse(int zone, bool isSouth, double easting, double northing, out double lat, out double lon, out double gamma, out double scale)
        {
            var lon0 = ZoneToLon0(zone);
            utmGrs80.Reverse(lon0, easting - UtmFalseEasting, isSouth ? northing - UtmFalseNorthing : northing, out lat,
                out lon, out gamma, out scale);
        }

        public static double GeoidHeight(double lat, double lon) => egm2008.GeoidHeight(lat, lon);

        public static double GaussianRadiusOfCurvature(double lat)
        {
            var sin = Math.Sin(DegToRad(lat));
            return Grs80.MinorRadius / (1.0 - (Grs80.EccentricitySq * sin * sin));
        }

        public static void GetGeoRef(bool isPosGeo, ref double lat, ref double lon, ref int? zone, ref double east, ref double north, ref bool isSouth,
            double orthoHeight, bool isRotGeo, ref double geoAzi, ref double gridAzi, out double combinedScale)
        {
            double convergence, projScale;
            if(isPosGeo)
            {
                zone = zone ?? LonToZone(lon);
                UtmGrs80Forward(zone.Value, lat, lon, out east, out north, out convergence, out projScale, out isSouth);
            }
            else
            {
                zone = zone ?? int.MaxValue;
                UtmGrs80Reverse(zone.Value, isSouth, east, north, out lat, out lon, out convergence, out projScale);
            }

            var radius = GaussianRadiusOfCurvature(lat);
            var geoidHeight = GeoidHeight(lat, lon);
            var heightScale = radius / (radius + orthoHeight + geoidHeight);

            combinedScale = projScale * heightScale;

            if(isRotGeo)
            {
                geoAzi = AzimuthToRange(geoAzi);
                gridAzi = AzimuthToRange(geoAzi - convergence);
            }
            else
            {
                gridAzi = AzimuthToRange(gridAzi);
                geoAzi = AzimuthToRange(gridAzi + convergence);
            }
        }
    }
}