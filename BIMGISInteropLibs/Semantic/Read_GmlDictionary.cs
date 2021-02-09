/* 
 * Class for reading of GmlDictionaries
 * 
 * - not used in current implementation
 * - could be useful, if "hard-coded" CityGml-Codelist should not be used in future but reading of dictionaries in file system should be supported
 */


//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Xml.Linq;

//namespace City2BIM.Semantic
//{
//    internal class Read_GmlDictionary
//    {
//        public Dictionary<string, string> ReadCodes(string codeListPath)
//        {
//            XDocument xmlCodes = XDocument.Load(codeListPath);

//            var allns = xmlCodes.Root.Attributes().
//                Where(a => a.IsNamespaceDeclaration).
//                GroupBy(a => a.Name.Namespace == XNamespace.None ? String.Empty : a.Name.LocalName,
//                a => XNamespace.Get(a.Value)).
//                ToDictionary(g => g.Key,
//                     g => g.First());

//            allns.TryGetValue("", out var namesp);

//            //Namespace teilweise in AdV-Codelisten für manche Tags nicht gesetzt
//            //Workaround: wenn namesp ohne name (""), dann verwende gml-namespace
//            if(namesp == null)
//                namesp = allns["gml"];

//            var entries = xmlCodes.Descendants(namesp + "dictionaryEntry");

//            Dictionary<string, string> kvpCodes = new Dictionary<string, string>();

//            foreach(var en in entries)
//            {
//                var gmlNames = en.Descendants(allns["gml"] + "name");
//                var gmlDescs = en.Descendants(allns["gml"] + "description");;

//                string code = "", desc = "";

//                if(gmlNames.Count() > 1) //spezielle Implementierung für AdV-Codeliste
//                {
//                    code = gmlNames.Where(c => c.HasAttributes).Select(c => c.Value).SingleOrDefault(); //Tag mit Attributen = Code
//                    desc = gmlNames.Where(c => !c.HasAttributes).Select(c => c.Value).SingleOrDefault();    //Tag ohne Attribute = Description
//                }
//                else if(gmlDescs.Count() == 1 && gmlNames.Count() == 1) //Standard-Dictionary, zB SIG3D
//                {
//                    code = gmlNames.SingleOrDefault().Value;
//                    desc = gmlDescs.SingleOrDefault().Value;
//                }

//                kvpCodes.Add(code, desc);
//            }
//            return kvpCodes;
//        }
//    }
//}