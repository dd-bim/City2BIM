using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace City2BIM.GetSemantics
{
    internal class ReadSemValues
    {
        public Dictionary<Attribute, string> ReadAttributeValues(XElement bldgEl, HashSet<Attribute> attributes, Dictionary<string, XNamespace> nsp)
        {
            var bldgParts = bldgEl.Elements(nsp["bldg"] + "consistsOfBuildingPart");
            var bldg = bldgEl.Elements().Except(bldgParts);

            var kvp = new Dictionary<Attribute, string>();

            foreach(var attr in attributes)
            {
                //values im tag eingeschlossen:

                var matchAttr = bldg.DescendantsAndSelf().Where(n => n.Name.LocalName == attr.Name).ToList();
                //ar match = bldg.Descendants().Where(n => n.Name.LocalName == attr.Name).FirstOrDefault();

                //generische Attribute:

                if(attr.GmlNamespace == Attribute.AttrNsp.gen)
                {
                    matchAttr = bldg.DescendantsAndSelf().Where(n => n.Name.LocalName == "value" && n.Parent.Attribute("name").Value == attr.Name).ToList();
                }

                if(attr.GmlNamespace == Attribute.AttrNsp.gml)
                {
                    matchAttr = bldg.DescendantsAndSelf().Where(n => n.Name.LocalName == attr.Name && n.Name.Namespace == nsp["gml"]).ToList();
                }

                if(matchAttr != null && matchAttr.Count() == 1)
                {
                    kvp.Add(attr, matchAttr[0].Value.Trim());
                }
                else if(matchAttr.Count > 1)
                {
                    var valList = new List<string>();

                    foreach(var m in matchAttr)
                    {
                        valList.Add(m.Value.Trim());
                    }

                    var distList = valList.Distinct().ToList();

                    if(distList.Count() == 1)
                    {
                        kvp.Add(attr, distList[0]);
                    }
                    else
                    {
                        kvp.Add(attr, distList.Where(c => !c.Equals("")).First());
                    }
                }
                else
                {
                    switch(attr.Name)
                    {
                        case ("LocalityType"):
                            var matchElem = bldg.DescendantsAndSelf().Where(n => n.Name.LocalName == "Locality").FirstOrDefault();
                            if(matchElem != null)
                            {
                                kvp.Add(attr, AddTypeValues(matchElem));
                            }
                            //else
                            //    kvp.Add(attr, null);
                            break;

                        case ("DependentLocalityType"):
                            var matchElem2 = bldg.DescendantsAndSelf().Where(n => n.Name.LocalName == "DependentLocality").FirstOrDefault();
                            if(matchElem2 != null)
                            {
                                kvp.Add(attr, AddTypeValues(matchElem2));
                            }
                            //else
                            //    kvp.Add(attr, null);
                            break;

                        case ("ThoroughfareType"):
                            var matchElem3 = bldg.DescendantsAndSelf().Where(n => n.Name.LocalName == "Thoroughfare").FirstOrDefault();
                            if(matchElem3 != null)
                            {
                                kvp.Add(attr, AddTypeValues(matchElem3));
                            }
                            //else
                            //    kvp.Add(attr, null);
                            break;

                        case ("Building_ID"):
                            var id = bldgEl.Attribute(nsp["gml"] + "id").Value;
                            if(id != null)
                                kvp.Add(attr, id);
                            //else
                            //    kvp.Add(attr, null);
                            break;

                        default:
                            //kvp.Add(attr, null);
                            break;
                    }
                }

                //Attribute, die als Type vorliegen:

                //foreach(var kp in kvp)
                //{
                //    Log.Information("Name: " + kp.Key + ", Value: " + kp.Value);
                //}
            }
            return kvp;
        }

        private void GetMatchAttrVal(Attribute attr, List<XElement> matchAttr)
        {
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