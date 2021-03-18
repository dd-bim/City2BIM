using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;              //need for convert json
using System.IO;                    //need for file handling
using BIMGISInteropLibs.IfcTerrain; //need for jSettings

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
            JsonSettings jSettings = JsonConvert.DeserializeObject<JsonSettings>(jText);

            //set to default values
            double? breakDist = 0.0;
            double? refLatitude = 0.0;
            double? refLongitude = 0.0;
            double? refElevation = 0.0;

            //create new instance of the ConnectionInterface
            var conn = new ConnectionInterface();


            conn.mapProcess(jSettings, breakDist, refLatitude, refLongitude, refElevation);

            
            
        }
    }
}
