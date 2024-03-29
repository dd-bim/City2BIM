﻿using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using BIMGISInteropLibs.CityGML;

namespace BIMGISInteropLibs.Semantic
{
    internal class ReadSemValues
    {
        public Dictionary<Xml_AttrRep, string> ReadAttributeValuesBldg(XElement bldgEl, HashSet<Xml_AttrRep> attributes, Dictionary<string, XNamespace> nsp)
        {
            var bldgAttributes = attributes.Where(a => a.Reference == Xml_AttrRep.AttrHierarchy.bldgCity);

            var bldgParts = bldgEl.Elements(nsp["bldg"] + "consistsOfBuildingPart");
            var bldg = bldgEl.Elements().Except(bldgParts);

            var kvp = new Dictionary<Xml_AttrRep, string>();

            foreach(var attr in bldgAttributes)
            {
                //values im tag eingeschlossen:

                var matchAttr = bldg.DescendantsAndSelf().Where(n => n.Name.LocalName == attr.Name).ToList();
                //ar match = bldg.Descendants().Where(n => n.Name.LocalName == attr.Name).FirstOrDefault();

                //generische Attribute:

                if(attr.XmlNamespace == Xml_AttrRep.AttrNsp.gen)
                {
                    matchAttr = bldg.DescendantsAndSelf().Where(n => n.Name.LocalName == "value" && n.Parent.Attribute("name").Value == attr.Name).ToList();
                }

                if(attr.XmlNamespace == Xml_AttrRep.AttrNsp.gml)
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
                            break;

                        case ("DependentLocalityType"):
                            var matchElem2 = bldg.DescendantsAndSelf().Where(n => n.Name.LocalName == "DependentLocality").FirstOrDefault();
                            if(matchElem2 != null)
                            {
                                kvp.Add(attr, AddTypeValues(matchElem2));
                            }
                            break;

                        case ("ThoroughfareType"):
                            var matchElem3 = bldg.DescendantsAndSelf().Where(n => n.Name.LocalName == "Thoroughfare").FirstOrDefault();
                            if(matchElem3 != null)
                            {
                                kvp.Add(attr, AddTypeValues(matchElem3));
                            }
                            break;

                        case ("Building_ID"):
                            var id = bldgEl.Attribute(nsp["gml"] + "id").Value;
                            if(id != null)
                                kvp.Add(attr, id);
                            break;

                        default:
                            break;
                    }
                }
            }
            return kvp;
        }

        public Dictionary<Xml_AttrRep, string> ReadAttributeValuesSurface(XElement surfaceEl, HashSet<Xml_AttrRep> attributes, CityGml_Surface.FaceType type)
        {
            var kvp = new Dictionary<Xml_AttrRep, string>();

            var surfaceAttributes = attributes.Where(a => a.Reference.ToString() == type.ToString());

            foreach(var attr in surfaceAttributes)
            {
                //generische Attribute:

                var matchAttr = surfaceEl.Descendants().Where(n => n.Name.LocalName == attr.Name).ToList();

                matchAttr = surfaceEl.Descendants().Where(n => n.Name.LocalName == "value" && n.Parent.Attribute("name").Value == attr.Name).ToList();

                kvp.Add(attr, matchAttr[0].Value.Trim());
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