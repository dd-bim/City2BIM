using System;

//include Revit API
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CityBIM.GUI
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_Documentation : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            try
            {
                //direct Link to GITHUB - Repro so it should be accessable for "all"
                string docuPath = "https://github.com/dd-bim/City2BIM/wiki/Revit-Plugin";
                
                //opens link
                System.Diagnostics.Process.Start(docuPath);

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