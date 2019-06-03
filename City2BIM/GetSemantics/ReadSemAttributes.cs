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

                    var attr = new Attribute(Attribute.AttrNsp.gen, genAttr.Attribute("name").Value, Attribute.AttrType.stringAttribute);
                    //ggf. weitere Typen prüfen (laut AdV aber nur stringAttribute zulässig)

                    var genListNames = genAttrList.Select(c => c.Name);

                    if(!genListNames.Contains(attr.Name))
                        genAttrList.Add(attr);
                }
            }

            return genAttrList;
        }

        public HashSet<Attribute> GetSchemaAttributes()
        {
            var regAttr = new HashSet<Attribute>();

            //gml:name

            regAttr.Add(new Attribute(Attribute.AttrNsp.gml, "name", Attribute.AttrType.stringAttribute));

            //-------------

            //bldg-Modul

            var bldgNames = new Dictionary<string, Attribute.AttrType>
            {
                {"Building_ID", Attribute.AttrType.stringAttribute },
                {"class", Attribute.AttrType.stringAttribute },
                {"function", Attribute.AttrType.stringAttribute },
                {"usage", Attribute.AttrType.stringAttribute },
                {"yearOfConstruction", Attribute.AttrType.intAttribute },
                {"yearOfDemolition", Attribute.AttrType.intAttribute },
                {"roofType", Attribute.AttrType.stringAttribute },
                {"measuredHeight", Attribute.AttrType.measureAttribute },
                {"storeysAboveGround", Attribute.AttrType.intAttribute },
                {"storeysBelowGround", Attribute.AttrType.intAttribute },
                {"storeysHeightsAboveGround", Attribute.AttrType.stringAttribute },
                {"storeysHeightsBelowGround", Attribute.AttrType.stringAttribute }
            };

            foreach(var bldg in bldgNames)
            {
                regAttr.Add(new Attribute(Attribute.AttrNsp.bldg, bldg.Key, bldg.Value));
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
                regAttr.Add(new Attribute(Attribute.AttrNsp.xal, entry, Attribute.AttrType.stringAttribute));
            }

            //-----------------
            //core-Modul

            var coreNames = new Dictionary<string, Attribute.AttrType>
            {
                {"creationDate", Attribute.AttrType.stringAttribute },
                {"terminationDate", Attribute.AttrType.stringAttribute },
                {"informationSystem", Attribute.AttrType.uriAttribute },
                {"externalObject", Attribute.AttrType.stringAttribute },
                {"relativeToTerrain", Attribute.AttrType.stringAttribute },
                {"relativeToWater", Attribute.AttrType.stringAttribute }
            };

            foreach(var core in coreNames)
            {
                regAttr.Add(new Attribute(Attribute.AttrNsp.core, core.Key, core.Value));
            }

            return regAttr;
        }
    }
}