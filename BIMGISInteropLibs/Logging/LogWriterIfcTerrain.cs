using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog; //include logging bib

namespace BIMGISInteropLibs.Logging
{
    /// <summary>
    /// Logging for processing using IFCTerrain
    /// </summary>
    public static class LogWriterIfcTerrain
    {
        /// <summary>
        /// logging container - include verbosity level + message
        /// </summary>
        public static List<LogPair> Entries { get; set; } = new List<LogPair>();

        /// <summary>
        /// function to write log file
        /// </summary>
        /// <param name="path">log file path</param>
        /// <param name="minLevel">min level for log output</param>
        public static void WriteLogFile(string path, LogType minLevel, string fileName)
        {
            //create level switching var
            var levelSwitch = new Serilog.Core.LoggingLevelSwitch();

            //change the minimum level for output in the log file
            switch (minLevel)
            {
                case (LogType.error):
                    {
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
                        break;
                    }

                case (LogType.warning):
                    {
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Warning;
                        break;
                    }

                case (LogType.info):
                    {
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;
                        break;
                    }

                case (LogType.debug):
                    {
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Debug;
                        break;
                    }

                case (LogType.verbose):
                    {
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
                        break;
                    }
            }

            //get current time in wanted format
            var date = DateTime.Now.ToString("HH_mm_ss");

            //create logger
            Serilog.Core.Logger results = new LoggerConfiguration()
                //write logging file to path --> use fileType and date for log file name
                .WriteTo.File(path + "/" + fileName + "_" + date + ".log")
                //change minimum level
                .MinimumLevel.ControlledBy(levelSwitch)
                //init logger (have to be at the end of this config)
                .CreateLogger();

            //start logging protocol 
            results.Information("Log-Protocol for IFCTerrain");
            results.Information("--------------------------------------------------");

            //go through each logging message
            foreach (var log in Entries)
            {
                //differentiation into the individual log types and set output message
                switch (log.Type)
                {
                    case (LogType.error): 
                        {
                            results.Error(log.Message);
                            break;
                        }
                    case (LogType.warning):
                        {
                            results.Warning(log.Message);
                            break;
                        }
                    case (LogType.info):
                        {
                            results.Information(log.Message);
                            break;
                        }
                    case (LogType.debug):
                        {
                            results.Debug(log.Message);
                            break;
                        }
                    case (LogType.verbose):
                        {
                            results.Verbose(log.Message);
                            break;
                        }
                }
            }
            //clear all entries
            Entries.Clear();
        }
    }
}
