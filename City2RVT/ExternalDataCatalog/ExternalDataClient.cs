using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RestSharp;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;

namespace City2RVT.ExternalDataCatalog
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
            request.AddHeader("Authorization", "Bearer " + Prop_Revit.DataCatToken.RawData);

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

    }
}
