using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using GmlAttribute = City2BIM.GetSemantics.GmlAttribute;

namespace City2BIM.RevitBuilder
{
    internal class RevitSemanticBuilder
    {
        private Document doc;
        private DefinitionFile city2BimParameterFile;
        private string userDefinedParameterFile;

        public RevitSemanticBuilder(Document doc)
        {
            this.doc = doc;
            this.userDefinedParameterFile = doc.Application.SharedParametersFilename;

            // create shared parameter file
            String modulePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            String paramFile = modulePath + "\\City2BIM_Parameters.txt";
            if(File.Exists(paramFile))
            {
                File.Delete(paramFile);
            }
            FileStream fs = File.Create(paramFile);

            fs.Close();

            doc.Application.SharedParametersFilename = paramFile;

            this.city2BimParameterFile = doc.Application.OpenSharedParameterFile();
        }

        public void CreateProjectParameters(string paramGroupName, string[] attributes)
        {
            //Create groups in parFile if not existent
            DefinitionGroup parGroupGeoref = SetDefinitionGroupToParameterFile(paramGroupName);

            Category projInfoCat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_ProjectInformation);

            var parProjInfo = GetExistentCategoryParameters(projInfoCat);

            foreach(var attribute in attributes)
            {
                Definition paramDef = SetDefinitionsToGroup(parGroupGeoref, attribute, ParameterType.Text);

                CategorySet assocCats = doc.Application.Create.NewCategorySet();

                if(!parProjInfo.Contains(attribute))
                    assocCats.Insert(projInfoCat);

                if(!assocCats.IsEmpty)
                {
                    BindParameterDefinitionToCategories(paramDef, assocCats);
                }
            }

            //set SharedParameterFile back to user defined one (if applied)

            if(this.userDefinedParameterFile != null)
                doc.Application.SharedParametersFilename = this.userDefinedParameterFile;
        }

