using CommandLine;

namespace IFCGeoRefCheckerCommand
{
    public class CommandLineOptions
    {
        [Option('f', "files", Required = true, HelpText = "Input files to be checked.")]
        public IEnumerable<string> InputFiles { get; set; } = null!;

        [Option('w', "workDir", Required = true, HelpText = "Working Directory where checking results are saved")]
        public string workingDir { get; set; } = null!;


    }
}
