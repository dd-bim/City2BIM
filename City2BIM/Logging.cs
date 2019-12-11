using Serilog;
using System.Collections.Generic;

namespace City2BIM
{
    public static class Logging
    {
        public static void WriteLogFile(List<GmlRep.BldgLog> messages, double all, double success, double? error, double? errorLod1, double? fatalError)
        {
            Serilog.Core.Logger results = new LoggerConfiguration()
               //.MinimumLevel.Debug()
               .WriteTo.File(@"C:\Users\goerne\Desktop\logs_revit_plugin\\RevitInfos_Solids.txt", rollingInterval: RollingInterval.Minute)
               .CreateLogger();

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
                double statErr = (double)error / all * 100;
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
