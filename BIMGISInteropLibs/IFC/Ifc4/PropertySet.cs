using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//embed Xbmim
using Xbim.Ifc;
using Xbim.Ifc4.MeasureResource;                //IfcLabel

using Xbim.Ifc4.Kernel;                         //PropertySets
using Xbim.Ifc4.PropertyResource;               //PSetResources
using Xbim.Ifc4.ProductExtension;               //IfcSite (entity mapping)

//embed IFCTerrain - JsonSettings (input values)
using BIMGISInteropLibs.IfcTerrain;

namespace BIMGISInteropLibs.IFC.Ifc4
{
    /// <summary>
    /// Class for creating property sets<para/>
    /// Used in IFCTerrain to assign metadata
    /// </summary>
    public static class PropertySet
    {
        /// <summary>
        /// TEST DUMMY for property sets
        /// </summary>
        /// <param name="model">exchange the whole ifc model</param>
        /// <param name="jSettings">json settings where meta data are stored (possible example)</param>
        /// <returns></returns>
        public static void CreatePSetMetaDin91391(IfcStore model, JsonSettings_DIN_SPEC_91391_2 jSettings)
        {
            //create entity
            var entity = model.Instances.OfType<IfcSite>().FirstOrDefault();
            
            /* create only one property
            //create property
            var prop = model.Instances.New<IfcPropertySingleValue>(p =>
            {
                p.Name = "TestProperty";
                p.NominalValue = new IfcLabel("SamplePropertyValue");
            });
            */

            //create property set
            var pSet = model.Instances.New<IfcPropertySet>(pset =>
            {
                //title of pset
                pset.Name = "Metadata DIN SPEC 91391-2"; //ATTENTION: DON'T start with 'Pset_'!

                //description
                pset.Description = "Storage of metadata according to DIN SPEC 91391-2";

                //add the properties
                pset.HasProperties.AddRange(new[] {
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "unique Identificator";
                        p.NominalValue = new IfcLabel(jSettings.id.ToString());
                    }),
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Length property";
                        p.NominalValue = new IfcLengthMeasure(56.0);
                    }),
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Number property";
                        p.NominalValue = new IfcNumericMeasure(789.2);
                    }),
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Logical property";
                        p.NominalValue = new IfcLogical(true);
                    })
                });
            });

            //add property set to site
            entity.AddPropertySet(pSet);

            return;
        }
    }
}
