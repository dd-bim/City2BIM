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
        /// logger (for use during the runtime)
        /// </summary>
        private static Serilog.Core.Logger logger { get; set; }

        private static bool bInitialized = false;

        private static readonly object syncLock = new object();
        // optional external sink (tests can register ITestOutputHelper via a lambda)
        private static Action<LogPair>? externalSink;

        /// <summary>
        /// Registers an external log sink to receive log entries as they are generated.
        /// </summary>
        /// <remarks>The specified sink will be invoked for each log entry. Registering a new sink replaces any previously
        /// registered sink.</remarks>
        /// <param name="sink">A delegate that processes log entries. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="sink"/> is <see langword="null"/>.</exception>
        public static void RegisterSink(Action<LogPair> sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            lock (syncLock)
            {
                externalSink = sink;
            }
        }

        /// <summary>
        /// removes current registered external sink.
        /// </summary>
        public static void UnregisterSink()
        {
            lock (syncLock)
            {
                externalSink = null;
            }
        }

        /// <summary>
        /// init instance of log writer
        /// </summary>
        public static void initLogger(IfcTerrain.Config config)
        {
            if (bInitialized) return;
            //get file path from config
            string path = config.logFilePath;

            //init logfile name
            string logfileName;

            if(config.fileName != null)
            {
                //set filepath
                logfileName = System.IO.Path.GetFileNameWithoutExtension(config.fileName);
            }
            else
            {
                //set alternativ log file name (e.g. postgis)
                logfileName = config.fileType.ToString();
            }
            //get verbosity level from json settings
            var minLevel = config.verbosityLevel;

            //create level switching var
            var levelSwitch = new Serilog.Core.LoggingLevelSwitch();

            //change the minimum level for output in the log file
            switch (minLevel)
            {
                case LogType.error:
                    {
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
                        break;
                    }

                case LogType.warning:
                    {
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Warning;
                        break;
                    }

                case LogType.info:
                    {
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;
                        break;
                    }

                case LogType.debug:
                    {
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Debug;
                        break;
                    }

                case LogType.verbose:
                    {
                        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
                        break;
                    }
            }

            //get current time in wanted format
            var date = DateTime.Now.ToString("HH_mm");

            //create logger
            Serilog.Core.Logger results = new LoggerConfiguration()
                //write logging file to path --> use fileType and date for log file name
                .WriteTo.File(path + "\\" + logfileName + "_" + date + ".log")
                //change minimum level (set by config)
                .MinimumLevel.ControlledBy(levelSwitch)
                //init logger (have to be at the end of this config)
                .CreateLogger();

            //set logging instance
            logger = results;
            bInitialized = true;
        }

        /// <summary>
        /// function to write log file
        /// </summary>
        /// <param name="path">log file path</param>
        /// <param name="minLevel">min level for log output</param>
        public static void WriteLogFile()
        {
            List<LogPair> toWrite;
            lock (syncLock)
            {
                if (Entries.Count == 0) return;
                toWrite = Entries.ToList();
                Entries.Clear();
            }

            //go through each logging message
            foreach (var log in toWrite)
            {
                //differentiation into the individual log types and set output message
                switch (log.Type)
                {
                    case LogType.error:
                        {
                            logger.Error(log.Message);
                            break;
                        }
                    case LogType.warning:
                        {
                            logger.Warning(log.Message);
                            break;
                        }
                    case LogType.info:
                        {
                            logger.Information(log.Message);
                            break;
                        }
                    case LogType.debug:
                        {
                            logger.Debug(log.Message);
                            break;
                        }
                    case LogType.verbose:
                        {
                            logger.Verbose(log.Message);
                            break;
                        }
                }
            }
        }
        
        /// <summary>
        /// auxilary to add entries to log writer
        /// </summary>
        public static void Add(LogType logType, string message)
        {
            var pair = new LogPair(logType, message);
            lock (syncLock)
            {
                Entries.Add(pair);
            }

            //console logging
            Console.WriteLine(message);

            externalSink?.Invoke(pair);

            //write to log file
            WriteLogFile();
        }
    }
}