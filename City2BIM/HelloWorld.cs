using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Serilog;

namespace City2BIM
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class HelloWorld : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Revit", "Hello World");

            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Debug()
            //    .WriteTo.File(@"D:\\1_CityBIM\\1_Programmierung\\City2BIM\\City2BIM_Revit\\log.txt", rollingInterval: RollingInterval.Day)
            //    .WriteTo.Console()
            //    .CreateLogger();

            //Log.Information("Hello, world!");

            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }
}