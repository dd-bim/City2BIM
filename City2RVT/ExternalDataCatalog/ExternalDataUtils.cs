using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                              findSubjects(input: {query: $searchText, pageSize: 20}) {
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
        public List<Property> properties { get; set; }
    }

    public class FindSubjects
    {
        public List<Node> nodes { get; set; }
    }

    public class DataFind
    {
        public FindSubjects findSubjects { get; set; }
    }

    public class FindResponse
    {
        public DataFind data { get; set; }
    }

}
