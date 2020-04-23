using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using City2BIM.Semantic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using City2RVT.Calc;
using System.Xml;


namespace City2RVT.GUI.XPlan2BIM
{
    public class XPlan_Parameter
    {
        //private readonly Document doc;

        /// <summary>
        /// Creates Definition file for shared parameters. 
        /// </summary>
        public DefinitionFile CreateDefinitionFile(string spFile, Autodesk.Revit.ApplicationServices.Application app, Document doc, string paramName, string defFileGroup)
        {
            DefinitionFile defFile = app.OpenSharedParameterFile();

            Transaction tCreateSpFile = new Transaction(doc, "Create Shared Parameter File");
            {
                tCreateSpFile.Start();

                try
                {
                    app.SharedParametersFilename = spFile;
                }
                catch (Exception)
                {
                    MessageBox.Show("No Shared Parameter File found");
                }

                DefinitionGroup defGrp = defFile.Groups.get_Item(defFileGroup);

                ExternalDefinitionCreationOptions externalDefinitionOption = new ExternalDefinitionCreationOptions(paramName, ParameterType.Text);
                Definition definition = default(Definition);

                if (defGrp == null)
                {
                    defGrp = defFile.Groups.Create(defFileGroup);
                   definition = defGrp.Definitions.Create(externalDefinitionOption);
                }

                else if (defGrp != null)
                {
                    if (defGrp.Definitions.get_Item(paramName) != null)
                    {

                    }
                    else if (defGrp.Definitions.get_Item(paramName) == null)
                    {
                        definition = defGrp.Definitions.Create(externalDefinitionOption);
                    }                    
                }
            }
            tCreateSpFile.Commit();

            return defFile;
        }

        public string getNodeText(XmlNode nodeExt, XmlNamespaceManager nsmgr, string xPlanObject, string nodeName)
        {
            string nodeText = default(string);

            XmlNode rechtsstand = default(XmlNode);

            rechtsstand = nodeExt.SelectSingleNode("//gml:featureMember/" + xPlanObject + "//xplan:" + nodeName, nsmgr);

            if (rechtsstand != null)
            {
                nodeText = rechtsstand.InnerText;
            }
            else
            {
                nodeText = "-";
            }

            return nodeText;
        }
    }
}
