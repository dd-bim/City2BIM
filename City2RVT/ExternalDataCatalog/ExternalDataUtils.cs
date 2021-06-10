using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using City2RVT;

namespace City2RVT.ExternalDataCatalog
{
    public class ExternalDataUtils
    {
        public static string getDataCatLoginQueryAsRawJson(string userName, string passWord)
        {
            string query = @"""mutation Login($username: ID!, $password: String!) {
                            login(input: {
                            username: $username
                            password: $password
                            })
                        }"" ";

            string payload = @"{{""query"" : {0}, ""variables"": {{ ""username"": ""{1}"", ""password"": ""{2}"" }} }}";

            payload = string.Format(payload, query, userName, passWord);

            return payload;
        }

        public static string getSubjectSearchQueryAsRawJson(string searchText)
        {
            string query = @"""query findInputQuery($searchText: String!) {
                              findSubjects(input: {query: $searchText, pageSize: 100}) {
                                nodes {
                                  id
                                  name
                                  properties {
                                    id
                                    name
                                  }
                                }
                              }
                            }"" ";

            string payload = @"{{""query"" : {0}, ""variables"": {{ ""searchText"": ""{1}""}} }}";
            payload = string.Format(payload, query, searchText);
            return payload;
        }

        public static string getSubjectSearchAndHierarchyQueryAsRawJson(string searchText)
        {
            string query = @"""query findSubjectQuery($searchText: String!) {
                                findSubjects(input: {query: $searchText}) {
                                    nodes {
                                        id
                                        name
                                    properties {
                                        id
                                        name
                                    }
                                    collectedBy {
                                        nodes {
                                            relatingCollection {
                                                name
                                                id
                                                versionId
                                                versionDate
                                                collectedBy {
                                                    nodes {
                                                        relatingCollection {
                                                            name
                                                            id
                                                            versionId
                                                            versionDate
                                                            collectedBy {
                                                                nodes {
                                                                    name
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                   }
                                }
                            }""";
            string payload = @"{{""query"" : {0}, ""variables"": {{ ""searchText"": ""{1}""}} }}";
            payload = string.Format(payload, query, searchText);
            return payload;
        }

        public static bool testTokenValidity()
        {

            var token = Prop_Revit.DataCatToken;

            if (token == null)
            {
                return false;
            }

            Int32 currentUnixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            if (currentUnixTimestamp + 7200 < Prop_Revit.TokenExpirationDate)
            {
                return true;
            }

            else
            {
                Prop_Revit.DataCatToken = null;
                Prop_Revit.DataClient = null;
                Prop_Revit.TokenExpirationDate = 0;
                return false;
            }
        }

        public static Schema createExternalDataCatalogSchema(Autodesk.Revit.DB.Document doc)
        {
            var externalSchema = utils.getSchemaByName("ExternalDataCatalogSchema");

            if (externalSchema != null)
            {
                return externalSchema;
            }

            using (SubTransaction trans = new SubTransaction(doc))
            {
                trans.Start();

                SchemaBuilder schemaBuilder = new SchemaBuilder(Guid.NewGuid());
                schemaBuilder.SetSchemaName("ExternalDataCatalogSchema");
                schemaBuilder.SetReadAccessLevel(AccessLevel.Public);
                schemaBuilder.SetWriteAccessLevel(AccessLevel.Public);

                schemaBuilder.AddMapField("data", typeof(string), typeof(string));
                schemaBuilder.AddSimpleField("ifcClassification", typeof(string));

                externalSchema = schemaBuilder.Finish();

                trans.Commit();
            }

            return externalSchema;
        }

        public static Dictionary<string, string> getIfc2ExternalDataGuidDic(Document doc)
        {
            Dictionary<string, string> IfcToRevitDic = new Dictionary<string, string>();
            List<Element> elements = new List<Element>();
            var externalDataSchema = utils.getSchemaByName("ExternalDataCatalogSchema");

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.WherePasses(new ExtensibleStorageFilter(externalDataSchema.GUID));

            elements.AddRange(collector.ToElements());

            foreach (var element in elements)
            {
                Guid cSharpGuid = ExportUtils.GetExportId(doc, element.Id);
                string ifcGuid = IfcGuid.ToIfcGuid(cSharpGuid);
                IfcToRevitDic.Add(ifcGuid, element.UniqueId);
            }

            return IfcToRevitDic;

        }

    }


    public class LoginResponse
    {
        public LoginData data { get; set; }
    }

    public class LoginData
    {
        public string login { get; set; }
    }


    public class Property
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Node
    {
        public string id { get; set; }
        public string name { get; set; }
        public ObservableCollection<Property> properties { get; set; }

        public IfcClassification ifcClassification {get; set;}

        public void addIfcClassificationReference(List<HierarchyEntry> hierarchyEntries)
        {
            var topCatalogue = hierarchyEntries[hierarchyEntries.Count - 1];
            IfcClassification ifcClassification = new IfcClassification{ ID = topCatalogue.Id, Name = topCatalogue.Name, Edition = topCatalogue.Version, EditionDate = topCatalogue.VersionDate, Location = Prop_Revit.DataClient.BaseUrl.ToString()};

            List<IfcClassificationReference> refList = new List<IfcClassificationReference>();
            //hierarchyEntries.Reverse();
            for (int i=0; i<hierarchyEntries.Count-1; i++)
            {
                refList.Add(new IfcClassificationReference
                {
                    ID = hierarchyEntries[i].Id,
                    Name = hierarchyEntries[i].Name,
                    Location = Prop_Revit.DataClient.BaseHost,
                    ReferencedSource = hierarchyEntries[i + 1].Id
                });
            }
            ifcClassification.RefList = refList;
            this.ifcClassification = ifcClassification;
        }
    }

    public class FindSubjects
    {
        public ObservableCollection<Node> nodes { get; set; }
    }

    public class DataFind
    {
        public FindSubjects findSubjects { get; set; }
    }

    public class FindResponse
    {
        public DataFind data { get; set; }
    }

    public class HierarchyEntry { 
    
        public string Name { get; set; }
        public string Id { get; set;}
        public string Version { get; set; }
        public string VersionDate { get; set; }

        public HierarchyEntry()
        {

        }
            
    }

    public class ExternalDataSchemaObject
    {
        public string ObjectType { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public IfcClassification IfcClassification { get; set; }

        public ExternalDataSchemaObject(string objectType, Dictionary<string, string> props, IfcClassification ifcClassification)
        {
            this.ObjectType = objectType;
            this.Properties = props;
            this.IfcClassification = ifcClassification;
        }

        public Dictionary<string, Dictionary<string, string>> prepareForEditorWindow()
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            result.Add(this.ObjectType, this.Properties);
            return result;
        }
    }
}
