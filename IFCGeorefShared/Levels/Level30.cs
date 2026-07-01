using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.RepresentationResource;
using static System.FormattableString;

namespace IFCGeorefShared.Levels
{

    public class Level30 : Level00, ILevel<Level30>
    {
        public IIfcPlacement? plcmt;

        public static string WriteLevelResult(GeoRefChecker checker, CultureInfo? culture = null)
        => WriteLevelResult<Level30>(checker, culture);
        protected override string Name => "LoGeoRef30";
        protected override HashSet<Type> AllowedConversions => new HashSet<Type> { typeof(Level40) };

        /// <summary>
        /// Checks each <see cref="Level30"/> context in the specified <see cref="GeoRefChecker"/> against GeoRef40 (<seealso href="https://github.com/dd-bim/City2BIM/wiki/Resources/GeoRefChecker/logeoref30.png"/>):
        /// </summary>
        /// <remarks>
        /// Checks the IFC model for:
        /// <list type="bullet"> 
        /// <item><description><see cref="IIfcSpatialStructureElement.ObjectPlacement"/> is of type <see cref="IIfcLocalPlacement"/></description></item>
        /// <item><description><see cref="IIfcLocalPlacement.PlacementRelTo"/> is <see langword="null"/></description></item>
        /// <item><description>At least one of Locations (<see cref="IIfcCartesianPoint"/>) components is greather than zero</description></item>
        /// </list>
        /// </remarks>
        /// <param name="geoRefChecker">The <see cref="GeoRefChecker"/> instance containing the level contexts to be checked. Cannot be <see
        /// langword="null"/>.</param>
        public static void CheckForLevel(GeoRefChecker geoRefChecker)
        {
            var BuildingsAndSites = new IIfcSpatialStructureElement[0]
                .Concat(geoRefChecker.Model.Instances.OfType<IIfcSite>())
                .Concat(geoRefChecker.Model.Instances.OfType<IIfcBuilding>()).ToList();
            foreach (var entity in BuildingsAndSites)
            {
                var localPlcm = (IIfcLocalPlacement)entity.ObjectPlacement;
                if (localPlcm == null) continue;
                var lvl = new Level30();
                lvl.ReferencedEntity = entity;

                if (localPlcm.PlacementRelTo == null)
                {

                    lvl.plcmt = (IIfcPlacement)localPlcm.RelativePlacement;

                    var location = lvl.plcmt.Location;
                    if (location.X > 0.0 || location.Y > 0.0 || location.Z > 0.0)
                    {
                        lvl.IsFullFilled = true;
                    }
                    else
                    { 
                        lvl.RejectionMessage = $"All locations (#{lvl.plcmt.Location.EntityLabel}) components are zero. Not valid!"; 
                    }
                }
                else
                {
                    lvl.RejectionMessage = $"IfcLocalPlacement #{localPlcm.EntityLabel} is relative to #{localPlcm.PlacementRelTo.EntityLabel}. Not valid!";
                }
                geoRefChecker.LoGeoRef30.Add(lvl);
            }
        }

