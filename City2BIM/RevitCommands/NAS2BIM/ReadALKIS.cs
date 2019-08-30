using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NasImport;

namespace City2BIM
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class ReadALKIS : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

                        {
                            NasImportForm newForm = new NasImportForm(commandData);
                            var result = newForm.ShowDialog();
                            if(result == System.Windows.Forms.DialogResult.OK)
                            {

                            }

                return Result.Succeeded;
            }



            //var process = new NasImport.PlugIn();

            //return Result.Succeeded;
        }
    }
}