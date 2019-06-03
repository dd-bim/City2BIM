using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.DB;
using Serilog;
using Attribute = City2BIM.GetSemantics.Attribute;

namespace City2BIM.RevitBuilder
{
    internal class RevitSemanticBuilder
    {
        private Document doc;
        private HashSet<Attribute> attributes;

        public RevitSemanticBuilder(Document doc, HashSet<Attribute> attributes)
        {
            this.doc = doc;
            this.attributes = attributes;
        }

        //nötige RevitAPI-interen Methoden zum Anlegen der SharedParameters sowie Gruppen
        //benötigt Übergaben der Gruppennamen (Vorschlag: CityGML_"namespace", zB CityGML_bldg, CityGML_gen)
        //dazu namespaces aller bldg-descendants übergeben
        //benötigt Parametername sowie Parameterwert

        //Herausforderung: SharedParameterFile muss alle eventuell vorkommenden Parameter beinhalten
        //erfordert Scanning aller Attribute in allen vorkommenden gebäuden
        //erst nacher Füllen pro bldg, wenn Attribut gesetzt

        private DefinitionFile SetAndOpenExternalSharedParamFile(Autodesk.Revit.ApplicationServices.Application application, string sharedParameterFile)
        {
            var filePath = sharedParameterFile.Split('.')[0];

            var newFile = filePath + "_test.txt";

            File.Copy(sharedParameterFile, newFile, true);

            // set the path of shared parameter file to current Revit
            application.SharedParametersFilename = newFile;
            // open the file
            return application.OpenSharedParameterFile();
        }

        public void CreateParameters()
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

                DefinitionGroup parGroupCore = parFile.Groups.get_Item("CityGML-Core Module");
                DefinitionGroup parGroupGen = parFile.Groups.get_Item("CityGML-Generic Module");
                DefinitionGroup parGroupBldg = parFile.Groups.get_Item("CityGML-Building Module");
                DefinitionGroup parGroupAddr = parFile.Groups.get_Item("CityGML-Address Module");
                DefinitionGroup parGroupGML = parFile.Groups.get_Item("CityGML-GML Module");

                //DefinitionGroup parGroup1 = parFile.Groups.get_Item("City Model data");

                if(parGroupCore == null)
                    parGroupCore = parFile.Groups.Create("CityGML-Core Module");

                if(parGroupGen == null)
                    parGroupGen = parFile.Groups.Create("CityGML-Generic Module");

                if(parGroupBldg == null)
                    parGroupBldg = parFile.Groups.Create("CityGML-Building Module");

                if(parGroupAddr == null)
                    parGroupAddr = parFile.Groups.Create("CityGML-Address Module");

                if(parGroupGML == null)
                    parGroupGML = parFile.Groups.Create("CityGML-GML Module");

                Definition parDef = default(Definition);

                foreach(var attribute in attributes)
                {
                    try
                    {

                        var pType = ParameterType.Text;

                        //Typ-Übersetzung für Revit

                        switch(attribute.GmlType)
                        {
                            case (Attribute.AttrType.intAttribute):
                                pType = ParameterType.Integer;
                                break;

                            case (Attribute.AttrType.doubleAttribute):
                                pType = ParameterType.Number;
                                break;

                            case (Attribute.AttrType.uriAttribute):
                                pType = ParameterType.URL;
                                break;

                            case (Attribute.AttrType.measureAttribute):
                                pType = ParameterType.Length;
                                break;

                            default: //stringAttribute, dateAttribute: ParameterType.Text;
                                break;
                        }

                        //Gruppenzuordnung für Revit

                        switch(attribute.GmlNamespace)
                        {
                            case (Attribute.AttrNsp.gen):
                                SetDefinitionsToGroup(parGroupGen, attribute, pType, assocCats, parDef);
                                break;

                            case (Attribute.AttrNsp.core):
                                SetDefinitionsToGroup(parGroupCore, attribute, pType, assocCats, parDef);
                                break;

                            case (Attribute.AttrNsp.bldg):
                                SetDefinitionsToGroup(parGroupBldg, attribute, pType, assocCats, parDef);
                                break;

                            case (Attribute.AttrNsp.xal):
                                SetDefinitionsToGroup(parGroupAddr, attribute, pType, assocCats, parDef);
                                break;

                            case (Attribute.AttrNsp.gml):
                                SetDefinitionsToGroup(parGroupGML, attribute, pType, assocCats, parDef);
                                break;
                        }
                        //ExternalDefinitionCreationOptions extDef = new ExternalDefinitionCreationOptions(attribute.GmlNamespace + ": " + attribute.Name, pType);

                        //// create an instance definition in definition group MyParameters

                        //extDef.UserModifiable = true;      //nicht modifizierbar?! nur zum Lesen der CityGML oder auch editierbar für IFC-Property-Export

                        //// Set tooltip
                        //extDef.Description = attribute.Description + i;  //später Übersetzungen der Codes als Description

                        //Definition parDef = default(Definition);

                        //parDef = parGroupGen.Definitions.Create(extDef);

                        //ExternalDefinition yoc = parGroup1.Definitions.get_Item(attribute.Name) as ExternalDefinition;

                        ////Create an instance of InstanceBinding
                        //InstanceBinding instanceBinding = doc.Application.Create.NewInstanceBinding(assocCats);

                        //doc.ParameterBindings.Insert(yoc, instanceBinding, BuiltInParameterGroup.PG_DATA);  //Parameter-Gruppe Daten ok?
                    }
                    catch(Exception ex)
                    {
                        Log.Error("Error while attributing: " + attribute.Name + "message: " + ex.Message);
                    }
                }

                tParam.Commit();
            }
        }

        private void SetDefinitionsToGroup(DefinitionGroup parGroup, Attribute attribute, ParameterType pType, CategorySet assocCats, Definition parDef)
        {
            ExternalDefinitionCreationOptions extDef = new ExternalDefinitionCreationOptions(attribute.GmlNamespace + ": " + attribute.Name, pType);

            // create an instance definition in definition group MyParameters

            extDef.UserModifiable = true;      //nicht modifizierbar?! nur zum Lesen der CityGML oder auch editierbar für IFC-Property-Export

            // Set tooltip
            //extDef.Description = attribute.Description + i;  //später Übersetzungen der Codes als Description

            //Definition parDef = default(Definition);

            parDef = parGroup.Definitions.Create(extDef);

            ExternalDefinition yoc = parGroup.Definitions.get_Item(attribute.GmlNamespace + ": " + attribute.Name) as ExternalDefinition;

            //Create an instance of InstanceBinding
            InstanceBinding instanceBinding = doc.Application.Create.NewInstanceBinding(assocCats);

            doc.ParameterBindings.Insert(yoc, instanceBinding, BuiltInParameterGroup.PG_DATA);  //Parameter-Gruppe Daten ok?
        }
    }
}