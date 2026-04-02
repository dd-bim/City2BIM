using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using static System.FormattableString;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4x3.RepresentationResource;

namespace IFCGeorefShared.Levels
{
    public class Level40 : Level00, ILevel<Level40>
    {
        public IIfcGeometricRepresentationContext? context;
        public IIfcProject? project;
        public IIfcDirection? trueNorth;
        public IIfcPlacement? wcs;

        public static string WriteLevelResult(GeoRefChecker checker, CultureInfo? culture = null)
        => WriteLevelResult<Level40>(checker, culture);
        protected override string Name => "LoGeoRef40";

        /// <summary>
        /// Creates a collection of <see cref="Level40"/> / <see cref="Level50"/> instances from the geometric representation contexts of the
        /// specified IFC project.
        /// </summary>
        /// <remarks>This method checks the IFC model for:
        /// <list type="bullet"> 
        /// <item><description>Containing exactly one <see cref="IIfcProject"/></description></item>
        /// </list>
        /// If no project is found, or if the project is <c>null</c>, the factory is invoked with <c>null</c> arguments
        /// and the resulting instance is returned in a singleton collection. Only top-level geometric representation
        /// contexts are considered; sub-contexts are excluded.</remarks>
        /// <typeparam name="T">The type of <see cref="Level40"/> / <see cref="Level40"/> to create for each context.</typeparam>
        /// <param name="geoRefChecker">The <see cref="GeoRefChecker"/> containing the IFC model from which projects and contexts are retrieved.</param>
        /// <param name="factory">A factory function that creates an instance of type <typeparamref name="T"/> from an <see
        /// cref="IIfcProject"/> and an <see cref="IIfcGeometricRepresentationContext"/>.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing one <typeparamref name="T"/> instance for each geometric
        /// representation context found in the IFC project.</returns>
        internal static IEnumerable<T> CreateLevelsFromContexts<T>(GeoRefChecker geoRefChecker, Func<IIfcProject?, IIfcGeometricRepresentationContext?, T> factory) where T : Level40
        {
            var list = new List<T>();

            var projects = geoRefChecker.Model.Instances.OfType<IIfcProject>().ToList();

            if (projects.Count != 1)
            {
                Log.Information("Ifc file does not contain an IfcProject. Invalid file!");
                return list;
            }

            var proj = projects.FirstOrDefault();

            if (proj == null)
            {
                var lvl = factory(null, null);
                lvl.IsFullFilled = false;
                lvl.project = null;
                list.Add(lvl);
                return list;
            }

            var allCtx = proj.RepresentationContexts.OfType<IIfcGeometricRepresentationContext>();  //includes also inherited SubContexts (not necessary for this application)
            var noSubCtx = allCtx.Where(ctx => ctx.ExpressType.ToString() != "IfcGeometricRepresentationSubContext").ToList(); //avoid subs (unneccessary overhead)

            foreach (var context in noSubCtx)
            {
                var lvl = factory(proj, context);
                lvl.IsFullFilled = false;
                lvl.project = proj;
                lvl.context = context;
                lvl.trueNorth = context.TrueNorth;
                list.Add(lvl);
            }

            return list;
        }

        /// <summary>
        /// Checks each <see cref="Level40"/> context in the specified <see cref="GeoRefChecker"/> against GeoRef40 (<seealso href="https://github.com/dd-bim/City2BIM/wiki/Resources/GeoRefChecker/logeoref40.png"/>):
        /// </summary>
        /// <remarks>
        /// Checks the IFC model for:
        /// <list type="bullet"> 
        /// <item><description><see cref="IIfcGeometricRepresentationContext.WorldCoordinateSystem"/> is of type <see cref="IIfcAxis2Placement3D"/></description></item>
        /// <item><description>At least one of Locations (<see cref="IIfcCartesianPoint"/>) components is greather than zero</description></item>
        /// </list>
        /// </remarks>
        /// <param name="geoRefChecker">The <see cref="GeoRefChecker"/> instance containing the level contexts to be checked. Cannot be <see
        /// langword="null"/>.</param>
        public static void CheckForLevel(GeoRefChecker geoRefChecker)
        {
            var levels = CreateLevelsFromContexts(geoRefChecker, (proj, ctx) => new Level40());
            foreach (var lvl in levels)
            {
                var wcsPlcmt = lvl.context?.WorldCoordinateSystem;
                if (wcsPlcmt != null && wcsPlcmt is IIfcAxis2Placement3D wcs)
                {
                    lvl.wcs = wcs;
                    var location = wcs.Location;

                    if (location.X > 0 || location.Y > 0 || location.Z > 0)
                    {
                        lvl.IsFullFilled = true;
                    }
                    else
                    {
                        lvl.RejectionMessage = $"All locations (#{wcs.Location.EntityLabel}) components are zero. Not valid!";
                        continue;
                    }
                }
                else if (wcsPlcmt != null && wcsPlcmt is IIfcAxis2Placement2D wcs2D)
                {

                    lvl.wcs = wcs2D;
                    var location = wcs2D.Location;

                    if (location.X > 0 || location.Y > 0)
                    {
                        lvl.IsFullFilled = true;
                        Log.Warning($"#{wcsPlcmt.EntityLabel} is a {wcsPlcmt.GetType().Name}, expected IIfcAxis2Placement3D");
                    }
                    else
                    {
                        lvl.RejectionMessage = $"All locations (#{wcs2D.Location.EntityLabel}) components are zero. Not valid!";
                        continue;
                    }  
                }
                geoRefChecker.LoGeoRef40.Add(lvl);
            }
        }

        public override string WriteInstanceResult(CultureInfo? culture = null)
        {
            var sb = new StringBuilder();
            var _translationService = GeoRefChecker.TranslationService;
            culture ??= CultureInfo.CurrentCulture;

            if (IsFullFilled)
            {
                if (context != null)
                {
                    sb.AppendLine($"IfcProject (#{project!.EntityLabel}, {project!.GlobalId}) {_translationService.Translate("Reference", culture)} IfcGeometricRepresentationContext (#{context.EntityLabel}) {_translationService.Translate("ContextType", culture)} {context.ContextType}");

                    sb.AppendLine(IsFullFilled ? $"{_translationService.Translate("ParametersWCS", culture)}:" : $"{_translationService.Translate("NoWCSGeoref", culture)}:");
                    sb.AppendLine($"{_translationService.Translate("LocationCoordinates", culture)}:");
                    sb.AppendLine($"X: {wcs!.Location.X}");
                    sb.AppendLine($"Y: {wcs.Location.Y}");
                    sb.AppendLine($"Z: {wcs.Location.Z}");
                    sb.AppendLine();

                    if (trueNorth != null)
                    {
                        sb.AppendLine(Invariant($"{_translationService.Translate("TrueNorth", culture)} {Utils.AngleFromDirection(trueNorth).ToString("F1", culture)}° ({trueNorth.X} | {trueNorth.Y})"));
                    }
                    else
                    {
                        sb.AppendLine($"{_translationService.Translate("NoTrueNorth", culture)} 0° (0 | 1)");
                    }
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
