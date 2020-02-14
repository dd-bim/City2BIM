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
        private readonly Document doc;

        public DefinitionFile CreateDefinitionFile(string spFile, Autodesk.Revit.ApplicationServices.Application app, Document doc)
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

                DefinitionGroup defGrp = defFile.Groups.get_Item("XPlanDaten");

                ExternalDefinitionCreationOptions rechtsstandOption = new ExternalDefinitionCreationOptions("Rechtsstand", ParameterType.Text);
                ExternalDefinitionCreationOptions ebeneOption = new ExternalDefinitionCreationOptions("Ebene", ParameterType.Text);
                ExternalDefinitionCreationOptions rechtscharakterOption = new ExternalDefinitionCreationOptions("Rechtscharakter", ParameterType.Text);
                ExternalDefinitionCreationOptions flaechenschlussOption = new ExternalDefinitionCreationOptions("Flaechenschluss", ParameterType.Text);
                ExternalDefinitionCreationOptions nutzungsformOption = new ExternalDefinitionCreationOptions("Nutzungsform", ParameterType.Text);

                Definition rechtsstandDefinition = default(Definition);
                Definition ebeneDefinition = default(Definition);
                Definition rechtscharakterDefinition = default(Definition);
                Definition flaechenschlussDefinition = default(Definition);
                Definition nutzungsformDefinition = default(Definition);

                if (defGrp == null)
                {
                    defGrp = defFile.Groups.Create("XPlanDaten");
                    rechtsstandDefinition = defGrp.Definitions.Create(rechtsstandOption);
                    ebeneDefinition = defGrp.Definitions.Create(ebeneOption);
                    rechtscharakterDefinition = defGrp.Definitions.Create(rechtscharakterOption);
                    flaechenschlussDefinition = defGrp.Definitions.Create(flaechenschlussOption);
                    nutzungsformDefinition = defGrp.Definitions.Create(nutzungsformOption);
                }

                else if (defGrp != null)
                {
                    if (defGrp.Definitions.get_Item("Rechtsstand") != null)
                    {

                    }
                    else if (defGrp.Definitions.get_Item("Rechtsstand") == null)
                    {
                        rechtsstandDefinition = defGrp.Definitions.Create(rechtsstandOption);
                    }
                    if (defGrp.Definitions.get_Item("Ebene") != null)
                    {

                    }
                    else if (defGrp.Definitions.get_Item("Ebene") == null)
                    {
                        ebeneDefinition = defGrp.Definitions.Create(ebeneOption);
                    }
                    if (defGrp.Definitions.get_Item("Rechtscharakter") != null)
                    {

                    }
                    else if (defGrp.Definitions.get_Item("Rechtscharakter") == null)
                    {
                        rechtscharakterDefinition = defGrp.Definitions.Create(rechtscharakterOption);
                    }
                    if (defGrp.Definitions.get_Item("Flaechenschluss") != null)
                    {

                    }
                    else if (defGrp.Definitions.get_Item("Flaechenschluss") == null)
                    {
                        flaechenschlussDefinition = defGrp.Definitions.Create(flaechenschlussOption);
                    }
                    if (defGrp.Definitions.get_Item("Nutzungsform") != null)
                    {

                    }
                    else if (defGrp.Definitions.get_Item("Nutzungsform") == null)
                    {
                        nutzungsformDefinition = defGrp.Definitions.Create(nutzungsformOption);
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
