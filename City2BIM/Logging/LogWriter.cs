using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace City2BIM
{
    public static class LogWriter
    {
        //public static string LogPath = System.Environment.SpecialFolder.LocalApplicationData.ToString();

        public static void WriteLogFile(List<Logging.LogPair> messages, double all, double success, double? error, double? errorLod1, double? fatalError)
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "City2BIM");
            string name = "Log_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".txt";

            string path = Path.Combine(folder, name);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            Serilog.Core.Logger results = new LoggerConfiguration().WriteTo.File(path).CreateLogger();

            results.Information("Log-Protocol for CityGML-Import to Revit");
            results.Information("--------------------------------------------------");
            results.Information("Statistic");
            results.Information("----------------------");

            double statSucc = success / all * 100;

            results.Information("Amount of Solids or Surfaces: " + all);
            results.Information("Success rate = " + statSucc + " procent = " + success + " objects");

            if (error.HasValue)
            {
                double statErr = (double)error / all * 100;
                results.Warning("Failure rate (LOD1, correct contour) = " + statErr + " procent = " + error + " objects");
            }

            if (errorLod1.HasValue)
            {
                double statErr = (double)errorLod1 / all * 100;
                results.Warning("Failure rate (LOD1 Fallback, convex hull contour) = " + statErr + " procent = " + errorLod1 + " objects");
            }

            if (fatalError.HasValue)
            {
                double fatStatErr = (double)fatalError / all * 100;
                results.Error("Fatal error: no geometry at = " + fatStatErr + " procent = " + fatalError + " objects");
            }
            results.Information("----------------------");
            results.Information("----------------------");

            results.Information("Protocol for each Building(Part)");
            results.Information("-------------------------------------");


            foreach (var log in messages)
            {
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
                }
            }
        }
        public enum LogType { error, info, warning }
    }
}
