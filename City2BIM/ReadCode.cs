using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using City2BIM.GetGeometry;
using City2BIM.GetSemantics;
using City2BIM.RevitBuilder;
using Serilog;
using Attribute = City2BIM.GetSemantics.XmlAttribute;
using Solid = City2BIM.GetGeometry.C2BSolid;
using XYZ = City2BIM.GetGeometry.C2BPoint;


namespace City2BIM
{

        [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
        public class ReadCode : IExternalCommand
        {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            //Auslesen der Codeliste:

            ////var path = @"D:\1_CityBIM\1_Programmierung\City2BIM\CityGML_Data\CityGML_Data\codelists\SIG3D\_AbstractBuilding_function.xml";
            var path = @"D:\1_CityBIM\1_Programmierung\City2BIM\CityGML_Data\CityGML_Data\codelists\AdV\BuildingFunctionTypeAdv.xml";

            var codeCl = new GetSemantics.ReadCodeList();
            var codeDict = codeCl.ReadCodes(path);

            //----------------------------------------------------

            //Revit-Elemente auslesen:

            UIApplication uiApp = revit.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            FilteredElementCollector fl = new FilteredElementCollector(doc);

            var flcat = fl.OfCategory(BuiltInCategory.OST_Entourage);

            var elem = flcat.ToElements();

            using(Transaction t = new Transaction(doc, "Update Attribute values"))
            {
                t.Start();

                foreach(var el in elem)
                {
                    var fkt = el.LookupParameter("bldg: function");

                    var fktCode = fkt.AsString();

                    codeDict.TryGetValue(fktCode, out string desc);

                    fkt.Set(desc);
                }

                t.Commit();

            }




            
            return Result.Succeeded;

        }
    }
}
