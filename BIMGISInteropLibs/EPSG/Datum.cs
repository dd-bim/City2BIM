using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RestSharp; //add lib to request from rest api
using Newtonsoft.Json;

namespace BIMGISInteropLibs.Datum
{
    public class Datum
    {
        public class Ellipsoid
        {
            public int Code { get; set; }
            public string Name { get; set; }
            public string href { get; set; }
        }

        public class PrimeMeridian
        {
            public int Code { get; set; }
            public string Name { get; set; }
            public string href { get; set; }
        }

        public class Scope
        {
            public int Code { get; set; }
            public string Name { get; set; }
            public string href { get; set; }
        }

        public class Extent
        {
            public int Code { get; set; }
            public string Name { get; set; }
            public string href { get; set; }
        }

        public class Usage
        {
            public int Code { get; set; }
            public string Name { get; set; }
            public string ScopeDetails { get; set; }
            public Scope Scope { get; set; }
            public Extent Extent { get; set; }
            public List<object> Links { get; set; }
            public object Deprecation { get; set; }
            public object Supersession { get; set; }
        }

        public class Change
        {
            public double Code { get; set; }
            public string href { get; set; }
        }

        public class NamingSystem
        {
            public int Code { get; set; }
            public string Name { get; set; }
            public string href { get; set; }
        }

        public class Alias
        {
            public int Code { get; set; }
            public string alias { get; set; }
            public NamingSystem NamingSystem { get; set; }
            public object Remark { get; set; }
        }

        public class Link
        {
            public string rel { get; set; }
            public string href { get; set; }
        }

        public class Root
        {
            public string Type { get; set; }
            public string Origin { get; set; }
            public object PublicationDate { get; set; }
            public object RealizationEpoch { get; set; }
            public Ellipsoid Ellipsoid { get; set; }
            public PrimeMeridian PrimeMeridian { get; set; }
            public List<Usage> Usage { get; set; }
            public object ConventionalReferenceSystem { get; set; }
            public object FrameReferenceEpoch { get; set; }
            public object RealizationMethod { get; set; }
            public int Code { get; set; }
            public List<Change> Changes { get; set; }
            public List<Alias> Alias { get; set; }
            public List<Link> Links { get; set; }
            public string Name { get; set; }
            public object Remark { get; set; }
            public string DataSource { get; set; }
            public string InformationSource { get; set; }
            public DateTime RevisionDate { get; set; }
            public List<object> Deprecations { get; set; }
            public List<object> Supersessions { get; set; }
        }

        public static Root get(int code)
        {
            //set http client
            var client = new RestClient("https://apps.epsg.org/api");

            //build request string (generic via espg code)
            string requestString = "/v1/Datum/" + code.ToString() + "/";

            //build request as rest request
            var request = new RestRequest(requestString, DataFormat.Json);

            //send request and get response
            var response = client.Execute(request) as RestResponse;

            //set root class (build object)
            Root root = JsonConvert.DeserializeObject<Root>(response.Content);

            //return root class
            return root;
        }

    }
}
