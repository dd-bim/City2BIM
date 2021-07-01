using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BIMGISInteropLibs.IfcTerrain;

namespace GuiHandler
{
    public class InitClass
    {
        /// <summary>
        /// instance for settings (getter + setter) to convert files
        /// </summary>
        public static JsonSettings config = new JsonSettings();

        /// <summary>
        /// create an instance for JSON Settings (getter + setter) <para/>
        /// mainly used to export metadata acording to DIN SPEC 91391-2 <para/>
        /// </summary>
        public static JsonSettings_DIN_SPEC_91391_2 config91391 = new JsonSettings_DIN_SPEC_91391_2();

        /// <summary>
        /// create an instance for JSON Settings (getter + setter) <para/>
        /// mainly used to export metadata acording to DIN SPEC 18740-6 <para/>
        /// </summary>
        public static JsonSettings_DIN_18740_6 config18740 = new JsonSettings_DIN_18740_6();
    
        /// <summary>
        /// create NEW instance for settings (to override the settings before)
        /// </summary>
        /// <returns></returns>
        public static JsonSettings clearConfig()
        {
            GuiSupport.resetTasks();
            JsonSettings config = new JsonSettings();
            return config;
        }
    }
}
