using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4x3.RepresentationResource;

namespace IFCGeorefShared.Levels
{
    public class Level50 : Level40
    {
        public IIfcMapConversion? MapConversion;
        public IfcRigidOperation? RigidOperation;
        public IfcProjectedCRS? ProjectedCRS4x3;

        public static string WriteLevelResult(GeoRefChecker checker, CultureInfo? culture = null)
        => WriteLevelResult<Level50>(checker, culture);
        protected override string Name => "LoGeoRef50";

        /// <summary>
        /// Checks each <see cref="Level50"/> context in the specified <see cref="GeoRefChecker"/> against GeoRef40 (<seealso href="https://github.com/dd-bim/City2BIM/wiki/Resources/GeoRefChecker/logeoref50.png"/>):
        /// </summary>
        /// <remarks>
        /// Checks the IFC model for:
        /// <list type="bullet"> 
        /// <item><description><see cref="IIfcGeometricRepresentationContext"/> has at least one <see cref="IIfcCoordinateOperation"/></description></item>
        /// <item><description>Type of  the <see cref="IIfcCoordinateOperation"/> is <see cref="IIfcMapConversion"/></description></item>
        /// <item><description>At least one of <see cref="IIfcMapConversion"/> components is unequal to zero</description></item>
        /// </list>
        /// </remarks>
        /// <param name="geoRefChecker">The <see cref="GeoRefChecker"/> instance containing the level contexts to be checked. Cannot be <see
        /// langword="null"/>.</param>
        public static new void CheckForLevel(GeoRefChecker geoRefChecker)
        {
            var levels = Level40.CreateLevelsFromContexts<Level50>(geoRefChecker, (proj, ctx) => new Level50());

            foreach (var lvl in levels)
            {
                foreach (var oper in lvl.context.HasCoordinateOperation)
                {
                    if (oper != null)
                    {
                        if (oper is IIfcMapConversion mapConv)
                        {
                            lvl.MapConversion = mapConv;
                            if (lvl.MapConversion.Eastings == 0.0 && lvl.MapConversion.Northings == 0.0)
                            {
                                Log.Warning("Translation Easting and Northing is 0. LoGeoRef50 is not fulfilled.");
                                continue;
                            }
                            lvl.IsFullFilled = true;
                            if (0.9 < mapConv.Scale && mapConv.Scale < 1.1)
                            {
                                Log.Warning("Scale of map conversion is between 0.9 and 1.1. This might not be used for conversion of units.");
                            }
                            if (mapConv.XAxisAbscissa != null || mapConv.XAxisOrdinate != null)
                            {
                                if (lvl.context.TrueNorth != null)
                                {
                                    var angleTrueNorth = Utils.AngleFromDirection(lvl.context.TrueNorth);
                                    var angleMapConv = Utils.AngleFromDirection((double)mapConv.XAxisOrdinate!, (double)mapConv.XAxisAbscissa!);
                                    Log.Warning("Ifc file contains both true north from the geometric representation context and a rotation angle from the map conversion");
                                    Log.Warning($"True north is: {angleTrueNorth}° and map conversion rotation is: {angleMapConv}°");
                                }
                            }
                        }
                        else if (oper is IfcRigidOperation rigidConv)
                        {
                            //TODO
                        }
                    }
                }
                geoRefChecker.LoGeoRef50.Add(lvl);
            }
        }

        public override string WriteInstanceResult(CultureInfo? culture = null)
        {
            var sb = new StringBuilder();
            var _translationService = GeoRefChecker.TranslationService;
            culture ??= CultureInfo.CurrentCulture;

            if (MapConversion != null) //und Eastings > 0 und Northing > 0 ? -> in LoGeoRef Spezifikation nochmal Voraussetzungen prüfen
            {
                sb.AppendLine($"{_translationService.Translate("MapConversionDefined", culture)} #{MapConversion.EntityLabel} for {context!.GetType().Name} (#{context.EntityLabel}) {_translationService.Translate("ContextType", culture)} {context.ContextType}");
                sb.AppendLine($"{_translationService.Translate("TransEast", culture)} {MapConversion.Eastings}");
                sb.AppendLine($"{_translationService.Translate("TransNorth", culture)} {MapConversion.Northings}");
                sb.AppendLine($"{_translationService.Translate("TransHeight", culture)} {MapConversion.OrthogonalHeight}");
                var RotationFromRefDirection = Utils.TrueNorthFromRefDirection((double)MapConversion.XAxisAbscissa, (double)MapConversion.XAxisOrdinate);
                sb.AppendLine($"{_translationService.Translate("TrueNorth", culture)} {RotationFromRefDirection.ToString("F1", culture)}° ({MapConversion.XAxisAbscissa} | {MapConversion.XAxisOrdinate})");
                if (context.TrueNorth != null)
                {
                    sb.AppendLine($"--> Ifc file contains both true north from the geometric representation context and a rotation angle from the map conversion\n True north is: {Utils.AngleFromDirection(context.TrueNorth).ToString("F1", culture)}° and map conversion rotation is: {RotationFromRefDirection.ToString("F1", culture)}°");
                }
                sb.AppendLine($"{_translationService.Translate("Scale", culture)} {MapConversion.Scale}");
                if (0.9 < MapConversion.Scale && MapConversion.Scale < 1.1)
                {
                    sb.Append($"--> {_translationService.Translate("ScaleMapConversion", culture)}");
                }

                sb.AppendLine();

                sb.AppendLine($"{_translationService.Translate("TargetCRS", culture)} {MapConversion.TargetCRS.Name}");
                sb.AppendLine($"{_translationService.Translate("Description", culture)} {(MapConversion.TargetCRS.Description.HasValue ? MapConversion.TargetCRS.Description : "not specified")}");
                sb.AppendLine($"{_translationService.Translate("GeoDatum", culture)} {(MapConversion.TargetCRS.GeodeticDatum.HasValue ? MapConversion.TargetCRS.GeodeticDatum : "not specified")}");

                if (ProjectedCRS4x3 != null)
                {
                    sb.AppendLine($"{_translationService.Translate("VertDatum", culture)} {(ProjectedCRS4x3.VerticalDatum.HasValue ? ProjectedCRS4x3.VerticalDatum : "not specified")}");
                }
            }
            else
            {
                sb.AppendLine($"{(context != null ? $"{_translationService.Translate("NoMapConvBy", culture)}{context.EntityLabel} IfcGeometricRepresentationContext {_translationService.Translate("ContextType", culture)} {context.ContextType}" : $"{_translationService.Translate("FoundNo", culture)}")}");
            }

            //Common section for all levels
            sb.AppendLine();
            sb.AppendLine($"{Name} {(IsFullFilled ? _translationService.Translate("Fulfilled", culture) : _translationService.Translate("NotFulfilled", culture))}");
            sb.AppendLine(Utils.dashLine);
            return sb.ToString();
        }
    }
}
