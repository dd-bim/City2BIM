using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using Xml_AttrRep = City2BIM.Semantic.Xml_AttrRep;

namespace City2RVT.Builder
{
    internal class Revit_Semantic
    {
        private readonly Document doc;
        private readonly DefinitionFile city2BimParameterFile;
        private readonly string userDefinedParameterFile;

        public Revit_Semantic(Document doc)
        {
            this.doc = doc;
            this.userDefinedParameterFile = doc.Application.SharedParametersFilename;

            //create shared parameter file
            string modulePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "City2BIM");
            string paramFile = Path.Combine(modulePath, "City2BIM_Parameters.txt");
            //System.Windows.Forms.MessageBox.Show(paramFile);

            if (!File.Exists(paramFile))
            {
                FileStream fs = File.Create(paramFile);
                fs.Close();
            }
            doc.Application.SharedParametersFilename = paramFile;
            this.city2BimParameterFile = doc.Application.OpenSharedParameterFile();
        }


        public void CreateProjectParameters(string paramGroupName, Dictionary<string, Xml_AttrRep.AttrType> attributes)
        {
            //Create groups in parFile if not existent
            DefinitionGroup parGroupGeoref = SetDefinitionGroupToParameterFile(paramGroupName);

            Category projInfoCat = GetCategory(BuiltInCategory.OST_ProjectInformation);

            List<Definition> parProjInfoDef = GetExistentCategoryParametersDef(projInfoCat);
            var selectedParams = GUI.Prop_NAS_settings.SelectedParams;


            var parProjInfo = parProjInfoDef.Select(p => p.Name);   //needed name for comparison

            foreach (var attribute in attributes)
            {
                if (selectedParams.Contains(attribute.Key))
                {
                    Definition paramDef = SetDefinitionsToGroup(parGroupGeoref, attribute.Key, GetParameterType(attribute.Value));

                    CategorySet assocCats = doc.Application.Create.NewCategorySet();

                    if (!parProjInfo.Contains(attribute.Key))
                        assocCats.Insert(projInfoCat);

                    if (!assocCats.IsEmpty)
                    {
                        BindParameterDefinitionToCategories(paramDef, assocCats);
                    }
                }                
            }
            //set SharedParameterFile back to user defined one (if applied)

            if (this.userDefinedParameterFile != null)
                doc.Application.SharedParametersFilename = this.userDefinedParameterFile;
        }

        public void CreateProjectInformationParameter(List<string> projInfoList, Autodesk.Revit.ApplicationServices.Application app, CategorySet projCatSet, BuiltInParameterGroup builtInParameterGroup)
        {
            DefinitionFile defFile = default;
            GUI.XPlan2BIM.XPlan_Parameter parameter = new GUI.XPlan2BIM.XPlan_Parameter();
            string paramFile = doc.Application.SharedParametersFilename;

            foreach (var p in projInfoList)
            {
                defFile = parameter.CreateDefinitionFile(paramFile, app, doc, p, "ProjectInformation");
            }

            foreach (DefinitionGroup dg in defFile.Groups)
            {
                foreach (var projInfoName in projInfoList)
                {
                    if (dg.Name == "ProjectInformation")
                    {
                        ExternalDefinition externalDefinition = dg.Definitions.get_Item(projInfoName) as ExternalDefinition;

                        Transaction tProjectInfo = new Transaction(doc, "Insert Project Information");
                        {
                            tProjectInfo.Start();
                            InstanceBinding newIB = app.Create.NewInstanceBinding(projCatSet);
                            if (externalDefinition != null)
                            {
                                doc.ParameterBindings.Insert(externalDefinition, newIB, builtInParameterGroup);
                            }
                            //logger.Info("Applied Parameters to '" + projInfoName + "'. ");
                        }
                        tProjectInfo.Commit();
                    }
                }
            }
        }

