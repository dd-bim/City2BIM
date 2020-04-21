using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using System.IO;
using System.Reflection;
using Xml_AttrRep = City2BIM.Semantic.Xml_AttrRep;

namespace City2RVT.Builder
{
    public class XPlan_Semantics
    {
        //private readonly Document doc;
        //private readonly DefinitionFile sharedParameterFile;

        public void CreateProjectInformation(string paramFile, Autodesk.Revit.ApplicationServices.Application app, Document doc, CategorySet projCategorySet, City2RVT.GUI.XPlan2BIM.XPlan_Parameter parameter, DefinitionFile defFile)
        {
            List<string> projectInformationList = new List<string>();
            projectInformationList.Add("Bezeichnung des Bauvorhabens");
            projectInformationList.Add("Art der Maßnahme");
            projectInformationList.Add("Art des Gebäudes");
            projectInformationList.Add("Gebäudeklasse");
            projectInformationList.Add("Bauweise");

            foreach (var p in projectInformationList)
            {
                defFile = parameter.CreateDefinitionFile(paramFile, app, doc, p, "ProjectInformation");
            }

            foreach (DefinitionGroup dg in defFile.Groups)
            {
                foreach (var projInfoName in projectInformationList)
                {
                    if (dg.Name == "ProjectInformation")
                    {
                        ExternalDefinition externalDefinition = dg.Definitions.get_Item(projInfoName) as ExternalDefinition;

                        Transaction tProjectInfo = new Transaction(doc, "Insert Project Information");
                        {
                            tProjectInfo.Start();
                            InstanceBinding newIB = app.Create.NewInstanceBinding(projCategorySet);
                            if (externalDefinition != null)
                            {
                                doc.ParameterBindings.Insert(externalDefinition, newIB, BuiltInParameterGroup.PG_GENERAL);
                            }
                            //logger.Info("Applied Parameters to '" + projInfoName + "'. ");
                        }
                        tProjectInfo.Commit();
                    }
                }
            }

            List<string> projectAddressList = new List<string>();
            projectAddressList.Add("Address Line");
            projectAddressList.Add("Postal Code");
            projectAddressList.Add("Town");
            projectAddressList.Add("Region");
            projectAddressList.Add("Country");

            foreach (var a in projectAddressList)
            {
                defFile = parameter.CreateDefinitionFile(paramFile, app, doc, a, "IfcSiteAddress");
            }

            foreach (DefinitionGroup dg in defFile.Groups)
            {
                foreach (var projInfoName in projectAddressList)
                {
                    if (dg.Name == "IfcSiteAddress")
                    {
                        ExternalDefinition externalDefinition = dg.Definitions.get_Item(projInfoName) as ExternalDefinition;

                        Transaction tProjectInfo = new Transaction(doc, "Insert IfcSiteAddress");
                        {
                            tProjectInfo.Start();
                            InstanceBinding newIB = app.Create.NewInstanceBinding(projCategorySet);
                            if (externalDefinition != null)
                            {
                                doc.ParameterBindings.Insert(externalDefinition, newIB, BuiltInParameterGroup.PG_DATA);
                            }
                            //logger.Info("Applied Parameters to '" + projInfoName + "'. ");
                        }
                        tProjectInfo.Commit();
                    }
                }
            }


        }

        public DefinitionGroup SetDefinitionGroupToParameterFile(string groupName, Document doc)
        {
            //if group already exists in file - set:
            DefinitionFile sharedParameterFile = doc.Application.OpenSharedParameterFile();

            DefinitionGroup paramGroup = sharedParameterFile.Groups.get_Item(groupName);

            //if not - create:
            if (paramGroup == null)
            {
                using (Transaction transDefGroup = new Transaction(doc, "Insert ParameterGroup"))
                {
                    transDefGroup.Start();
                    paramGroup = sharedParameterFile.Groups.Create(groupName);
                    transDefGroup.Commit();
                }
            }
            return paramGroup;
        }

        public Definition SetDefinitionsToGroup(DefinitionGroup parGroup, string paramName, ParameterType pType, Document doc)
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

        //public void CreateParameters(IEnumerable<Xml_AttrRep> attributes)
        //{
        //    var groupedAttributes = attributes.GroupBy(r => (r.XmlNamespace + ": " + r.Name));

        //    foreach (var attributeGroup in groupedAttributes)
        //    {
        //        string paramName = attributeGroup.Key;

        //        Xml_AttrRep attribute = attributeGroup.First();

        //        DefinitionGroup currentGroup = default(DefinitionGroup);

        //        Definition paramDef = SetDefinitionsToGroup(currentGroup, paramName, GetParameterType(attribute.XmlType), doc);

        //    }
        //}

        public static HashSet<Xml_AttrRep> GetParcelAttributes()
        {
            var regAttr = new HashSet<Xml_AttrRep>();

            var parcelData = new Dictionary<string, Xml_AttrRep.AttrType>
            {
                {"Rechtsstand", Xml_AttrRep.AttrType.stringAttribute },
                {"Ebene", Xml_AttrRep.AttrType.stringAttribute },
                {"Rechtscharakter", Xml_AttrRep.AttrType.stringAttribute },
                {"Flaechenschluss", Xml_AttrRep.AttrType.stringAttribute },
                {"Nutzungsform", Xml_AttrRep.AttrType.areaAttribute },
            };

            foreach (var parcel in parcelData)
            {
                regAttr.Add(new Xml_AttrRep(Xml_AttrRep.AttrNsp.alkis, parcel.Key, parcel.Value, Xml_AttrRep.AttrHierarchy.parcel));
            }

            return regAttr;
        }


        //public void CreateProjectParameters(string paramGroupName, Dictionary<string, Xml_AttrRep.AttrType> attributes)
        //{
        //    //Create groups in parFile if not existent
        //    DefinitionGroup parGroupGeoref = SetDefinitionGroupToParameterFile(paramGroupName);

        //    Category projInfoCat = GetCategory(BuiltInCategory.OST_ProjectInformation);

        //    List<Definition> parProjInfoDef = GetExistentCategoryParametersDef(projInfoCat);

        //    var parProjInfo = parProjInfoDef.Select(p => p.Name);   //needed name for comparison

        //    foreach (var attribute in attributes)
        //    {
        //        Definition paramDef = SetDefinitionsToGroup(parGroupGeoref, attribute.Key, GetParameterType(attribute.Value));

        //        CategorySet assocCats = doc.Application.Create.NewCategorySet();

        //        if (!parProjInfo.Contains(attribute.Key))
        //            assocCats.Insert(projInfoCat);

        //        if (!assocCats.IsEmpty)
        //        {
        //            BindParameterDefinitionToCategories(paramDef, assocCats);
        //        }
        //    }
        //    //set SharedParameterFile back to user defined one (if applied)

        //    if (this.userDefinedParameterFile != null)
        //        doc.Application.SharedParametersFilename = this.userDefinedParameterFile;
        //}
    }


}