        public override bool ConvertToLevel(Type targetLevelType, GeoRefChecker checker)
        {
            if (!base.ConvertToLevel(targetLevelType, checker)) return false;

            if (targetLevelType == typeof(Level40))
            {
                if (checker.getCheckResult<Level40>()) return false; //no need to convert if target level already fulfilled

                var newLevel40 = new Level40
                {
                    IsFullFilled = true,
                    project = checker.Model.Instances.OfType<IIfcProject>().FirstOrDefault(),
                    wcs = plcmt
                };
                if (newLevel40.project == null)
                {
                    Log.Warning("No project found for Level40 conversion. Cannot convert!");
                    return false;
                }
                var model = checker.Model;
                using (var txn = model.BeginTransaction("Create GeometricRepresentationContext [LoGeoRef40]"))
                {
                    IfcDirection? trueNorth = null;
                    if (plcmt is IIfcAxis2Placement3D plcmt3D)
                    {
                        trueNorth = Utils.TrueNorthFromRefDirection<IfcDirection>(model, plcmt3D.RefDirection);
                    }
                    else if (plcmt is IIfcAxis2Placement2D plcmt2D)
                    {
                        trueNorth = Utils.TrueNorthFromRefDirection<IfcDirection>(model, plcmt2D.RefDirection);
                    }
                    newLevel40.context = model.Instances.New<IfcGeometricRepresentationContext>(ctx =>
                    {
                        ctx.CoordinateSpaceDimension = 3;
                        ctx.ContextType = "Model";
                        ctx.ContextIdentifier = "LoGeoRef40";
                        ctx.WorldCoordinateSystem = (IfcAxis2Placement)plcmt;
                        if (trueNorth is not null) ctx.TrueNorth = trueNorth;
                    });
                    model.Instances.OfType<IIfcProject>().First().RepresentationContexts.Add(newLevel40.context);
                    txn.Commit();
                }
                checker.LoGeoRef40.Add(newLevel40);
                return true;
            }
            throw new NotImplementedException($"Conversion from {GetType().Name} to {targetLevelType.Name} is not implemented.");
        }

        public override string WriteInstanceResult(CultureInfo? culture = null)
        {
            var sb = new StringBuilder();
            var _translationService = GeoRefChecker.TranslationService;
            culture ??= CultureInfo.CurrentCulture;

            sb.AppendLine($"{_translationService.Translate("UpperEntity", culture)}: #{ReferencedEntity!.EntityLabel} {ReferencedEntity!.GetType().Name} {_translationService.Translate("With", culture)} GUID: {ReferencedEntity.GlobalId}");
            //sb.AppendLine(IsFullFilled ? $"{_translationService.Translate("GeographicContext", culture)}" : $"{_translationService.Translate("NoGeographicContext", culture)}");
            if (IsFullFilled)
            {
                sb.AppendLine(Invariant($"{_translationService.Translate("LocationCoordinates", culture)}:\nX: {plcmt.Location.X} \nY: {plcmt.Location.Y} \nZ: {plcmt.Location.Z}"));

                if (plcmt is IIfcAxis2Placement3D plcmt3D)
                {
                    sb.AppendLine(Invariant($"{_translationService.Translate("TrueNorth", culture)} {(plcmt3D.RefDirection == null ? _translationService.Translate("NotSpecified", culture) : Utils.TrueNorthFromRefDirection(plcmt3D.RefDirection).ToString("F1", culture) + "°")}"));
                    sb.AppendLine(Invariant($"{_translationService.Translate("DirectionX", culture)} {(plcmt3D.RefDirection == null ? "(1 | 0 | 0)" : $"({plcmt3D.RefDirection.X} | {plcmt3D.RefDirection.Y} | {plcmt3D.RefDirection.Z})")}"));
                    sb.AppendLine(Invariant($"{_translationService.Translate("DirectionZ", culture)} {(plcmt3D.Axis == null ? "(0 | 0 | 1)" : $"({plcmt3D.Axis.X} | {plcmt3D.Axis.Y} | {plcmt3D.Axis.Z}")})"));
                }
                else if (plcmt is IIfcAxis2Placement2D plcmt2D)
                {
                    sb.AppendLine(Invariant($"{_translationService.Translate("TrueNorth", culture)}: {(plcmt2D.RefDirection == null ? _translationService.Translate("NotSpecified", culture) : Utils.TrueNorthFromRefDirection(plcmt2D.RefDirection).ToString("F1", culture) + "°")}"));
                    sb.AppendLine(Invariant($"{_translationService.Translate("DirectionX", culture)}  ({plcmt2D.RefDirection.X} | {plcmt2D.RefDirection.Y})"));
                }
            }
            else
            {
                sb.AppendLine(RejectionMessage);
            }

            //Common section for all levels
            sb.AppendLine();
            sb.AppendLine($"{Name} {(IsFullFilled ? _translationService.Translate("Fulfilled", culture) : _translationService.Translate("NotFulfilled", culture))}");
            sb.AppendLine(Utils.dashLine);
            return sb.ToString();
        }
    }
}
