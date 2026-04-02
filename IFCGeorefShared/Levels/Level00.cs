using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;

namespace IFCGeorefShared.Levels
{
    public abstract class Level00
    {
        public bool IsFullFilled { get; set; }
        public IIfcProduct? ReferencedEntity { get; set; }

        protected virtual string Name => "LoGeoRef00";
        public abstract string WriteInstanceResult(CultureInfo? culture = null);

        /// <summary>
        /// If the level check is not fulfilled, this property can be used to provide a message describing the reason for the rejection.
        /// Value shall be set in the <see cref="ILevelChecker{T}.CheckForLevel"/> implementation of the respective level, and can be used in the <see cref="WriteInstanceResult"/> method to provide more detailed feedback on the check result.
        /// </summary>
        protected string RejectionMessage = "";

        public static string WriteLevelResult<T>(GeoRefChecker checker, CultureInfo? culture = null) where T : Level00
        {
            if (culture == null) culture = CultureInfo.CurrentCulture;
            var sb = new StringBuilder();
            var _translationService = GeoRefChecker.TranslationService;

            var result = $"{typeof(T).Name} {(checker.getCheckResult<T>() ? _translationService.Translate("Fulfilled", culture) : _translationService.Translate("NotFulfilled", culture))}";

            var result_list = checker.LoGeoRef<T>();
            sb.AppendLine($"{result} ({result_list.Count(x => x.IsFullFilled == true)}/{result_list.Count()})");
            sb.AppendLine(Utils.dashLine);

            foreach (var lvl in result_list)
            {
                sb.AppendLine(lvl.WriteInstanceResult(culture));
            }

            sb.AppendLine(Utils.starLine);
            return sb.ToString();
        }
    }
    public interface ILevelChecker<T> where T : Level00
    {
        static abstract void CheckForLevel(GeoRefChecker checker);
    }
}
