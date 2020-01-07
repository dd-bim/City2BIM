using City2BIM.Semantic;
using System.Collections.Generic;

namespace City2BIM.Alkis
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
                {"Gemarkung_Land", Xml_AttrRep.AttrType.stringAttribute },
                {"Gemarkung_Nummer", Xml_AttrRep.AttrType.stringAttribute },
                {"Flurstuecksnummer", Xml_AttrRep.AttrType.stringAttribute },
                {"Flurstueckskennzeichen", Xml_AttrRep.AttrType.stringAttribute },
                {"Amtliche_Flaeche", Xml_AttrRep.AttrType.areaAttribute },
                {"Flurnummer", Xml_AttrRep.AttrType.intAttribute },
                {"Flurstuecksfolge", Xml_AttrRep.AttrType.stringAttribute },
                {"Abweichender_Rechtszustand", Xml_AttrRep.AttrType.boolAttribute },
                {"Zweifelhafter_Flurstuecksnachweis", Xml_AttrRep.AttrType.boolAttribute },
                {"Rechtsbehelfsverfahren", Xml_AttrRep.AttrType.boolAttribute },
                {"Zeitpunkt_der_Entstehung", Xml_AttrRep.AttrType.stringAttribute },
                {"Gemeinde_Land", Xml_AttrRep.AttrType.stringAttribute },
                {"Gemeinde_Bezirk", Xml_AttrRep.AttrType.stringAttribute },
                {"Gemeinde_Kreis", Xml_AttrRep.AttrType.stringAttribute },
                {"Gemeinde", Xml_AttrRep.AttrType.stringAttribute },
                {"Gemeindeteil", Xml_AttrRep.AttrType.stringAttribute },
                {"Zustaendige_Stelle_Land", Xml_AttrRep.AttrType.stringAttribute },
                {"Zustaendige_Stelle", Xml_AttrRep.AttrType.stringAttribute },
                {"Eigentuemer_Name_Firma", Xml_AttrRep.AttrType.stringAttribute },
                {"Eigentuemer_Vorname", Xml_AttrRep.AttrType.stringAttribute },
                {"Eigentuemer_Ort", Xml_AttrRep.AttrType.stringAttribute },
                {"Eigentuemer_Plz", Xml_AttrRep.AttrType.stringAttribute },
                {"Eigentuemer_Strasse", Xml_AttrRep.AttrType.stringAttribute },
                {"Eigentuemer_Hausnummer", Xml_AttrRep.AttrType.stringAttribute }
            };

            foreach (var parcel in parcelData)
            {
                regAttr.Add(new Xml_AttrRep(Xml_AttrRep.AttrNsp.alkis, parcel.Key, parcel.Value, Xml_AttrRep.AttrHierarchy.parcel));
            }

            return regAttr;
        }
    }
}