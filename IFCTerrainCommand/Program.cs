using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BIMGISInteropLibs.IfcTerrain.Model; //needed for json collection
using Newtonsoft.Json;  //need for convert json

namespace IFCTerrainCommand
{
    /// <summary>
    /// IFCTerrain Command (for batch processing)
    /// </summary>
    static class Program
    {
        static void Main(string[] args)
        {
            //path of json files
            string jPath = args[0];
            
            //read json as text
            string jText = System.IO.File.ReadAllText(jPath);

            //create collection from each json file
            JsonCollection jColl = JsonConvert.DeserializeObject<JsonCollection>(jText);

            //set to default values
            double? breakDist = 0.0;
            double? refLatitude = 0.0;
            double? refLongitude = 0.0;
            double? refElevation = 0.0;

            //create new instance of the ConnectionInterface
            var conn = new BIMGISInteropLibs.IfcTerrain.ConnectionInterface();

            //loop through all json settings
            foreach (BIMGISInteropLibs.IfcTerrain.JsonSettings jSettings in jColl.JsonSettings)
            {
                //start mapping process
                conn.mapProcess(jSettings, breakDist, refLatitude, refLongitude, refElevation);
            }
        }
    }
}
