using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Serilog;

namespace City2BIM.GetSemantics
{
    public class ReadSemData
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
                    Log.Information("Observing bldg-xsd.");
                    schemaAttr.Add(new Attribute(gmlModule, "Building_ID", "stringAttribute"));
                    var addrAttr = CreateAddressAttributes(nsp);
                    schemaAttr.UnionWith(addrAttr);
                    break;

                case (""):
                    goto case "core";

                case ("core"):
                    type = "AbstractCityObjectType";
                    Log.Information("Observing core-xsd.");
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

                    if(typeVal.Contains("Year") || typeVal.Contains("CodeType") || typeVal.Contains("Integer"))
                    {
                        Attribute attr = new Attribute(gmlModule, el.Attribute("name").Value, "intAttribute");

                        schemaAttr.Add(attr);
                    }

                    if(typeVal.Contains("MeasureOrNullList") || typeVal.Contains("date"))
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

        public Dictionary<Attribute, string> ReadAttributeValues(XElement bldg, HashSet<Attribute> attributes, Dictionary<string, XNamespace> nsp)
        {
            var kvp = new Dictionary<Attribute, string>();
            
            foreach(var attr in attributes)
            {
                //values im tag eingeschlossen:

                var match = bldg.Descendants().Where(n => n.Name.LocalName == attr.Name).FirstOrDefault();

                //generische Attribute:

                if(attr.GmlNamespace == "gen")
                {
                    match = bldg.Descendants().Where(n => n.Name.LocalName == "value" && n.Parent.Attribute("name").Value == attr.Name).FirstOrDefault();
                }

                if(match != null)
                {
                    kvp.Add(attr, match.Value);
                }
                else
                {
                    //Attribute, die als Type vorliegen:

                    switch(attr.Name)
                    {
                        case ("LocalityType"):
                            var matchElem = bldg.Descendants().Where(n => n.Name.LocalName == "Locality").FirstOrDefault();
                            if(matchElem != null)
                            {
                                kvp.Add(attr, AddTypeValues(matchElem));
                            }
                            else
                                kvp.Add(attr, null);
                            break;

                        case ("DependentLocalityType"):
                            var matchElem2 = bldg.Descendants().Where(n => n.Name.LocalName == "DependentLocality").FirstOrDefault();
                            if(matchElem2 != null)
                            {
                                kvp.Add(attr, AddTypeValues(matchElem2));
                            }
                            else
                                kvp.Add(attr, null);
                            break;

                        case ("ThoroughfareType"):
                            var matchElem3 = bldg.Descendants().Where(n => n.Name.LocalName == "Thoroughfare").FirstOrDefault();
                            if(matchElem3 != null)
                            {
                                kvp.Add(attr, AddTypeValues(matchElem3));
                            }
                            else
                                kvp.Add(attr, null);
                            break;

                        case ("Building_ID"):
                            var id = bldg.Attribute(nsp["gml"]+"id").Value;
                            if (id != null)
                                kvp.Add(attr, id);
                            else
                                kvp.Add(attr, null);
                            break;

                        default:
                            kvp.Add(attr, null);
                            break;
                    }
                }

                //foreach(var kp in kvp)
                //{
                //    Log.Information("Name: " + kp.Key + ", Value: " + kp.Value);
                //}
            }
            return kvp;
        }

        private string AddTypeValues(XElement matchElem)
        {
            var matchAttr = matchElem.Attribute("Type");

            if(matchAttr != null)
            {
                return matchAttr.Value;
            }
            else
                return null;
        }
    }
}