        public void CreateParameters(IEnumerable<GmlAttribute> attributes)
        {
            //Create groups in parFile if not existent
            DefinitionGroup parGroupCore = SetDefinitionGroupToParameterFile("CityGML-Core");
            DefinitionGroup parGroupGen = SetDefinitionGroupToParameterFile("CityGML-Generics");
            DefinitionGroup parGroupBldg = SetDefinitionGroupToParameterFile("CityGML-Building");
            DefinitionGroup parGroupAddr = SetDefinitionGroupToParameterFile("CityGML-Address");
            DefinitionGroup parGroupGML = SetDefinitionGroupToParameterFile("CityGML-GML");

            //creation of unique attribute list because attribute list can contain multiple attributes
            //with the same name but different category assignments
            var uniqueAttr = new Dictionary<Definition, List<GmlAttribute.AttrHierarchy>>();

            //loop over attributes

            var genCat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel);
            var wallCat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Walls);
            var roofCat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Roofs);
            var groundCat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_StructuralFoundation);
            var entCat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Entourage);

            var parClosure = GetExistentCategoryParameters(genCat);
            var parWall = GetExistentCategoryParameters(wallCat);
            var parRoof = GetExistentCategoryParameters(roofCat);
            var parGround = GetExistentCategoryParameters(groundCat);
            var parEnt = GetExistentCategoryParameters(entCat);

            var parBldg = parClosure.Union(parWall).Union(parRoof).Union(parGround).Union(parEnt);

            var groupedAttributes = attributes.GroupBy(r => (r.GmlNamespace + ": " + r.Name));

            foreach(var attributeGroup in groupedAttributes)
            {
                string paramName = attributeGroup.Key;

                GmlAttribute attribute = attributeGroup.First();

                var ab = attributeGroup.ToList();

                ParameterType pType = default(ParameterType);

                switch(attribute.GmlType)
                {
                    case (GmlAttribute.AttrType.intAttribute):
                        pType = ParameterType.Integer;
                        break;

                    case (GmlAttribute.AttrType.doubleAttribute):
                        pType = ParameterType.Number;
                        break;

                    case (GmlAttribute.AttrType.uriAttribute):
                        pType = ParameterType.URL;
                        break;

                    case (GmlAttribute.AttrType.measureAttribute):
                        pType = ParameterType.Length;
                        break;

                    case (GmlAttribute.AttrType.stringAttribute):
                        pType = ParameterType.Text;
                        break;

                    default:
                        break;
                }

                DefinitionGroup currentGroup = default(DefinitionGroup);

                switch(attribute.GmlNamespace)
                {
                    case (GmlAttribute.AttrNsp.gen):
                        currentGroup = parGroupGen;
                        break;

                    case (GmlAttribute.AttrNsp.core):
                        currentGroup = parGroupCore;
                        break;

                    case (GmlAttribute.AttrNsp.bldg):
                        currentGroup = parGroupBldg;
                        break;

                    case (GmlAttribute.AttrNsp.xal):
                        currentGroup = parGroupAddr;
                        break;

                    case (GmlAttribute.AttrNsp.gml):
                        currentGroup = parGroupGML;
                        break;
                }

                Definition paramDef = SetDefinitionsToGroup(currentGroup, paramName, pType);

                CategorySet assocCats = doc.Application.Create.NewCategorySet();

                var unAttr = attributeGroup.Select(a => a.Reference).ToList();

                if(unAttr.Contains(GmlAttribute.AttrHierarchy.bldg) ||
                    unAttr.Contains(GmlAttribute.AttrHierarchy.surface))
                {
                    if(!parBldg.Contains(attributeGroup.Key))
                    {
                        assocCats.Insert(entCat);
                        assocCats.Insert(wallCat);
                        assocCats.Insert(roofCat);
                        assocCats.Insert(groundCat);
                        assocCats.Insert(genCat);
                    }
                }

                if(unAttr.Contains(GmlAttribute.AttrHierarchy.wall))
                {
                    if(!parWall.Contains(attributeGroup.Key))
                        assocCats.Insert(wallCat);
                }
                if(unAttr.Contains(GmlAttribute.AttrHierarchy.roof))
                {
                    if(!parRoof.Contains(attributeGroup.Key))
                        assocCats.Insert(roofCat);
                }

                if(unAttr.Contains(GmlAttribute.AttrHierarchy.ground))
                {
                    if(!parGround.Contains(attributeGroup.Key))
                        assocCats.Insert(groundCat);
                }

                if(unAttr.Contains(GmlAttribute.AttrHierarchy.closure))
                {
                    if(!parClosure.Contains(attributeGroup.Key))
                        assocCats.Insert(genCat);
                }

                if(!assocCats.IsEmpty)
                {
                    BindParameterDefinitionToCategories(paramDef, assocCats);
                }
            }

            //set SharedParameterFile back to user defined one (if applied)

            if(this.userDefinedParameterFile != null)
                doc.Application.SharedParametersFilename = this.userDefinedParameterFile;
        }

        /// <summary>
        /// Transaction: Adds DefinitionGroup (SharedParameterFile) or get existing one
        /// </summary>
        /// <param name="groupName">Defined name of DefinitionGroup</param>
        /// <returns>DefinitionGroup</returns>
        private DefinitionGroup SetDefinitionGroupToParameterFile(string groupName)
        {
            //if group already exists in file - set:
            DefinitionGroup paramGroup = city2BimParameterFile.Groups.get_Item(groupName);

            //if not - create:
            if(paramGroup == null)
            {
                using(Transaction transDefGroup = new Transaction(doc, "Insert ParameterGroup"))
                {
                    transDefGroup.Start();
                    paramGroup = city2BimParameterFile.Groups.Create(groupName);
                    transDefGroup.Commit();
                }
            }
            return paramGroup;
        }

        /// <summary>
        /// Transaction: Adds Definition to DefinitionGroup (SharedParameterFile)
        /// </summary>
        /// <param name="parGroup">Group where Parameter should be inserted</param>
        /// <param name="paramName">Input data for parameter</param>
        /// <param name="pType">Data type for parameter</param>
        /// <returns>Parameter Definition</returns>
        private Definition SetDefinitionsToGroup(DefinitionGroup parGroup, string paramName, ParameterType pType)
        {
            //if definition already exists in file - set:
            Definition paramDef = parGroup.Definitions.get_Item(paramName);

            //if not - create:
            using(Transaction tParam = new Transaction(doc, "Insert Parameter"))
            {
                tParam.Start();
                ExternalDefinitionCreationOptions extDef = new ExternalDefinitionCreationOptions(paramName, pType);
                paramDef = parGroup.Definitions.Create(extDef);
                tParam.Commit();
            }
            return paramDef;
        }

        /// <summary>
        /// Transaction: Binds delivered parameter to associated categories
        /// </summary>
        /// <param name="paramDef">Parameter definition</param>
        /// <param name="associatedCategories">Categories where Parameter should apply</param>
        private void BindParameterDefinitionToCategories(Definition paramDef, CategorySet associatedCategories)
        {
            using(Transaction transBindToCat = new Transaction(doc, "Bind Parameter"))
            {
                transBindToCat.Start();

                //Create an instance of InstanceBinding
                InstanceBinding instanceBinding = doc.Application.Create.NewInstanceBinding(associatedCategories);

                doc.ParameterBindings.Insert(paramDef, instanceBinding, BuiltInParameterGroup.PG_DATA);  //Parameter-Gruppe Daten ok?

                transBindToCat.Commit();
            }
        }

        /// <summary>
        /// Reads Parameteres of delivered category
        /// </summary>
        /// <param name="currentCat">Category which should be invetigated</param>
        /// <returns>List of existent Parameters</returns>
        private List<string> GetExistentCategoryParameters(Category currentCat)
        {
            List<string> parList = new List<string>();

            var bindingMap = doc.ParameterBindings;

            var iterator = bindingMap.ForwardIterator();

            while(iterator.MoveNext())
            {
                var elementBinding = iterator.Current as ElementBinding;

                if(elementBinding.Categories.Contains(currentCat))
                {
                    var definiton = iterator.Key as Definition;

                    var parName = definiton.Name;

                    parList.Add(parName);
                }
            }
            return parList;
        }
    }
}