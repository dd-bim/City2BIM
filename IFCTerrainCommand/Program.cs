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
        #region config
        /// <summary>
        /// input files
        /// </summary>
        private static string[] fileInput { get; set; }

        /// <summary>
        /// user handling to restart the process again without cmd restart
        /// </summary>
        private static string restart { get; set; } = "no";

        /// <summary>
        /// if batch process (true = the files to be converted has already been passed)
        /// </summary>
        private static bool batch { get; set; }
        #endregion config

        /// <summary>
        /// main programm (currently only this programm)
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            do
            {
                //init boolean for error handling
                bool inputValid = false;

                //check if args are set (equals batch input) 
                if (args.Length == 0)
                {
                    //set batch process to false
                    batch = false;

                    //get filepath
                    inputValid = cmdInput();
                }
                else
                {
                    //set args to file input
                    fileInput = args;

                    //set batch process to true
                    batch = true;

                    //set restart to false
                    restart = "n";

                    //[TODO] check if file paths are valid
                    inputValid = true;
                }

                //if input file paths are valid
                if (inputValid)
                {
                    //kick off conversion process
                    startConverison(fileInput);

                    //if it is not process via batch --> more user handling
                    if (!batch)
                    {
                        Console.WriteLine("Conversion completed!");

                        //request
                        Console.WriteLine("Want to convert more files? (j/n)");

                        //wait for user feedback
                        restart = Console.ReadLine();
                    }
                }
                //for error handling --> user can input file path again
                else
                {
                    Console.WriteLine("Want to try again? ('j' or 'n')");

                    //wait for user input
                    restart = Console.ReadLine();
                }
            } while (restart.Contains('j') || restart.Contains('J'));
        }

        /// <summary>
        /// start conversion process
        /// </summary>
        private static void startConverison(string[] files)
        {
            //loop through all (*.json) files
            foreach (string path in files)
            {
                //read json as text
                string jText = File.ReadAllText(path);

                //create collection from each json file
                Config jSettings = JsonConvert.DeserializeObject<Config>(jText);

                //init logger
                BIMGISInteropLibs.Logging.LogWriterIfcTerrain.initLogger(jSettings);

                //create new instance of the ConnectionInterface
                var conn = new ConnectionInterface();

                //start mapping process
                //TODO: add jSettings for metadata to IfcPropertySet
                bool result = conn.mapProcess(jSettings, null, null);

                if (!result)
                {
                    Console.WriteLine("[ERROR] Processing failed. Please check log file!");
                    
                    //Console.WriteLine("Close application with 'enter'");
                    //Console.ReadLine();
                    break;
                }
                else
                {
                    Console.WriteLine("Processing succesful. Please check log file for more information!");
                }
            }
            //finish programm
            return;
        }

        /// <summary>
        /// read file path(s) via cmd input
        /// </summary>
        /// <returns></returns>
        private static bool cmdInput()
        {
            //error handling add file
            string addFile = null;

            //list to be filled for different files
            List<string> files = new List<string>();

            do
            {
                Console.WriteLine("Input file path to config: ");

                //get path from user input
                var dirPath = @"" + Console.ReadLine();

                try
                {
                    //get file from path
                    FileInfo file = new FileInfo(dirPath.Replace("\"", ""));

                    //check if file is json
                    if (file.Extension.Equals(".json"))
                    {
                        //add file to list
                        files.Add(file.FullName);
                        Console.WriteLine("File add: " + file.Name.ToString());
                    }
                    else
                    {
                        Console.WriteLine("File format is invalid. Please use a *.json file!");
                        return false;
                    }
                }
                catch
                {
                    Console.WriteLine("Input file path is not valid");
                    return false;
                }

                //write / overwrite fileInput via file list
                fileInput = files.ToArray();

                Console.WriteLine("Want to add more files? (j/ n)");

                //request if more files should be added
                addFile = Console.ReadLine();

            } while (addFile.Contains('j') || addFile.Contains('J'));
            return true;
        }
    }
}