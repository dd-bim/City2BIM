using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RestSharp; //add lib to request from rest api
using Newtonsoft.Json;

namespace BIMGISInteropLibs.ProjCRS
{
    public class BaseCoordRefSystem
    {
        public int Code { get; set; }
        public string Name { get; set; }
        public string href { get; set; }
    }

    public class Projection
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

    public class CoordSys
    {
        public int Code { get; set; }
        public string Name { get; set; }
        public string href { get; set; }
    }

    public class Change
    {
        public double Code { get; set; }
        public string href { get; set; }
    }

    public class Link
    {
        public string rel { get; set; }
        public string href { get; set; }
    }

    public class Root
    {
        public BaseCoordRefSystem BaseCoordRefSystem { get; set; }
        public Projection Projection { get; set; }
        public List<Usage> Usage { get; set; }
        public CoordSys CoordSys { get; set; }
        public string Kind { get; set; }
        public object Deformations { get; set; }
        public int Code { get; set; }
        public List<Change> Changes { get; set; }
        public List<object> Alias { get; set; }
        public List<Link> Links { get; set; }
        public string Name { get; set; }
        public string Remark { get; set; }
        public string DataSource { get; set; }
        public object InformationSource { get; set; }
        public DateTime RevisionDate { get; set; }
        public List<object> Deprecations { get; set; }
        public List<object> Supersessions { get; set; }
    }


    public class Request
    {
        /// <summary>
        /// request class for epsg codes
        /// </summary>
        public static Root get(int epsgCode, out bool requestSucessful)
        {
            //set http client
            var client = new RestClient("https://apps.epsg.org/api");

            //build request string (generic via espg code)
            string requestString = "/v1/ProjectedCoordRefSystem/" + epsgCode.ToString() +"/";

            //build request as rest request
            var request = new RestRequest(requestString, DataFormat.Json);

            //send request and get response
            var response = client.Execute(request) as RestResponse;

            //error handler
            if (response.IsSuccessful)
            {
                //set root class (build object)
                Root root = JsonConvert.DeserializeObject<Root>(response.Content);

                //set to true so processing will continue
                requestSucessful = true;

                //return response content
                return root;
            }
            else
            {
                requestSucessful = false;
                return null;
            }
        }
    }
}
