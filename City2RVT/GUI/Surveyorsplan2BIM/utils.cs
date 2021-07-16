using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using System.IO;

namespace City2RVT.GUI.Surveyorsplan2BIM
{
    /// <summary>
    /// util collection to support import for surveyorsplan
    /// </summary>
    public class utils
    {
        /// <summary>
        /// import family
        /// </summary>
        /// <param name="absFilePath"></param>
        /// <param name="doc"></param>
        public static Family importFamily(string storagePath, string familyName, Document doc)
        {
            //without *.rfa
            string famName = Path.GetFileNameWithoutExtension(familyName);

            //check if family is allready in document
            Family family = utils.findFamilyViaName(doc, famName);
            
            //if family has been found this will be used
            if(family != null)
            {
                return family;
            }
            //import family to document
            else
            {
                //read family as transaction (otherwise: famliy = null)
                using (Transaction t = new Transaction(doc))
                {
                    //start transaction
                    t.Start("Load family: " + familyName);

                    //get absoult path
                    var absPath = Path.Combine(storagePath, familyName);

                    try
                    {
                        //load via absoult path
                        doc.LoadFamily(absPath, out family);

                        //commit transaction (family will be loaded)
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        //return error message to user
                        TaskDialog.Show("Family loading", ex.Message, TaskDialogCommonButtons.Ok);

                        //rollback transaction family loading failed
                        t.RollBack();
                    }
                }
                return family;
            }
        }

        /// <summary>
        /// find family via family name <para/>
        /// Hint: does not need to be in a transaction!
        /// </summary>
        /// <param name="doc">current revit doc</param>
        /// <param name="familyName">name of the family (for selection)</param>
        /// <returns></returns>
        public static Family findFamilyViaName(Document doc, string familyName)
        {
            //init collector
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            //set filter via input (targetType
            collector = collector.OfClass(typeof(Family));

            //get family 
            Family family = collector.FirstOrDefault<Element>(el => el.Name.Equals(familyName)) as Family;

            if(family != null)
            {
                //return family
                return family;
            }
            else
            {
                //no family found (can be used for request)
                return null;
            }
        }

        public class FParameterData
        {
            public string Family { get; set; }
            public string BuiltinParameter { get; set; }
            public string ParameterType { get; set; }
            public string ParameterName { get; set; }
            public string ParameterGroup { get; set; }
            public string BuiltinGroup { get; set; }

            public static FParameterData GetParameterData(FamilyParameter familyparam, Document doc)
            {
                FParameterData parameterdata = new FParameterData
                {
                    Family = Path.GetFileNameWithoutExtension(doc.Title.ToString()),
                    ParameterName = familyparam.Definition.Name,
                    BuiltinParameter = ((InternalDefinition)familyparam.Definition).BuiltInParameter.ToString(),
                    ParameterGroup = LabelUtils.GetLabelFor(familyparam.Definition.ParameterGroup),
                    BuiltinGroup = familyparam.Definition.ParameterGroup.ToString(),
                    ParameterType = familyparam.Definition.ParameterType.ToString(),
                };

                return parameterdata;
            }
        }

        public static List<FParameterData> readFamilyParameterInfo(Application app, string absPath)
        {
            //open revit family file in a separate document
            var doc = app.OpenDocumentFile(absPath);

            /// Get the familyManager instance from the open document
            var familyManager = doc.FamilyManager;
            
            //int totalParams = familyManager.Parameters.Size;

            List<FParameterData> ParametersData = new List<FParameterData>();

            foreach (FamilyParameter familyParameter in familyManager.Parameters)
            {
                /// Add Parameter Data into a list
                ParametersData.Add(FParameterData.GetParameterData(familyParameter, doc));
            }

            return ParametersData;
        }

        public class mappingList
        {
            public Guid mappingId { get; set; }

            public string dxfName { get; set; }

            public string familyName { get; set; } = null;

            public Dictionary<string, string> parameterMap { get; set; }

            public static mappingList setMappingPair(string dxfName, string familyName)
            {
                mappingList mappingList = new mappingList
                {
                    mappingId = Guid.NewGuid(),
                    dxfName = dxfName,
                    familyName = familyName,
                };

                return mappingList;
            }
        }

        
    }
}
