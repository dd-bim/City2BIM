using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RestSharp; //add lib to request from rest api
using RestSharp.Authenticators;

namespace BIMGISInteropLibs.EPSG
{
    public class EPSGReader
    {
        /// <summary>
        /// request class for epsg codes
        /// </summary>
        public static void Request(int epsgCode)
        {
            //set http client
            var client = new RestClient("https://apps.epsg.org/api");

            //build request string (generic via espg code)
            string requestString = "/v1/CoordRefSystem/?keywords=" + epsgCode.ToString();

            //build request as rest request
            var request = new RestRequest(requestString, DataFormat.Json);

            //send request and get response
            var response = client.Get(request);
        }
    }
}