        /// <summary>
        /// Creates Project Information for the revit project where the data is imported to, like general data or postal address 
        /// </summary>
        public void CreateProjectInformation(Autodesk.Revit.ApplicationServices.Application app, Document doc, CategorySet projCategorySet)
        {
            List<string> projectInformationList = new List<string>();
            projectInformationList.Add("Bezeichnung des Bauvorhabens");
            projectInformationList.Add("Art der Massnahme");
            projectInformationList.Add("Art des Gebaeudes");
            projectInformationList.Add("Gebaeudeklasse");
            projectInformationList.Add("Bauweise");

            CreateProjectInformationParameter(projectInformationList, app, projCategorySet, BuiltInParameterGroup.PG_GENERAL);

            List<string> projectAddressList = new List<string>();
            projectAddressList.Add("Address Line");
            projectAddressList.Add("Postal Code");
            projectAddressList.Add("Town");
            projectAddressList.Add("Region");
            projectAddressList.Add("Country");

            CreateProjectInformationParameter(projectAddressList, app, projCategorySet, BuiltInParameterGroup.PG_DATA);

            List<string> crsList = new List<string>();
            crsList.Add("GeodeticDatum");
            crsList.Add("VerticalDatum");
            crsList.Add("MapProjection");
            crsList.Add("MapZone");

            CreateProjectInformationParameter(crsList, app, projCategorySet, BuiltInParameterGroup.PG_ANALYTICAL_MODEL);
        }

        private ParameterType GetParameterType(Xml_AttrRep.AttrType gmlType)
        {
            ParameterType pType = default(ParameterType);

            switch (gmlType)
            {
                case (Xml_AttrRep.AttrType.intAttribute):
                    pType = ParameterType.Integer;
                    break;

                case (Xml_AttrRep.AttrType.doubleAttribute):
                    pType = ParameterType.Number;
                    break;

                case (Xml_AttrRep.AttrType.uriAttribute):
                    pType = ParameterType.URL;
                    break;

                case (Xml_AttrRep.AttrType.measureAttribute):
                    pType = ParameterType.Length;
                    break;

                case (Xml_AttrRep.AttrType.stringAttribute):
                    pType = ParameterType.Text;
                    break;

                case (Xml_AttrRep.AttrType.areaAttribute):
                    pType = ParameterType.Area;
                    break;

                case (Xml_AttrRep.AttrType.boolAttribute):
                    pType = ParameterType.YesNo;
                    break;

                default:
                    break;
            }
            return pType;
        }

        private Category GetCategory(BuiltInCategory builInCat)
        {
            return doc.Settings.Categories.get_Item(builInCat);
        }

        private CategorySet GetCategorySet(List<BuiltInCategory> builtInCats)
        {
            CategorySet catSet = new CategorySet();

            foreach (BuiltInCategory builtInCat in builtInCats)
            {
                catSet.Insert(GetCategory(builtInCat));
            }

            return catSet;
        }

