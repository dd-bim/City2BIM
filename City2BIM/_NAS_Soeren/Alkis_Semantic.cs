using City2BIM.GetSemantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static City2BIM._NAS_Soeren.AX_Object;

namespace City2BIM._NAS_Soeren
{
    public static class Alkis_Semantic
    {
        public static HashSet<XmlAttribute> GetParcelAttributes()
        {
            var regAttr = new HashSet<XmlAttribute>();

            var parcelData = new Dictionary<string, XmlAttribute.AttrType>
            {
                {"Gemarkung_Land", XmlAttribute.AttrType.stringAttribute },
                {"Gemarkung_Nummer", XmlAttribute.AttrType.stringAttribute },
                {"Flurstuecksnummer", XmlAttribute.AttrType.stringAttribute },
                {"Flurstueckskennzeichen", XmlAttribute.AttrType.stringAttribute },
                {"Amtliche_Flaeche", XmlAttribute.AttrType.areaAttribute },
                {"Flurnummer", XmlAttribute.AttrType.intAttribute },
                {"Flurstuecksfolge", XmlAttribute.AttrType.stringAttribute },
                {"Abweichender_Rechtszustand", XmlAttribute.AttrType.boolAttribute },
                {"Zweifelhafter_Flurstuecksnachweis", XmlAttribute.AttrType.boolAttribute },
                {"Rechtsbehelfsverfahren", XmlAttribute.AttrType.boolAttribute },
                {"Zeitpunkt_der_Entstehung", XmlAttribute.AttrType.stringAttribute },
                {"Gemeinde_Land", XmlAttribute.AttrType.stringAttribute },
                {"Gemeinde_Bezirk", XmlAttribute.AttrType.stringAttribute },
                {"Gemeinde_Kreis", XmlAttribute.AttrType.stringAttribute },
                {"Gemeinde", XmlAttribute.AttrType.stringAttribute },
                {"Gemeindeteil", XmlAttribute.AttrType.stringAttribute },
                {"Zustaendige_Stelle_Land", XmlAttribute.AttrType.stringAttribute },
                {"Zustaendige_Stelle", XmlAttribute.AttrType.stringAttribute },
                {"Eigentuemer_Name_Firma", XmlAttribute.AttrType.stringAttribute },
                {"Eigentuemer_Vorname", XmlAttribute.AttrType.stringAttribute },
                {"Eigentuemer_Ort", XmlAttribute.AttrType.stringAttribute },
                {"Eigentuemer_Plz", XmlAttribute.AttrType.stringAttribute },
                {"Eigentuemer_Strasse", XmlAttribute.AttrType.stringAttribute },
                {"Eigentuemer_Hausnummer", XmlAttribute.AttrType.stringAttribute }
            };

            foreach (var parcel in parcelData)
            {
                regAttr.Add(new XmlAttribute(XmlAttribute.AttrNsp.alkis, parcel.Key, parcel.Value, XmlAttribute.AttrHierarchy.parcel));
            }

            return regAttr;
        }
    }
}