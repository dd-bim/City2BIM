using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using City2BIM.Semantic;

namespace City2RVT.GUI.XPlan2BIM
{
    public class XPlan_Inhalt
    {
        public static HashSet<Xml_AttrRep> GetParcelAttributes()
        {
            var regAttr = new HashSet<Xml_AttrRep>();

            var parcelData = new Dictionary<string, Xml_AttrRep.AttrType>
            {
                {"Rechtsstand", Xml_AttrRep.AttrType.stringAttribute },
                {"Ebene", Xml_AttrRep.AttrType.stringAttribute },
                {"Rechtscharakter", Xml_AttrRep.AttrType.stringAttribute },
                {"Flaechenschluss", Xml_AttrRep.AttrType.stringAttribute },
                {"Nutzungsform", Xml_AttrRep.AttrType.areaAttribute },
            };

            foreach (var parcel in parcelData)
            {
                regAttr.Add(new Xml_AttrRep(Xml_AttrRep.AttrNsp.alkis, parcel.Key, parcel.Value, Xml_AttrRep.AttrHierarchy.parcel));
            }

            return regAttr;
        }
    }
}
