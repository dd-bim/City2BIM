using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Newtonsoft.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

using CommonRevit.Semantics;
using CommonRevit.IFC;
using DataCatPlugin.Settings;

namespace DataCatPlugin.ExternalDataCatalog
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

            var token = Connection.DataCatToken;

            if (token == null)
            {
                return false;
            }

            Int32 currentUnixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            if (currentUnixTimestamp + 7200 < Connection.TokenExpirationDate)
            {
                return true;
            }

            else
            {
                Connection.DataCatToken = null;
                Connection.DataClient = null;
                Connection.TokenExpirationDate = 0;
                return false;
            }
        }

        public static Schema createExternalDataCatalogSchema(Autodesk.Revit.DB.Document doc)
        {
            var externalSchema = SchemaUtils.getSchemaByName("ExternalDataCatalogSchema");

            if (externalSchema != null)
            {
                return externalSchema;
            }

            //using (SubTransaction trans = new SubTransaction(doc))
            using (Transaction trans = new Transaction(doc, "create DataCat Schema"))
            {
                trans.Start();

                SchemaBuilder schemaBuilder = new SchemaBuilder(Guid.NewGuid());
                schemaBuilder.SetSchemaName("ExternalDataCatalogSchema");
                schemaBuilder.SetReadAccessLevel(AccessLevel.Public);
                schemaBuilder.SetWriteAccessLevel(AccessLevel.Public);

                //schemaBuilder.AddMapField("data", typeof(string), typeof(string));
                //schemaBuilder.AddSimpleField("ifcClassification", typeof(string));

                schemaBuilder.AddMapField("Objects", typeof(string), typeof(string));

                externalSchema = schemaBuilder.Finish();

                trans.Commit();
            }

            return externalSchema;
        }

        public static Dictionary<string, string> getIfc2ExternalDataGuidDic(Document doc)
        {
            Dictionary<string, string> IfcToRevitDic = new Dictionary<string, string>();
            List<Element> elements = new List<Element>();
            var externalDataSchema = SchemaUtils.getSchemaByName("ExternalDataCatalogSchema");

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

        public static List<ExternalDataSchemaObject> getExternalDataSchemaObjectsFromDoc (Document doc)
        {
            var externalDataSchema = SchemaUtils.getSchemaByName("ExternalDataCatalogSchema");

            if (externalDataSchema == null)
            {
                return null;
            }

            var edsoList = new List<ExternalDataSchemaObject>();

            List<Element> affectedRevitElements = new List<Element>();
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            affectedRevitElements.AddRange(collector.WherePasses(new ExtensibleStorageFilter(externalDataSchema.GUID)).ToElements());
            
            if (affectedRevitElements.Count > 0)
            {
                foreach(var revitElement in affectedRevitElements)
                {
                    var entity = revitElement.GetEntity(externalDataSchema);

                    if (entity.IsValid())
                    {
                        var dict = entity.Get<IDictionary<string, string>>("Objects");
                        foreach (var dictEntry in dict)
                        {
                            edsoList.Add(JsonConvert.DeserializeObject<ExternalDataSchemaObject>(dictEntry.Value));
                        }
                    }
                }
                return edsoList;
            }
            else
            {
                return null;
            }
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
            IfcClassification ifcClassification = new IfcClassification{ ID = topCatalogue.Id, Name = topCatalogue.Name, Edition = topCatalogue.Version, EditionDate = topCatalogue.VersionDate, Location = Connection.DataClient.BaseUrl.ToString()};

            List<IfcClassificationReference> refList = new List<IfcClassificationReference>();
            //hierarchyEntries.Reverse();
            for (int i=0; i<hierarchyEntries.Count-1; i++)
            {
                refList.Add(new IfcClassificationReference
                {
                    ID = hierarchyEntries[i].Id,
                    Name = hierarchyEntries[i].Name,
                    Location = Connection.DataClient.BaseHost,
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
}
