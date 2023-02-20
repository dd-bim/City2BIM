using System.Collections.Generic;

using RestSharp;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using DataCatPlugin.Settings;

namespace DataCatPlugin.ExternalDataCatalog
{
    public class ExternalDataClient: RestClient
    {
        public ExternalDataClient(string urlEndPoint) : base(urlEndPoint)
        {

        }

        public JwtSecurityToken getLoginTokenForCredentials(string userName, string passWord)
        {
            string query = ExternalDataUtils.getDataCatLoginQueryAsRawJson(userName, passWord);

            var request = new RestRequest(Method.POST);
            request.AddParameter("application/json", query, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;

            var response = this.Post(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(response.Content);
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(loginResponse.data.login);
                return token;
            }
            else
            {
                return null;
            }
        }

        public FindResponse querySubjects(string searchText)
        {
            string query = ExternalDataUtils.getSubjectSearchQueryAsRawJson(searchText);

            var request = new RestRequest(Method.POST);
            request.AddParameter("application/json", query, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Authorization", "Bearer " + Connection.DataCatToken.RawData);

            var response = this.Post(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var searchResponse = JsonConvert.DeserializeObject<FindResponse>(response.Content);
                return searchResponse;
            }
            else
            {
                return null;
            }
        }

        public FindResponse querySubjectsWithHierarchy(string searchText)
        {
            string query = ExternalDataUtils.getSubjectSearchAndHierarchyQueryAsRawJson(searchText);

            var request = new RestRequest(Method.POST);
            request.AddParameter("application/json", query, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Authorization", "Bearer " + Connection.DataCatToken.RawData);

            var response = this.Post(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var searchResponse = JsonConvert.DeserializeObject<FindResponse>(response.Content);
                var heList = getHierarchy(response.Content);

                for (int i=0; i<searchResponse.data.findSubjects.nodes.Count; i++)
                {
                    searchResponse.data.findSubjects.nodes[i].addIfcClassificationReference(heList[i]);
                }

                return searchResponse;
            }
            else
            {
                return null;
            }
        }

        private static List<List<HierarchyEntry>> getHierarchy(string jsonResponse)
        {
            List<List<HierarchyEntry>> heList = new List<List<HierarchyEntry>>();
            dynamic response = JObject.Parse(jsonResponse);
            var nodes = response.data.findSubjects.nodes;

            foreach(var node in nodes)
            {
                List<HierarchyEntry> innerList = new List<HierarchyEntry>();
                getReferencedCollections(node.collectedBy, innerList);
                heList.Add(innerList);
            }

            return heList;
        }

        private static void getReferencedCollections(dynamic collectedBy, List<HierarchyEntry> list)
        {
            if (collectedBy.nodes.Count > 0)
            {
                var name = collectedBy.nodes[0].relatingCollection.name.Value;
                var id = collectedBy.nodes[0].relatingCollection.id.Value;
                var versionId = collectedBy.nodes[0].relatingCollection.versionId.Value;
                var versionDate = collectedBy.nodes[0].relatingCollection.versionDate.Value;
                var he = new HierarchyEntry();
                he.Id = id;
                he.Name = name;
                he.Version = versionId;
                he.VersionDate = versionDate;
                list.Add(he);
                getReferencedCollections(collectedBy.nodes[0].relatingCollection.collectedBy, list);
            }
        }

    }
}
