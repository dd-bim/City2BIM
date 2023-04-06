using CommandLine;
using IFCGeorefShared;
using Serilog;
using Xbim.Ifc;

namespace IFCGeoRefCheckerCommand
{
    internal class Program
    {
        static void Main(string[] args)
        {

#if DEBUG
            args = new[] { "-f", @"D:\Testdaten\GeoRefChecker\XPlanung 3D Tegel Projekt\XPlanung-3D_Bebauungsplan_12-50a.ifc", @"..\..\..\input\Buerogebaeude.ifc", @"..\..\..\input\301110Gebaeude-Gruppe.ifc", "-w", @"..\..\..\workingDir" };
#endif
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            GdalConfiguration.ConfigureOgr();
            CommandLine.Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(RunChecks).WithNotParsed(HandleParseError);

        }
        private static void RunChecks(CommandLineOptions options)
        {
            List<string> fileNames = options.InputFiles.ToList();
            Log.Information($"A total of {fileNames.Count} files were specified.");

            foreach (string file in fileNames)
            {
                if (!File.Exists(file)) 
                {
                    Log.Error($"Specified file does not exist or can not be read: {file}");
                    continue;
                }

                Log.Information($"Opening and checking file {file}");
                using (var model = IfcStore.Open(file))
                {
                    var checker = new GeoRefChecker(model);
                    
                    try
                    {
                        Directory.CreateDirectory(options.workingDir);
                    }
                    catch (Exception ex)
                    {
                        if (ex is IOException || ex is DirectoryNotFoundException)
                        {
                            Log.Error($"Specified working dir {options.workingDir} is not valid!");
                        }

                        else
                        {
                            throw;
                        }

                        Log.Error($"Terminating check due to error");
                        return;

                    }

                    Log.Information($"Writing protocoll to working directory {options.workingDir}");
                    checker.WriteProtocoll(options.workingDir);
                }
            }

            Log.Information($"Finished checking files");

        }

        private static void HandleParseError(IEnumerable<Error> errors)
        {
            Log.Error($"Error while parsing command line arguments!");
            foreach (Error error in errors)
            {
                Log.Error(error.ToString());
            }
        }

        
    }
}