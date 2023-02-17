using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Xml_AttrRep = BIMGISInteropLibs.Semantic.Xml_AttrRep;

namespace CityBIM.Builder
{
    internal class Revit_Semantic
    {
        private readonly Document doc;
        private readonly string userDefinedParameterFile;

        public Revit_Semantic(Document doc)
        {
            this.doc = doc;
            this.userDefinedParameterFile = doc.Application.SharedParametersFilename;
        }

        private Category GetCategory(BuiltInCategory builInCat)
        {
            return doc.Settings.Categories.get_Item(builInCat);
        }

        private CategorySet GetCategorySet(List<BuiltInCategory> builtInCats)
        {
            CategorySet catSet = new CategorySet();

            foreach (BuiltInCategory builtInCat in builtInCats)
            {
                catSet.Insert(GetCategory(builtInCat));
            }

            return catSet;
        }           
    }
}