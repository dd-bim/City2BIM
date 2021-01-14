using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

using City2RVT.ExternalDataCatalog;

namespace City2RVT.GUI.DataCat
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_DataCatLogin : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            Document doc = revit.Application.ActiveUIDocument.Document;

            bool tokenValidity = ExternalDataUtils.testTokenValidity();

            if (tokenValidity)
            {
                string output = "You are already successfully loged into: {0} \nCredentials valid until {1}";
                output = string.Format(output, Prop_Revit.DataCatToken.Issuer, Prop_Revit.DataCatToken.ValidTo);
                TaskDialog.Show("Information", output);
            }

            else
            {
                var loginScreen = new LoginScreen();
                var dialogResult = loginScreen.ShowDialog();

                if (dialogResult == true)
                {
                    var client = new ExternalDataClient(loginScreen.endPointUrl);
                    var token = client.getLoginTokenForCredentials(loginScreen.UserName, loginScreen.passWord);

                    if (token != null)
                    {
                        Prop_Revit.DataCatToken = token;
                        Prop_Revit.TokenExpirationDate = (Int32)(token.ValidTo.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        Prop_Revit.DataClient = client;
                        TaskDialog.Show("Message", "Successfully loged in!");
                    }

                    else
                    {
                        TaskDialog.Show("Warning!", "Something went wrong! \n UserName, Password and URL correct?");
                    }
                } 
            }

            return Result.Succeeded;
        }
    }
}
