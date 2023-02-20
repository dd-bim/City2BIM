using System;
using System.Reflection;
using System.IO;

//include Revit API
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;

namespace CityBIM.GUI
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_LogFile : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            try
            {
                var today = DateTime.Now.ToString("yyyyMMdd");
                var programmDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                var versionNumber = revit.Application.Application.VersionNumber;
                var filePath = "Autodesk\\Revit\\Addins\\" + versionNumber + "\\HTWDDLog" + today + ".log";
                var logPath = Path.Combine(programmDataPath, filePath);

                //opens link
                System.Diagnostics.Process.Start(logPath);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Result.Failed;
            }
        }
    }
}