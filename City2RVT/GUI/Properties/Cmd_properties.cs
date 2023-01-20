using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.ExtensibleStorage;
using Serilog;

using CommonRevit.Semantics;

namespace CityBIM.GUI.Properties
{
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Cmd_properties : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            Prop_GeoRefSettings.SetInitialSettings(doc);

            try
            {
                Selection sel = uiDoc.Selection;
                ICollection<ElementId> selectedIds = sel.GetElementIds();

                if (selectedIds.Count != 1)
                {
                    Log.Warning("ManageProperties: to much or to less elements were selected. nr of selected elements: " + selectedIds.Count);
                    TaskDialog.Show("Hint", "Please only select one element for which custom attribtes should be shown!");
                }
                else
                {
                    var firstElemId = selectedIds.First();
                    Element pickedElement = doc.GetElement(firstElemId);

                    var schemaGUIDS = pickedElement.GetEntitySchemaGuids();

                    Dictionary<string, Dictionary<string, string>> schemaAndAttrDict = new Dictionary<string, Dictionary<string, string>>();

                    foreach (var schemaGUID in schemaGUIDS)
                    {
                        var currentSchema = Schema.Lookup(schemaGUID);
                        if (currentSchema.SchemaName == "ExternalDataCatalogSchema")
                        {
                            continue;
                        }

                        Entity ent = pickedElement.GetEntity(currentSchema);

                        Dictionary<string, string> schemaAttributes = new Dictionary<string, string>();
                        foreach (var currentField in currentSchema.ListFields())
                        {
                            schemaAttributes.Add(currentField.FieldName, ent.Get<string>(currentField));
                        }
                        schemaAndAttrDict.Add(currentSchema.SchemaName, schemaAttributes);
                    }

                    var propUI = new PropertyWindow(schemaAndAttrDict);

                    propUI.ShowDialog();

                    if (propUI.saveChanges)
                    {
                        using (Transaction trans = new Transaction(doc, "Update Schema Information"))
                        {
                            trans.Start();
                            var modified = propUI.data;
                            foreach (var schemaGUID in schemaGUIDS)
                            {
                                var currentSchema = Schema.Lookup(schemaGUID);
                                Entity ent = pickedElement.GetEntity(currentSchema);

                                ObservableCollection<AttributeContainer> currentCollection = modified[currentSchema.SchemaName];
                                var fieldList = currentSchema.ListFields();

                                foreach (AttributeContainer attrCont in currentCollection)
                                {
                                    var currentField = from field in fieldList
                                                       where field.FieldName.Equals(attrCont.attrName)
                                                       select field;

                                    ent.Set<string>(currentField.FirstOrDefault(), attrCont.attrValue);
                                }

                                pickedElement.SetEntity(ent);
                            }
                            trans.Commit();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TaskDialog.Show("Exception", e.ToString());
                return Result.Failed;
            }

            return Result.Succeeded;

        }
    }
}
