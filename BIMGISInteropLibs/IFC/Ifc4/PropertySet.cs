﻿using System;
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
        /// An ifcPopertySet for DTM metadata according to DIN91391 is created and added to the ifcSite entity
        /// </summary>
        /// <param name="model">exchange the whole ifc model</param>
        /// <param name="jSettings">json settings where meta data are stored</param>
        /// <returns></returns>
        public static void CreatePSetMetaDin91391(IfcStore model, JsonSettings_DIN_SPEC_91391_2 jSettings)
        {
            //create entity
            var entity = model.Instances.OfType<IfcSite>().FirstOrDefault();

            #region Examples
            /* 
            //create only one property
            var prop = model.Instances.New<IfcPropertySingleValue>(p =>
            {
                p.Name = "TestProperty";
                p.NominalValue = new IfcLabel("SamplePropertyValue");
            });
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
            */
            #endregion Examples

            //create property set
            var pSet = model.Instances.New<IfcPropertySet>(pset =>
            {
                //title of pset
                pset.Name = "Metadata DIN SPEC 91391-2";

                //description
                pset.Description = "Storage of metadata according to DIN SPEC 91391-2";

                //read out properties from object jSettings and add them to the property set
                pset.HasProperties.AddRange(new[] {
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Unique Identificator";
                        p.NominalValue = new IfcLabel(jSettings.id.ToString());
                    }),
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Description";
                        if (!string.IsNullOrEmpty(jSettings.description))
                        {
                            p.NominalValue = new IfcText(jSettings.description);
                        }
                    }),
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Creator";
                        if (!string.IsNullOrEmpty(jSettings.creator))
                        {
                            p.NominalValue = new IfcText(jSettings.creator);
                        }
                    }),
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Revision";
                        if (!string.IsNullOrEmpty(jSettings.revision))
                        {
                            p.NominalValue = new IfcText(jSettings.revision);
                        }
                    }),
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Version";
                        if (!string.IsNullOrEmpty(jSettings.version))
                        {
                            p.NominalValue = new IfcText(jSettings.version);
                        }
                    }),
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Project ID";
                        if (!string.IsNullOrEmpty(jSettings.projectId))
                        {
                            p.NominalValue = new IfcText(jSettings.projectId);
                        }
                    }),
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Meta scheme";
                        if (!string.IsNullOrEmpty(jSettings.metaScheme))
                        {
                            p.NominalValue = new IfcText(jSettings.metaScheme);
                        }
                    }),
                });
            });

            //add property set to site
            entity.AddPropertySet(pSet);
            
            return;
        }

        /// <summary>
        /// An ifcPopertySet for DTM metadata according to DIN18740 is created and added to the ifcSite entity
        /// </summary>
        /// <param name="model">exchange the whole ifc model</param>
        /// <param name="jSettings">json settings where meta data are stored</param>
        public static void CreatePSetMetaDin18740(IfcStore model, JsonSettings_DIN_18740_6 jSettings)
        {
            //create entity
            var entity = model.Instances.OfType<IfcSite>().FirstOrDefault();

            //create property set
            var pSet = model.Instances.New<IfcPropertySet>(pset =>
            {
                //title of pset
                pset.Name = "Metadata DIN 18740-6";

                //description
                pset.Description = "Storage of metadata according to DIN 18740-6";

                //read out properties from object jSettings and add them to the property set
                pset.HasProperties.AddRange(new[] {
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Model type";
                        if (!string.IsNullOrEmpty(jSettings.modelType))
                        {
                            p.NominalValue = new IfcText(jSettings.modelType);
                        }
                    }),
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Datastructur";
                        if (!string.IsNullOrEmpty(jSettings.dataStructure))
                        {
                            p.NominalValue = new IfcText(jSettings.dataStructure);
                        }
                    }),
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Topicality";
                        if (!string.IsNullOrEmpty(jSettings.topicality))
                        {
                            p.NominalValue = new IfcText(jSettings.topicality);
                        }
                    }),
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Positon reference system";
                        if (!string.IsNullOrEmpty(jSettings.positionReferenceSystem))
                        {
                            p.NominalValue = new IfcText(jSettings.positionReferenceSystem);
                        }
                    }),
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Altitude reference system";
                        if (!string.IsNullOrEmpty(jSettings.altitudeReferenceSystem))
                        {
                            p.NominalValue = new IfcText(jSettings.altitudeReferenceSystem);
                        }
                    }),
                    model.Instances.New<IfcPropertySingleValue>(p =>
                    {
                        p.Name = "Projection";
                        if (!string.IsNullOrEmpty(jSettings.projection))
                        {
                            p.NominalValue = new IfcText(jSettings.projection);
                        }
                    }),
                });
            });

            //add property set to site
            entity.AddPropertySet(pSet);

            return;
        }
    }
}
