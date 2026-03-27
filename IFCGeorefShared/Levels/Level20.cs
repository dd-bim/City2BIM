using OSGeo.OGR;
using Serilog;
using System;
using System.Collections.Generic;
using static System.FormattableString;
using System.Globalization;
using System.Linq;
using System.Text;
using Xbim.Ifc4.Interfaces;

namespace IFCGeorefShared.Levels
{
    public class Level20 : Level00, IEquatable<Level20>, ILevelChecker<Level20>
    {
        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public double? Elevation { get; set; }

        public string? GeographicDescription { get; set; }

        public static string WriteLevelResult(GeoRefChecker checker, CultureInfo? culture = null)
        => WriteLevelResult<Level20>(checker, culture);
        protected override string Name => "LoGeoRef20";

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

        /// <summary>
        /// Checks each <see cref="Level20"/> context in the specified <see cref="GeoRefChecker"/> against GeoRef40 (<seealso href="https://github.com/dd-bim/City2BIM/wiki/Resources/GeoRefChecker/logeoref20.png"/>):
        /// </summary>
        /// <remarks>
        /// Checks the IFC model for:
        /// <list type="bullet"> 
        /// <item><description><see cref="IIfcSite"/> has valid values for <see cref="IIfcSite.RefLatitude"/> and <see cref="IIfcSite.RefLongitude"/></description></item>
        /// <item><description>Warning if <see cref="IIfcSite.RefElevation"/> is <see langword="null"/></description></item>
        /// </list>
        /// </remarks>
        /// <param name="geoRefChecker">The <see cref="GeoRefChecker"/> instance containing the level contexts to be checked. Cannot be <see
        /// langword="null"/>.</param>
        public static void CheckForLevel(GeoRefChecker geoRefChecker)
        {
            var Sites = geoRefChecker.Model.Instances.OfType<IIfcSite>().ToList();

            foreach (var site in Sites)
            {
                var lvl20 = new Level20();

                if (site.RefLatitude.HasValue && site.RefLongitude.HasValue)
                {
                    lvl20.Latitude = site.RefLatitude.Value.AsDouble;
                    lvl20.Longitude = site.RefLongitude.Value.AsDouble;
                    lvl20.Elevation = site.RefElevation.HasValue ? site.RefElevation.Value : null;
                    lvl20.IsFullFilled = true;
                    if (!(-90 < lvl20.Latitude && lvl20.Latitude < 90))
                    {
                        Log.Error($"Latitude not in range of -90 - 90 Degree. Latitude is: {lvl20.Latitude}");
                        lvl20.IsFullFilled = false;
                    }
                    if (!(-180 < lvl20.Longitude && lvl20.Longitude < 180))
                    {
                        Log.Error($"Longitude not in range of -180 - 180 Degree. Longitude is: {lvl20.Longitude}");
                        lvl20.IsFullFilled = false;
                    }

                    if (lvl20.Elevation == 0)
                    {
                        Log.Warning($"Elevation might not be properly specified");
                    }
                }
                else
                {
                    lvl20.IsFullFilled = false;
                }
                lvl20.ReferencedEntity = site;
                geoRefChecker.LoGeoRef20.Add(lvl20);

            }
        }

        public override string WriteInstanceResult(CultureInfo? culture = null)
        {
            var sb = new StringBuilder();
            var _translationService = GeoRefChecker.TranslationService;
            culture ??= CultureInfo.CurrentCulture;

            sb.AppendLine($"{_translationService.Translate("GeographicLocation", culture)}{ReferencedEntity!.EntityLabel} {ReferencedEntity!.GetType().Name} {_translationService.Translate("With", culture)} GUID {ReferencedEntity.GlobalId}");
            sb.AppendLine(Invariant($"Latitude: {(Latitude != null ? Latitude : _translationService.Translate("NotSpecified", culture))} \t\tLongitude: {(Longitude != null ? Longitude : _translationService.Translate("NotSpecified", culture))}"));
            sb.AppendLine(Invariant($"Elevation: {(Elevation != null ? Elevation : _translationService.Translate("NotSpecified", culture))}"));
            _ = GeographicDescription != null ? sb.AppendLine($"{_translationService.Translate("AccordingCoordinates", culture)} {GeographicDescription}") : null;

            //Common section for all levels
            sb.AppendLine();
            sb.AppendLine($"{Name} {(IsFullFilled ? _translationService.Translate("Fulfilled", culture) : _translationService.Translate("NotFulfilled", culture))}");
            sb.AppendLine(Utils.dashLine);
            return sb.ToString();
        }

        public static void CheckGeoLocation(IList<Level20> lvl20s)
        {
            var hasResults = lvl20s.Any(x => x.IsFullFilled);
            if (!hasResults) return;

            using (DataSource ds = Ogr.Open(Settings.GetSettings().RegionPath, 0))
            {
                if (ds == null)
                {
                    Log.Error("Reading of region file failed! \nCanceling operation");
                    return;
                }

                var countries = ds.GetLayerByName("ne_10m_admin_1_states_provinces");
                Feature? lastFoundFeature = null;
                var feature = countries.GetNextFeature();
                var nameIdx = feature.GetFieldIndex("name");
                var adminIdx = feature.GetFieldIndex("admin");

                foreach (var lvl20 in lvl20s)
                {
                    if (!lvl20.Latitude.HasValue || !lvl20.Longitude.HasValue)
                    {
                        Log.Information($"IfcSite {lvl20.ReferencedEntity!.GlobalId} has either no latitude or longitude");
                        continue;
                    }

                    countries.ResetReading();

                    var pointToTest = new OSGeo.OGR.Geometry(wkbGeometryType.wkbPoint);
                    pointToTest.AddPoint_2D((double)lvl20.Longitude, (double)lvl20.Latitude);

                    if (lastFoundFeature != null)
                    {
                        if (pointToTest.Within(lastFoundFeature.GetGeometryRef()))
                        {
                            lvl20.GeographicDescription = $"Site is located in {lastFoundFeature.GetFieldAsString(adminIdx)} in Region {lastFoundFeature.GetFieldAsString(nameIdx)}";
                            //lvl20.GeographicDescription = $"{_translationService.Translate("SiteLocated", CultureInfo.CurrentCulture)} {lastFoundFeature.GetFieldAsString(adminIdx)} in {_translationService.Translate("Region", CultureInfo.CurrentCulture)} {lastFoundFeature.GetFieldAsString(nameIdx)}";
                            continue;
                        }
                    }

                    while (feature != null)
                    {
                        if (pointToTest.Within(feature.GetGeometryRef()))
                        {
                            lastFoundFeature = feature;
                            lvl20.GeographicDescription = $"Site is located in {feature.GetFieldAsString(adminIdx)} in Region {feature.GetFieldAsString(nameIdx)}";
                            //lvl20.GeographicDescription = $"{_translationService.Translate("SiteLocated", CultureInfo.CurrentCulture)} {feature.GetFieldAsString(adminIdx)} in {_translationService.Translate("Region", CultureInfo.CurrentCulture)} {feature.GetFieldAsString(nameIdx)}";
                        }
                        feature = countries.GetNextFeature();
                    }
                }
            }
        }
    }
}
