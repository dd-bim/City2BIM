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
        public HashSet<GmlAttribute> ReadGenericAttributes(IEnumerable<XElement> bldgs, Dictionary<string, XNamespace> nsp)
        {
            var genAttrList = new HashSet<GmlAttribute>();
            var genAttrNames = new HashSet<string>();
            var genAttrRef = new HashSet<GmlAttribute.AttrHierarchy>();

            foreach(var bldg in bldgs)
            {
                var genValues = bldg.Descendants(nsp["gen"] + "value");       //tag for attribute value, needed here only for efficient search

                foreach(var val in genValues)
                {
                    var genAttr = val.Parent;       //parent tag contains the attribute name and its type

                    var attr = new GmlAttribute(GmlAttribute.AttrNsp.gen, genAttr.Attribute("name").Value, GmlAttribute.AttrType.stringAttribute, GmlAttribute.AttrHierarchy.bldg);

                    var wallGen = genAttr.Ancestors(nsp["bldg"] + "WallSurface");
                    if(wallGen.Any())
                        attr.Reference = GmlAttribute.AttrHierarchy.wall;

                    var roofGen = genAttr.Ancestors(nsp["bldg"] + "RoofSurface");
                    if(roofGen.Any())
                        attr.Reference = GmlAttribute.AttrHierarchy.roof;

                    var groundGen = genAttr.Ancestors(nsp["bldg"] + "GroundSurface");
                    if(groundGen.Any())
                        attr.Reference = GmlAttribute.AttrHierarchy.ground;

                    var closureGen = genAttr.Ancestors(nsp["bldg"] + "ClosureSurface");
                    if(closureGen.Any())
                        attr.Reference = GmlAttribute.AttrHierarchy.closure;

                    //ggf. weitere Typen prüfen (laut AdV aber nur stringAttribute zulässig) -> TO DO

                    //if(!genListNames.Contains(attr.Name))
                    //{
                    //    genAttrList.Add(attr);                              //add to hashset, if not present
                    //    genAttrListBldg.Add(attr);
                    //}

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

                    //else
                    //{                                                       //duplicate will not be added but handled as surface attribute
                    //                                                        //they occure multiple times because an building contains multiple surfaces
                    //    var duplicate = (from g in genAttrListBldg
                    //                     where g.Name == attr.Name
                    //                     select g).SingleOrDefault();

                    //    if(duplicate != null)
                    //        duplicate.Reference = GmlAttribute.AttrHierarchy.surface;
                    //}
                }
            }

            return genAttrList;
        }

        /// <summary>
        /// Read fixed attributes (CityGML 2.0 standard)
        /// </summary>
        /// <returns>Distinct list of fixed attributes</returns>
        public HashSet<GmlAttribute> GetSchemaAttributes()
        {
            var regAttr = new HashSet<GmlAttribute>();

            //gml:name

            regAttr.Add(new GmlAttribute(GmlAttribute.AttrNsp.gml, "name", GmlAttribute.AttrType.stringAttribute, GmlAttribute.AttrHierarchy.bldg));

            //-------------

            //bldg-Modul

            var bldgNames = new Dictionary<string, GmlAttribute.AttrType>
            {
                {"Building_ID", GmlAttribute.AttrType.stringAttribute },
                {"class", GmlAttribute.AttrType.stringAttribute },
                {"function", GmlAttribute.AttrType.stringAttribute },
                {"usage", GmlAttribute.AttrType.stringAttribute },
                {"yearOfConstruction", GmlAttribute.AttrType.intAttribute },
                {"yearOfDemolition", GmlAttribute.AttrType.intAttribute },
                {"roofType", GmlAttribute.AttrType.stringAttribute },
                {"measuredHeight", GmlAttribute.AttrType.measureAttribute },
                {"storeysAboveGround", GmlAttribute.AttrType.intAttribute },
                {"storeysBelowGround", GmlAttribute.AttrType.intAttribute },
                {"storeysHeightsAboveGround", GmlAttribute.AttrType.stringAttribute },
                {"storeysHeightsBelowGround", GmlAttribute.AttrType.stringAttribute }
            };

            foreach(var bldg in bldgNames)
            {
                regAttr.Add(new GmlAttribute(GmlAttribute.AttrNsp.bldg, bldg.Key, bldg.Value, GmlAttribute.AttrHierarchy.bldg));
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
                regAttr.Add(new GmlAttribute(GmlAttribute.AttrNsp.xal, entry, GmlAttribute.AttrType.stringAttribute, GmlAttribute.AttrHierarchy.bldg));
            }

            //-----------------
            //core-Modul

            var coreNames = new Dictionary<string, GmlAttribute.AttrType>
            {
                {"creationDate", GmlAttribute.AttrType.stringAttribute },
                {"terminationDate", GmlAttribute.AttrType.stringAttribute },
                {"informationSystem", GmlAttribute.AttrType.uriAttribute },
                {"externalObject", GmlAttribute.AttrType.stringAttribute },
                {"relativeToTerrain", GmlAttribute.AttrType.stringAttribute },
                {"relativeToWater", GmlAttribute.AttrType.stringAttribute }
            };

            foreach(var core in coreNames)
            {
                regAttr.Add(new GmlAttribute(GmlAttribute.AttrNsp.core, core.Key, core.Value, GmlAttribute.AttrHierarchy.bldg));
            }

            return regAttr;
        }
    }
}