using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Xml;
using netDxf;
using netDxf.Entities;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

using Excel = Microsoft.Office.Interop.Excel;
using lageplanImport.Lines;

namespace lageplanImport
{
    [TransactionAttribute(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    // External Revit Command. Executed when user pushs the Ribbon Button to start the Plugin. 
    public class Attributes : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // A listbox is called and shown. This listbox will be used to provide different objects and attributes for the families. 
            AddAttributes lbf = new AddAttributes(commandData);            
            var result = lbf.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {

            }
            return Result.Succeeded;
        }

        public List<string> addParam(object chosen)
        {
            // A dictionary is created. It includes the name of different objects (e.g. tree, fence, street lamp) as key and a list of attributes as value. 
            Dictionary<string, List<String>> objectList = new Dictionary<string, List<string>>();
            List<string> Laubbaum = new List<string>();
            Laubbaum.Add("Art");
            Laubbaum.Add("Stammumfang");
            Laubbaum.Add("Kronendurchmesser");

            List<string> Nadelbaum = new List<string>();
            Nadelbaum.Add("Art");
            Nadelbaum.Add("Stammumfang");
            Nadelbaum.Add("Kronendurchmesser");

            List<string> Strassenlaterne = new List<string>();
            Strassenlaterne.Add("Befestigungstyp");
            Strassenlaterne.Add("Bemerkung");

            List<string> Gebuesch = new List<string>();
            Gebuesch.Add("Bemerkung");
            Gebuesch.Add("Botanische Art");

            objectList.Add("Laubbaum", Laubbaum);
            objectList.Add("Nadelbaum", Nadelbaum);
            objectList.Add("Strassenlaterne", Strassenlaterne);
            objectList.Add("Gebuesch", Gebuesch);

            // Depending on the object the user chosses in the given listbox, a new list is created with the chosen key and value of the dictionary. 
            // This new list contains the attributes and will be used in the transaction for adding parameters to the family. 
            List<string> chosenObjectList = new List<string>();
            try
            {
                objectList.TryGetValue(chosen.ToString(), out chosenObjectList);
            }
            catch
            {
                TaskDialog.Show("Revit", "Für die ausgewählte Objektart '" + chosen.ToString() + "' existieren keine definierten Attribute.");

            }

            return chosenObjectList;
        }
        public Dictionary<string, List<String>> dictMethod()
        {
            // A dictionary is created. It includes the name of different objects (e.g. tree, fence, street lamp) as key and a list of attributes as value. 
            Dictionary<string, List<String>> objectList = new Dictionary<string, List<string>>();
            List<string> Laubbaum = new List<string>();
            Laubbaum.Add("Art");
            Laubbaum.Add("Stammumfang");
            Laubbaum.Add("Kronendurchmesser");

            List<string> Nadelbaum = new List<string>();
            Nadelbaum.Add("Art");
            Nadelbaum.Add("Stammumfang");
            Nadelbaum.Add("Kronendurchmesser");

            List<string> Strassenlaterne = new List<string>();
            Strassenlaterne.Add("Befestigungstyp");
            Strassenlaterne.Add("Bemerkung");

            List<string> Gebuesch = new List<string>();
            Gebuesch.Add("Bemerkung");
            Gebuesch.Add("Botanische Art");

            objectList.Add("Laubbaum", Laubbaum);
            objectList.Add("Nadelbaum", Nadelbaum);
            objectList.Add("Strassenlaterne", Strassenlaterne);
            objectList.Add("Gebuesch", Gebuesch);

            return objectList;
        }
    }
}