        public void CreateParameters(IEnumerable<Xml_AttrRep> attributes)
        {
            //Create groups in parFile if not existent
            DefinitionGroup parGroupCore = SetDefinitionGroupToParameterFile("CityGML-Core");
            DefinitionGroup parGroupGen = SetDefinitionGroupToParameterFile("CityGML-Generics");
            DefinitionGroup parGroupBldg = SetDefinitionGroupToParameterFile("CityGML-Building");
            DefinitionGroup parGroupAddr = SetDefinitionGroupToParameterFile("CityGML-Address");
            DefinitionGroup parGroupGML = SetDefinitionGroupToParameterFile("CityGML-GML");
            DefinitionGroup parGroupAlkis = SetDefinitionGroupToParameterFile("ALKIS-Parcel");

            //creation of unique attribute list because attribute list can contain multiple attributes
            //with the same name but different category assignments
            var uniqueAttr = new Dictionary<Definition, List<Xml_AttrRep.AttrHierarchy>>();

            //loop over attributes

            var genCat = GetCategory(BuiltInCategory.OST_GenericModel);
            var wallCat = GetCategory(BuiltInCategory.OST_Walls);
            var roofCat = GetCategory(BuiltInCategory.OST_Roofs);
            var groundCat = GetCategory(BuiltInCategory.OST_StructuralFoundation);
            var entCat = GetCategory(BuiltInCategory.OST_Entourage);
            var topoCat = GetCategory(BuiltInCategory.OST_Topography);

            var parClosure = GetExistentCategoryParametersDef(genCat);
            var parWall = GetExistentCategoryParametersDef(wallCat);
            var parRoof = GetExistentCategoryParametersDef(roofCat);
            var parGround = GetExistentCategoryParametersDef(groundCat);
            var parEnt = GetExistentCategoryParametersDef(entCat);
            var parTopo = GetExistentCategoryParametersDef(topoCat);

            var parBldg = parClosure.Union(parWall).Union(parRoof).Union(parGround).Union(parEnt);


            var groupedAttributes = attributes.GroupBy(r => (r.XmlNamespace + ": " + r.Name));

            foreach (var attributeGroup in groupedAttributes)
            {
                string paramName = attributeGroup.Key;

                Xml_AttrRep attribute = attributeGroup.First();

                DefinitionGroup currentGroup = default(DefinitionGroup);

                switch (attribute.XmlNamespace)
                {
                    case (Xml_AttrRep.AttrNsp.alkis):
                        currentGroup = parGroupAlkis;
                        break;

                    case (Xml_AttrRep.AttrNsp.gen):
                        currentGroup = parGroupGen;
                        break;

                    case (Xml_AttrRep.AttrNsp.core):
                        currentGroup = parGroupCore;
                        break;

                    case (Xml_AttrRep.AttrNsp.bldg):
                        currentGroup = parGroupBldg;
                        break;

                    case (Xml_AttrRep.AttrNsp.xal):
                        currentGroup = parGroupAddr;
                        break;

                    case (Xml_AttrRep.AttrNsp.gml):
                        currentGroup = parGroupGML;
                        break;
                }

                Definition paramDef = SetDefinitionsToGroup(currentGroup, paramName, GetParameterType(attribute.XmlType));

                CategorySet assocCats = doc.Application.Create.NewCategorySet();

                var unAttr = attributeGroup.Select(a => a.Reference).ToList();

                if (unAttr.Contains(Xml_AttrRep.AttrHierarchy.parcel))
                {
                    if (!parTopo.Select(n => n.Name).Contains(attributeGroup.Key))
                        assocCats.Insert(topoCat);
                }

                if (unAttr.Contains(Xml_AttrRep.AttrHierarchy.bldgCity) ||
                    unAttr.Contains(Xml_AttrRep.AttrHierarchy.surface))
                {
                    if (!parBldg.Select(n => n.Name).Contains(attributeGroup.Key))
                    {
                        assocCats.Insert(entCat);
                        assocCats.Insert(wallCat);
                        assocCats.Insert(roofCat);
                        assocCats.Insert(groundCat);
                        assocCats.Insert(genCat);
                    }
                }

                if (unAttr.Contains(Xml_AttrRep.AttrHierarchy.wall))
                {
                    if (!parWall.Select(n => n.Name).Contains(attributeGroup.Key))
                        assocCats.Insert(wallCat);
                }
                if (unAttr.Contains(Xml_AttrRep.AttrHierarchy.roof))
                {
                    if (!parRoof.Select(n => n.Name).Contains(attributeGroup.Key))
                        assocCats.Insert(roofCat);
                }

                if (unAttr.Contains(Xml_AttrRep.AttrHierarchy.ground))
                {
                    if (!parGround.Select(n => n.Name).Contains(attributeGroup.Key))
                        assocCats.Insert(groundCat);
                }

                if (unAttr.Contains(Xml_AttrRep.AttrHierarchy.closure))
                {
                    if (!parClosure.Select(n => n.Name).Contains(attributeGroup.Key))
                        assocCats.Insert(genCat);
                }

                if (!assocCats.IsEmpty)
                {
                    var selectedParams = GUI.Prop_NAS_settings.SelectedParams;

                    if (selectedParams != null)
                    {
                        if (selectedParams.Contains(paramName.Substring(paramName.LastIndexOf(':') + 2)))
                        {
                            BindParameterDefinitionToCategories(paramDef, assocCats);
                        }
                    }                        
                    else
                    {
                        BindParameterDefinitionToCategories(paramDef, assocCats);
                    }
                }
            }

            //set SharedParameterFile back to user defined one (if applied)

            if (this.userDefinedParameterFile != null)
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
            if (paramGroup == null)
            {
                using (Transaction transDefGroup = new Transaction(doc, "Insert ParameterGroup"))
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
            if (paramDef == null)
            {
                using (Transaction tParam = new Transaction(doc, "Insert Parameter"))
                {
                    tParam.Start();
                    ExternalDefinitionCreationOptions extDef = new ExternalDefinitionCreationOptions(paramName, pType);
                    paramDef = parGroup.Definitions.Create(extDef);
                    tParam.Commit();
                }
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
            using (Transaction transBindToCat = new Transaction(doc, "Bind Parameter"))
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
        private List<Definition> GetExistentCategoryParametersDef(Category currentCat)
        {
            List<Definition> parList = new List<Definition>();

            var bindingMap = doc.ParameterBindings;

            var iterator = bindingMap.ForwardIterator();

            while (iterator.MoveNext())
            {
                var elementBinding = iterator.Current as ElementBinding;

                if (elementBinding.Categories.Contains(currentCat))
                {
                    var definiton = iterator.Key as Definition;

                    parList.Add(definiton);
                }
            }
            return parList;
        }

        //Following method was used to create ParameterSet File for mapping to IFC
        //needs revision for later direct IFC export from PlugIn (currently only for naming of MapConversion and ProjectedCRS)

        public void CreateParameterSetFile()
        {
            using (CategorySet usedCategories = GetCategorySet(
                new List<BuiltInCategory>(){
            BuiltInCategory.OST_ProjectInformation//,
            //BuiltInCategory.OST_Walls,
            //BuiltInCategory.OST_Roofs,
            //BuiltInCategory.OST_StructuralFoundation,
            //BuiltInCategory.OST_Entourage,
            //BuiltInCategory.OST_GenericModel 
                }))
            {
                string modulePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "City2BIM");
                string paramSetFile = Path.Combine(modulePath, "City2BIM_ParameterSet.txt");

                string tab = "\t", pset = "PropertySet:", type = "T", newLine = Environment.NewLine;

                using (FileStream fs = File.Open(paramSetFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    foreach (Category category in usedCategories)
                    {
                        string elementType = "";

                        switch (category.Id.IntegerValue)
                        {
                            case (int)BuiltInCategory.OST_ProjectInformation:
                                elementType = "IfcProject";
                                break;

                            //case (int)BuiltInCategory.OST_GenericModel:
                            //    elementType = "IfcBuildingElementProxy";
                            //    break;

                            //case (int)BuiltInCategory.OST_Walls:
                            //    elementType = "IfcWall";
                            //    break;

                            //case (int)BuiltInCategory.OST_Roofs:
                            //    elementType = "IfcRoof";
                            //    break;

                            //case (int)BuiltInCategory.OST_StructuralFoundation:
                            //    elementType = "IfcSlab";
                            //    break;

                            //case (int)BuiltInCategory.OST_Entourage:
                            //    elementType = "IfcBuildingElementProxy";
                            //    break;
                        }

                        var defAtCat = GetExistentCategoryParametersDef(category);

                        foreach (DefinitionGroup defGr in city2BimParameterFile.Groups)
                        {
                            AddText(fs, newLine + pset + tab + defGr.Name + tab + type + tab + elementType);

                            foreach (Definition def in defGr.Definitions)
                            {
                                if (defAtCat.Select(s => s.Name).Contains(def.Name))
                                {
                                    string ifcParType = def.ParameterType.ToString();

                                    if (ifcParType.Equals("Number"))
                                        ifcParType = "Real";

                                    AddText(fs, newLine + tab + def.Name + tab + ifcParType);
                                }
                            }
                        }

                    }
                }
            }
        }

        private static void AddText(FileStream fs, string value)
        {
            byte[] info = new UTF8Encoding(true).GetBytes(value);
            fs.Write(info, 0, info.Length);
        }

        /// <summary>
        /// Provides namespaces for XML and GML. 
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <returns></returns>
        public XmlNamespaceManager GetNamespaces(XmlDocument xmlDoc)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("ns2", "http://www.adv-online.de/namespaces/adv/gid/6.0");
            nsmgr.AddNamespace("gml", "http://www.opengis.net/gml/3.2");
            nsmgr.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
            nsmgr.AddNamespace("xplan", "http://www.xplanung.de/xplangml/5/2");

            return nsmgr;
        }
    }
}