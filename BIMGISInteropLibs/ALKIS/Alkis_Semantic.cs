using BIMGISInteropLibs.Semantic;
using System.Collections.Generic;

namespace BIMGISInteropLibs.Alkis
{
    /// <summary>
    /// Class for set up of parcel attributes (source: ALKIS schema)
    /// </summary>
    public static class Alkis_Semantic
    {
        /// <summary>
        /// Gets the ALKIS attributes (could be enhanced for other themes like usage...)
        /// </summary>
        /// <returns>HashSet with Xml-Attributes (namespace, name, type, theme)</returns>
        public static HashSet<Xml_AttrRep> GetParcelAttributes()
        {
            var regAttr = new HashSet<Xml_AttrRep>();

            var parcelData = new Dictionary<string, Xml_AttrRep.AttrType>
            {
                {"gemarkung_Land", Xml_AttrRep.AttrType.stringAttribute },
                {"gemarkung_Nummer", Xml_AttrRep.AttrType.stringAttribute },
                {"flurstuecksnummer", Xml_AttrRep.AttrType.stringAttribute },
                {"flurstueckskennzeichen", Xml_AttrRep.AttrType.stringAttribute },
                {"amtlicheFlaeche", Xml_AttrRep.AttrType.areaAttribute },
                {"flurnummer", Xml_AttrRep.AttrType.intAttribute },
                {"flurstuecksfolge", Xml_AttrRep.AttrType.stringAttribute },
                {"abweichenderRechtszustand", Xml_AttrRep.AttrType.boolAttribute },
                {"zweifelhafterFlurstuecksnachweis", Xml_AttrRep.AttrType.boolAttribute },
                {"rechtsbehelfsverfahren", Xml_AttrRep.AttrType.boolAttribute },
                {"zeitpunkt_der_Entstehung", Xml_AttrRep.AttrType.stringAttribute },
                {"gemeinde_Land", Xml_AttrRep.AttrType.stringAttribute },
                {"gemeinde_Bezirk", Xml_AttrRep.AttrType.stringAttribute },
                {"gemeinde_Kreis", Xml_AttrRep.AttrType.stringAttribute },
                {"gemeinde", Xml_AttrRep.AttrType.stringAttribute },
                {"gemeindeteil", Xml_AttrRep.AttrType.stringAttribute },
                {"zustaendige_Stelle_Land", Xml_AttrRep.AttrType.stringAttribute },
                {"zustaendige_Stelle", Xml_AttrRep.AttrType.stringAttribute },
                {"eigentuemer_Name_Firma", Xml_AttrRep.AttrType.stringAttribute },
                {"eigentuemer_Vorname", Xml_AttrRep.AttrType.stringAttribute },
                {"eigentuemer_Ort", Xml_AttrRep.AttrType.stringAttribute },
                {"eigentuemer_Plz", Xml_AttrRep.AttrType.stringAttribute },
                {"eigentuemer_Strasse", Xml_AttrRep.AttrType.stringAttribute },
                {"eigentuemer_Hausnummer", Xml_AttrRep.AttrType.stringAttribute },
                {"zeitpunktDerEntstehung", Xml_AttrRep.AttrType.stringAttribute }
            };

            foreach (var parcel in parcelData)
            {
                regAttr.Add(new Xml_AttrRep(Xml_AttrRep.AttrNsp.alkis, parcel.Key, parcel.Value, Xml_AttrRep.AttrHierarchy.parcel));
            }

            return regAttr;
        }
    }
}