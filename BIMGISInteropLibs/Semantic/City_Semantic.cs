using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace BIMGISInteropLibs.Semantic
{
    /// <summary>
    /// Set CityGML attributes (fixed and generic)
    /// </summary>
    public static class City_Semantic
    {
        /// <summary>
        /// Read generic attributes (see CityGML Generics-module)
        /// </summary>
        /// <param name="bldgs">all building tags per file</param>
        /// <param name="generics">gen: namespace</param>
        /// <returns>Disctinct list of generic attributes</returns>
        public static HashSet<Xml_AttrRep> ReadGenericAttributes(IEnumerable<XElement> bldgs, Dictionary<string, XNamespace> nsp)
        {
            var genAttrList = new HashSet<Xml_AttrRep>();
            var genAttrNames = new HashSet<string>();
            var genAttrRef = new HashSet<Xml_AttrRep.AttrHierarchy>();

            foreach(var bldg in bldgs)
            {
                var genValues = bldg.Descendants(nsp["gen"] + "value");       //tag for attribute value, needed here only for efficient search

                foreach(var val in genValues)
                {
                    var genAttr = val.Parent;       //parent tag contains the attribute name and its type

                    var attr = new Xml_AttrRep(Xml_AttrRep.AttrNsp.gen, genAttr.Attribute("name").Value, Xml_AttrRep.AttrType.stringAttribute, Xml_AttrRep.AttrHierarchy.bldgCity);

                    var wallGen = genAttr.Ancestors(nsp["bldg"] + "WallSurface");
                    if(wallGen.Any())
                        attr.Reference = Xml_AttrRep.AttrHierarchy.wall;

                    var roofGen = genAttr.Ancestors(nsp["bldg"] + "RoofSurface");
                    if(roofGen.Any())
                        attr.Reference = Xml_AttrRep.AttrHierarchy.roof;

                    var groundGen = genAttr.Ancestors(nsp["bldg"] + "GroundSurface");
                    if(groundGen.Any())
                        attr.Reference = Xml_AttrRep.AttrHierarchy.ground;

                    var closureGen = genAttr.Ancestors(nsp["bldg"] + "ClosureSurface");
                    if(closureGen.Any())
                        attr.Reference = Xml_AttrRep.AttrHierarchy.closure;

                    var floorGen = genAttr.Ancestors(nsp["bldg"] + "OuterFloorSurface");
                    if (floorGen.Any())
                        attr.Reference = Xml_AttrRep.AttrHierarchy.outerFloor;

                    var ceilingGen = genAttr.Ancestors(nsp["bldg"] + "OuterCeilingSurface");
                    if (ceilingGen.Any())
                        attr.Reference = Xml_AttrRep.AttrHierarchy.outerCeiling;

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
        public static HashSet<Xml_AttrRep> GetSchemaAttributes()
        {
            var regAttr = new HashSet<Xml_AttrRep>
            {
                new Xml_AttrRep(Xml_AttrRep.AttrNsp.gml, "name", Xml_AttrRep.AttrType.stringAttribute, Xml_AttrRep.AttrHierarchy.bldgCity)
            };

            //-------------

            //bldg-Modul

            var bldgNames = new Dictionary<string, Xml_AttrRep.AttrType>
            {
                {"Building_ID", Xml_AttrRep.AttrType.stringAttribute },
                {"class", Xml_AttrRep.AttrType.stringAttribute },
                {"function", Xml_AttrRep.AttrType.stringAttribute },
                {"usage", Xml_AttrRep.AttrType.stringAttribute },
                {"yearOfConstruction", Xml_AttrRep.AttrType.intAttribute },
                {"yearOfDemolition", Xml_AttrRep.AttrType.intAttribute },
                {"roofType", Xml_AttrRep.AttrType.stringAttribute },
                {"measuredHeight", Xml_AttrRep.AttrType.measureAttribute },
                {"storeysAboveGround", Xml_AttrRep.AttrType.intAttribute },
                {"storeysBelowGround", Xml_AttrRep.AttrType.intAttribute },
                {"storeysHeightsAboveGround", Xml_AttrRep.AttrType.stringAttribute },
                {"storeysHeightsBelowGround", Xml_AttrRep.AttrType.stringAttribute }
            };

            foreach(var bldg in bldgNames)
            {
                regAttr.Add(new Xml_AttrRep(Xml_AttrRep.AttrNsp.bldg, bldg.Key, bldg.Value, Xml_AttrRep.AttrHierarchy.bldgCity));
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
                regAttr.Add(new Xml_AttrRep(Xml_AttrRep.AttrNsp.xal, entry, Xml_AttrRep.AttrType.stringAttribute, Xml_AttrRep.AttrHierarchy.bldgCity));
            }
            //-----------------
            //core-Modul

            var coreNames = new Dictionary<string, Xml_AttrRep.AttrType>
            {
                {"creationDate", Xml_AttrRep.AttrType.stringAttribute },
                {"terminationDate", Xml_AttrRep.AttrType.stringAttribute },
                {"informationSystem", Xml_AttrRep.AttrType.uriAttribute },
                {"externalObject", Xml_AttrRep.AttrType.stringAttribute },
                {"relativeToTerrain", Xml_AttrRep.AttrType.stringAttribute },
                {"relativeToWater", Xml_AttrRep.AttrType.stringAttribute }
            };

            foreach(var core in coreNames)
            {
                regAttr.Add(new Xml_AttrRep(Xml_AttrRep.AttrNsp.core, core.Key, core.Value, Xml_AttrRep.AttrHierarchy.bldgCity));
            }
            return regAttr;
        }
    }
}