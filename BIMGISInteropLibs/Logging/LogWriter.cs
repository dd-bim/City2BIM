using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace BIMGISInteropLibs.Logging
{
    public static class LogWriter
    {
        public static void WriteLogFile(List<LogPair> messages, bool solid, double all, double success, double? error, double? errorLod1, double? fatalError)
        {
            //string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "City2BIM");
            //string name = "Log_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".txt";

            //string path = Path.Combine(folder, name);

            //if (!Directory.Exists(folder))
            //    Directory.CreateDirectory(folder);

            //Serilog.Core.Logger result = new LoggerConfiguration().WriteTo.File(path).CreateLogger();

            //  !!!!!!!!!!!!!!!!!!!!!!!!!!!
            //  The function requires a serilog logger set up at a different place!
            //  !!!!!!!!!!!!!!!!!!!!!!!!!!!

            Log.Information("Log-Protocol for CityGML-Import to BIM");
            Log.Information("--------------------------------------------------");

            if (solid)
            {
                Log.Information("Kind of transfered geometry: Solids");
                Log.Information("Calculation parameters");
                Log.Information("----------------------");

                Log.Information("Equal Point distance = " + Math.Sqrt(City2BIM_prop.EqualPtSq) + " m.");
                Log.Information("Equal Planes normal distance (normal length 1m) = " + Math.Sqrt(City2BIM_prop.EqualPlSq) + " m.");
                Log.Information("Maximum deviation between calculated point and original point (if applicable) = " + Math.Sqrt(City2BIM_prop.MaxDevPlaneCutSq) + " m.");

                Log.Information("----------------------");
                Log.Information("----------------------");
            }
            else
                Log.Information("Kind of transfered geometry: Surfaces");

            Log.Information("Statistic");
            Log.Information("----------------------");

            double statSucc = success / all * 100;

            Log.Information("Amount of Solids or Surfaces: " + all);
            Log.Information("Success rate = " + statSucc + " percent = " + success + " objects");

            if (error.HasValue)
            {
                double statErr = (double)error / all * 100;
                Log.Warning("Failure rate (LOD1, correct contour) = " + statErr + " percent = " + error + " objects");
            }

            if (errorLod1.HasValue)
            {
                double statErr = (double)errorLod1 / all * 100;
                Log.Warning("Failure rate (LOD1 Fallback, convex hull contour) = " + statErr + " percent = " + errorLod1 + " objects");
            }

            if (fatalError.HasValue)
            {
                double fatStatErr = (double)fatalError / all * 100;
                Log.Error("Fatal error: no geometry at = " + fatStatErr + " percent = " + fatalError + " objects");
            }
            Log.Information("----------------------");
            Log.Information("----------------------");

            Log.Information("Protocol for each Building(Part)");
            Log.Information("-------------------------------------");


            foreach (var log in messages)
            {
                switch (log.Type)
                {
                    case (LogType.error):
                        {
                            Log.Error(log.Message);
                            break;
                        }
                    case (LogType.warning):
                        {
                            Log.Warning(log.Message);
                            break;
                        }
                    case (LogType.info):
                        {
                            Log.Information(log.Message);
                            break;
                        }
                }
            }
        }

    }
}
