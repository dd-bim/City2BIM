using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Serilog;

namespace City2BIM.GetSemantics
{
    public class ReadSemAttributes
    {
        public HashSet<Attribute> ReadGenericAttributes(IEnumerable<XElement> bldgs, XNamespace generics)
        {
            var genAttrList = new HashSet<Attribute>();

            foreach(var bldg in bldgs)
            {
                var genValues = bldg.Descendants(generics + "value");

                foreach(var val in genValues)
                {
                    var genAttr = val.Parent;

                    var attr = new Attribute("gen", genAttr.Attribute("name").Value, genAttr.Name.LocalName);

                    var genListNames = genAttrList.Select(c => c.Name);

                    if(!genListNames.Contains(attr.Name))
                        genAttrList.Add(attr);
                }
            }

            return genAttrList;
        }

        public HashSet<Attribute> ReadSchemaAttributes(XDocument xsdDoc, string gmlModule, Dictionary<string, XNamespace> nsp)
        {
            var type = "";

            if(gmlModule == "")
                gmlModule = "core";

            var schemaAttr = new HashSet<Attribute>();

            switch(gmlModule)
            {
                case ("bldg"):
                    type = "AbstractBuildingType";
                    schemaAttr.Add(new Attribute(gmlModule, "Building_ID", "stringAttribute"));
                    var addrAttr = CreateAddressAttributes(nsp);
                    schemaAttr.UnionWith(addrAttr);
                    break;

                case (""):
                    goto case "core";

                case ("core"):
                    type = "AbstractCityObjectType";
                    break;

                default:
                    Log.Error("No Building or CityObject was found!");
                    type = "";
                    break;
            }

            var xsdObj = xsdDoc.Descendants().Where(s => s.Name.LocalName == "complexType").Where(t => t.Attribute("name").Value == type);
            var elem = xsdObj.Descendants().Where(s => s.Name.LocalName == "element");

            foreach(var el in elem)
            {
                if(el.Attributes("name").Count() > 0)
                {
                    var typeVal = el.Attribute("type").Value;

                    if(typeVal.Contains("LengthType"))
                    {
                        Attribute attr = new Attribute(gmlModule, el.Attribute("name").Value, "measureAttribute");

                        schemaAttr.Add(attr);
                    }

                    if(typeVal.Contains("Year") || typeVal.Contains("Integer"))
                    {
                        Attribute attr = new Attribute(gmlModule, el.Attribute("name").Value, "intAttribute");

                        schemaAttr.Add(attr);
                    }

                    if(typeVal.Contains("MeasureOrNullList") || typeVal.Contains("date") || typeVal.Contains("CodeType"))
                    {
                        Attribute attr = new Attribute(gmlModule, el.Attribute("name").Value, "stringAttribute");

                        schemaAttr.Add(attr);
                    }

                    if(typeVal.Contains("ExternalReferenceType"))
                    {
                        Attribute attr = new Attribute(gmlModule, "informationSystem", "uriAttribute");
                        Attribute attr2 = new Attribute(gmlModule, "externalObject", "stringAttribute");

                        schemaAttr.Add(attr);
                        schemaAttr.Add(attr2);
                    }

                    if(typeVal.Contains("RelativeToTerrainType") || typeVal.Contains("RelativeToWaterType"))
                    {
                        Attribute attr = new Attribute(gmlModule, el.Attribute("name").Value, "stringAttribute");

                        schemaAttr.Add(attr);
                    }

                    //Log.Information("Name: " + attr.Name + ", Type: " + attr.GmlType);
                }
                else
                    Log.Information("kein name bei " + el.Name.LocalName);
            }

            //var addrAttr = CreateAddressAttributes(nsp);
            //schemaAttr.UnionWith(addrAttr);

            return schemaAttr;
        }

        private HashSet<Attribute> CreateAddressAttributes(Dictionary<string, XNamespace> nsp)
        {
            var addrAttr = new HashSet<Attribute>();

            var type = "stringAttribute";

            var attrNames = new List<string>
            {
                "CountryName",
                "LocalityName",
                "LocalityType",
                "DependentLocalityName",
                "DependentLocalityType",
                "ThoroughfareName",
                "ThoroughfareNumber",
                "ThoroughfareType",
                "PostalCodeNumber"
            };

            foreach(var entry in attrNames)
            {
                addrAttr.Add(new Attribute("xal", entry, type));
            }

            return addrAttr;
        }


    }
}