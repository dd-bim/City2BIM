using System;
using System.Globalization;
using System.Collections.Generic;
using System.Xml.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using City2BIM.RevitBuilder;
using City2BIM.GetGeometry;
using Serilog;

namespace City2BIM
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class ReadCityGML : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            Log.Logger = new LoggerConfiguration()
                //.MinimumLevel.Debug()
                .WriteTo.File(@"C:\Users\goerne\Desktop\logs_revit_plugin\\log_plugin" + DateTime.UtcNow.ToFileTimeUtc() + ".txt"/*, rollingInterval: RollingInterval.Day*/)
                .CreateLogger();



            UIApplication uiApp = revit.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            TaskDialog.Show("CityGML", "Button funzt");

            ReadData readdata = new ReadData();

            RevitGeometryBuilder cityModel = new RevitGeometryBuilder(doc, readdata.ReadGeometryFromXML());
            //RevitGeometryBuilder cityModel = new RevitGeometryBuilder(doc, buildingList);

            //debug:


            Log.Information("Start...");

            Log.Debug("ReadData-Object, gelesene Geometrien = " + readdata.ReadGeometryFromXML().Count);

            string res = "";

            if(cityModel == null)
                res = "empty";
            else
                res = "not empty";

            Log.Debug("CityModel: " + res);

            //debug


            cityModel.CreateBuildings();




            return Result.Succeeded;
        }
    }
}