using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace City2BIM.GetSemantics
{
    /// <summary>
    /// Set CityGML attributes (fixed and generic)
    /// </summary>
    public class ReadSemAttributes
    {
        /// <summary>
        /// Read generic attributes (see CityGML Generics-module)
        /// </summary>
        /// <param name="bldgs">all building tags per file</param>
        /// <param name="generics">gen: namespace</param>
        /// <returns>Disctinct list of generic attributes</returns>
        public HashSet<XmlAttribute> ReadGenericAttributes(IEnumerable<XElement> bldgs, Dictionary<string, XNamespace> nsp)
        {
            var genAttrList = new HashSet<XmlAttribute>();
            var genAttrNames = new HashSet<string>();
            var genAttrRef = new HashSet<XmlAttribute.AttrHierarchy>();

            foreach(var bldg in bldgs)
            {
                var genValues = bldg.Descendants(nsp["gen"] + "value");       //tag for attribute value, needed here only for efficient search

                foreach(var val in genValues)
                {
                    var genAttr = val.Parent;       //parent tag contains the attribute name and its type

                    var attr = new XmlAttribute(XmlAttribute.AttrNsp.gen, genAttr.Attribute("name").Value, XmlAttribute.AttrType.stringAttribute, XmlAttribute.AttrHierarchy.bldgCity);

                    var wallGen = genAttr.Ancestors(nsp["bldg"] + "WallSurface");
                    if(wallGen.Any())
                        attr.Reference = XmlAttribute.AttrHierarchy.wall;

                    var roofGen = genAttr.Ancestors(nsp["bldg"] + "RoofSurface");
                    if(roofGen.Any())
                        attr.Reference = XmlAttribute.AttrHierarchy.roof;

                    var groundGen = genAttr.Ancestors(nsp["bldg"] + "GroundSurface");
                    if(groundGen.Any())
                        attr.Reference = XmlAttribute.AttrHierarchy.ground;

                    var closureGen = genAttr.Ancestors(nsp["bldg"] + "ClosureSurface");
                    if(closureGen.Any())
                        attr.Reference = XmlAttribute.AttrHierarchy.closure;

                    var floorGen = genAttr.Ancestors(nsp["bldg"] + "OuterFloorSurface");
                    if (floorGen.Any())
                        attr.Reference = XmlAttribute.AttrHierarchy.outerFloor;

                    var ceilingGen = genAttr.Ancestors(nsp["bldg"] + "OuterCeilingSurface");
                    if (ceilingGen.Any())
                        attr.Reference = XmlAttribute.AttrHierarchy.outerCeiling;

                    var names = attr.Name;
                    var references = attr.Reference;

                    bool refn = genAttrRef.Contains(attr.Reference);
                    bool name = genAttrNames.Contains(attr.Name);

                    if(!refn)
                    {
                        genAttrList.Add(attr);
                        genAttrNames.Add(attr.Name);
                        genAttrRef.Add(attr.Reference);
                    }
                    else
                    {
                        var attrRef = from g in genAttrList
                                      where g.Reference == attr.Reference
                                      select g.Name;

                        if(!attrRef.Contains(attr.Name))
                        {
                            genAttrList.Add(attr);
                            genAttrNames.Add(attr.Name);
                            genAttrRef.Add(attr.Reference);
                        }
                    }
                }
            }

            return genAttrList;
        }

        /// <summary>
        /// Read fixed attributes (CityGML 2.0 standard)
        /// </summary>
        /// <returns>Distinct list of fixed attributes</returns>
        public HashSet<XmlAttribute> GetSchemaAttributes()
        {
            var regAttr = new HashSet<XmlAttribute>();
            regAttr.Add(new XmlAttribute(XmlAttribute.AttrNsp.gml, "name", XmlAttribute.AttrType.stringAttribute, XmlAttribute.AttrHierarchy.bldgCity));

            //-------------

            //bldg-Modul

            var bldgNames = new Dictionary<string, XmlAttribute.AttrType>
            {
                {"Building_ID", XmlAttribute.AttrType.stringAttribute },
                {"class", XmlAttribute.AttrType.stringAttribute },
                {"function", XmlAttribute.AttrType.stringAttribute },
                {"usage", XmlAttribute.AttrType.stringAttribute },
                {"yearOfConstruction", XmlAttribute.AttrType.intAttribute },
                {"yearOfDemolition", XmlAttribute.AttrType.intAttribute },
                {"roofType", XmlAttribute.AttrType.stringAttribute },
                {"measuredHeight", XmlAttribute.AttrType.measureAttribute },
                {"storeysAboveGround", XmlAttribute.AttrType.intAttribute },
                {"storeysBelowGround", XmlAttribute.AttrType.intAttribute },
                {"storeysHeightsAboveGround", XmlAttribute.AttrType.stringAttribute },
                {"storeysHeightsBelowGround", XmlAttribute.AttrType.stringAttribute }
            };

            foreach(var bldg in bldgNames)
            {
                regAttr.Add(new XmlAttribute(XmlAttribute.AttrNsp.bldg, bldg.Key, bldg.Value, XmlAttribute.AttrHierarchy.bldgCity));
            }

            //----------------------
            //xAL (Adressen)

            var xalNames = new List<string>
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

            foreach(var entry in xalNames)
            {
                regAttr.Add(new XmlAttribute(XmlAttribute.AttrNsp.xal, entry, XmlAttribute.AttrType.stringAttribute, XmlAttribute.AttrHierarchy.bldgCity));
            }
            //-----------------
            //core-Modul

            var coreNames = new Dictionary<string, XmlAttribute.AttrType>
            {
                {"creationDate", XmlAttribute.AttrType.stringAttribute },
                {"terminationDate", XmlAttribute.AttrType.stringAttribute },
                {"informationSystem", XmlAttribute.AttrType.uriAttribute },
                {"externalObject", XmlAttribute.AttrType.stringAttribute },
                {"relativeToTerrain", XmlAttribute.AttrType.stringAttribute },
                {"relativeToWater", XmlAttribute.AttrType.stringAttribute }
            };

            foreach(var core in coreNames)
            {
                regAttr.Add(new XmlAttribute(XmlAttribute.AttrNsp.core, core.Key, core.Value, XmlAttribute.AttrHierarchy.bldgCity));
            }
            return regAttr;
        }
    }
}