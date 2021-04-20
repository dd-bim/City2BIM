using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed Xbmim
using Xbim.Ifc;
using Xbim.Ifc2x3.MeasureResource;                //IfcLabel

using Xbim.Ifc2x3.Kernel;                         //PropertySets
using Xbim.Ifc2x3.PropertyResource;               //PSetResources
using Xbim.Ifc2x3.ProductExtension;               //IfcSite (entity mapping)

//embed IFCTerrain - JsonSettings (input values)
using BIMGISInteropLibs.IfcTerrain;

namespace BIMGISInteropLibs.IFC.Ifc2x3
{
    public class PropertySet
    {
        /// <summary>
        /// TEST DUMMY for property sets
        /// </summary>
        /// <param name="model">exchange the whole ifc model</param>
        /// <param name="jSettings">json settings where meta data are stored (possible example)</param>
        public static void CreatePSetMetaDin91391(IfcStore model, JsonSettings_DIN_SPEC_91391_2 jSettings)
        {
            //create entity
            var entity = model.Instances.OfType<IfcSite>().FirstOrDefault();

            //create property
            var prop = model.Instances.New<IfcPropertySingleValue>(p =>
            {
                p.Name = "TestProperty";
                p.NominalValue = new IfcLabel("SamplePropertyValue");
            });

            //create property set
            var pSet = model.Instances.New<IfcPropertySet>(pset =>
            {
                //title of pset
                pset.Name = "Property Set - Example"; //ATTENTION: DON'T start with 'Pset_'!

                //assign the properties
                pset.HasProperties.Add(prop);
            });

            //add property set to site
            entity.AddPropertySet(pSet);

            return;
        }
    }
}
