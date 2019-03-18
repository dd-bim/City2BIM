using Autodesk.Revit.DB;

namespace City2BIM.RevitBuilder
{
    internal class RevitSemanticBuilder
    {
        private DefinitionFile SetAndOpenExternalSharedParamFile(Autodesk.Revit.ApplicationServices.Application application, string sharedParameterFile)
        {
            // set the path of shared parameter file to current Revit
            application.SharedParametersFilename = sharedParameterFile;
            // open the file
            return application.OpenSharedParameterFile();
        }

        public void CreateParameters(Document doc)
        {
            Category cat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Entourage);
            CategorySet assocCats = doc.Application.Create.NewCategorySet();
            assocCats.Insert(cat);

            //zunächst wird der Fall betrachtet, dass nur leere Shared Parameter Files vorliegen (leer = "nur" Revit-Tabellenkopf)
            using(Transaction tParam = new Transaction(doc, "Insert Parameter"))
            {
                tParam.Start();

                //opened the txt-file and set it to shared parameter file
                var parFile = SetAndOpenExternalSharedParamFile(doc.Application, @"D:\1_CityBIM\1_Programmierung\City2BIM\CityGML_Data\SharedParameterFile.txt");
                //Testdefinitionen (Hierarchie? evtl. nützlich)

                DefinitionGroup parGroup1 = parFile.Groups.get_Item("City Model data");
                Definition parYOC = default(Definition);
                ExternalDefinitionCreationOptions optYOC = new ExternalDefinitionCreationOptions("Year of Construction", ParameterType.Integer);

                if(parGroup1 == null)
                {
                    parGroup1 = parFile.Groups.Create("City Model data");
                    parYOC = parGroup1.Definitions.Create(optYOC);
                }

                // create an instance definition in definition group MyParameters

                optYOC.UserModifiable = false;

                // Set tooltip
                optYOC.Description = "Baujahr";

                ExternalDefinition yoc = parGroup1.Definitions.get_Item("Year of Construction") as ExternalDefinition;

                //Create an instance of InstanceBinding
                InstanceBinding instanceBinding = doc.Application.Create.NewInstanceBinding(assocCats);

                doc.ParameterBindings.Insert(yoc, instanceBinding, BuiltInParameterGroup.PG_DATA);

                tParam.Commit();
            }
        }
    }
}