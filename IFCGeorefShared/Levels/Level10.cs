using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Text;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;

namespace IFCGeorefShared.Levels
{
    public class Level10 : Level00, ILevel<Level10>
    {

        public IIfcPostalAddress? PostalAddress { get; set; }

        public static string WriteLevelResult(GeoRefChecker checker, CultureInfo? culture = null)
        => WriteLevelResult<Level10>(checker, culture);

        protected override string Name => "LoGeoRef10";
        /// <summary>
        /// Checks each <see cref="Level10"/> context in the specified <see cref="GeoRefChecker"/> against GeoRef40 (<seealso href="https://github.com/dd-bim/City2BIM/wiki/Resources/GeoRefChecker/logeoref10.png"/>):
        /// </summary>
        /// <remarks>
        /// Checks the IFC model for:
        /// <list type="bullet"> 
        /// <item><description>Checks if <see cref="IIfcPostalAddress"/> is not <see langword="null"/></description></item>
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
                IIfcPostalAddress address = null!;
                if (entity is IIfcSite)
                {
                    address = ((IIfcSite)entity).SiteAddress;
                }
                else if (entity is IIfcBuilding)
                {
                    address = ((IIfcBuilding)entity).BuildingAddress;
                }

                var level10 = new Level10();
                level10.IsFullFilled = (address != null);
                level10.PostalAddress = address;
                level10.ReferencedEntity = entity;

                geoRefChecker.LoGeoRef10.Add(level10);
            }
        }


        public override string WriteInstanceResult(CultureInfo? culture = null)
        {
            var sb = new StringBuilder();
            var _translationService = GeoRefChecker.TranslationService;
            culture ??= CultureInfo.CurrentCulture;
            if (IsFullFilled)
            {
                string header = $"{_translationService.Translate("PostalAddress", culture)}{ReferencedEntity!.EntityLabel} {ReferencedEntity!.GetType().Name} {_translationService.Translate("With", culture)} GUID {ReferencedEntity.GlobalId}";
                //string header = $"Postal address referenced by Entity #{lvl10.ReferencedEntity!.EntityLabel} {lvl10.ReferencedEntity!.GetType().Name} with GUID {lvl10.ReferencedEntity.GlobalId}";
                sb.AppendLine(header);

                string info = $"{_translationService.Translate("Country", culture)}: {(PostalAddress!.Country != "" ? PostalAddress!.Country : _translationService.Translate("NotSpecified", culture))} \t\t{_translationService.Translate("Region", culture)}: {(PostalAddress!.Region != "" ? PostalAddress!.Region : _translationService.Translate("NotSpecified", culture))}";
                info += $"\n{_translationService.Translate("Town", culture)}: {(PostalAddress!.Town != "" ? PostalAddress!.Town : _translationService.Translate("NotSpecified", culture))} \t\t{_translationService.Translate("PostalCode", culture)}: {(PostalAddress!.PostalCode != "" ? PostalAddress!.PostalCode : _translationService.Translate("NotSpecified", culture))}";
                foreach (var line in PostalAddress.AddressLines)
                {
                    info += $"\n{_translationService.Translate("Address", culture)}: {(line.ToString() != "" ? line.ToString() : _translationService.Translate("NotSpecified", culture))}";
                }
                sb.AppendLine(info);
            }
            else
            {
                sb.AppendLine($"{_translationService.Translate("NoPostalAddress", culture)}{ReferencedEntity!.EntityLabel} {ReferencedEntity!.GetType().Name} {_translationService.Translate("With", culture)} GUID {ReferencedEntity.GlobalId}");
            }

            //Common section for all levels
            sb.AppendLine();
            sb.AppendLine($"{Name} {(IsFullFilled ? _translationService.Translate("Fulfilled", culture) : _translationService.Translate("NotFulfilled", culture))}");
            sb.AppendLine(Utils.dashLine);
            return sb.ToString();
        }

    }
}
