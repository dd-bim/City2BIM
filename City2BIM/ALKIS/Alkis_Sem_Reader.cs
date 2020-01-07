using City2BIM.Semantic;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace City2BIM.Alkis
{
    /// <summary>
    /// Class for reading of attribute values
    /// </summary>
    public class Alkis_Sem_Reader
    {
        private readonly XDocument xDoc;
        private readonly XElement Dienststelle;
        private readonly Dictionary<string, XNamespace> nsp;

        public Alkis_Sem_Reader(XDocument xDoc, Dictionary<string, XNamespace> allns)
        {
            this.xDoc = xDoc;
            this.nsp = allns;

            XElement dienstParent = xDoc.Descendants(nsp[""] + "AX_Dienststelle").FirstOrDefault();

            if (dienstParent != null)
            {
                this.Dienststelle = dienstParent.Descendants(nsp[""] + "AX_Dienststelle_Schluessel").FirstOrDefault();
            }
        }

        /// <summary>
        /// Reads attribute values from defined (parcel) attributes
        /// </summary>
        /// <param name="p">Parcel xml object</param>
        /// <param name="attributes">Predefined attributes</param>
        /// <returns>Key-Value attribute, value</returns>
        public Dictionary<Xml_AttrRep, string> ReadAttributeValuesParcel(XElement p, HashSet<Xml_AttrRep> attributes)
        {
            var kvp = new Dictionary<Xml_AttrRep, string>();

            //Gemarkung

            XElement gem = GetXmlParent(p, "AX_Gemarkung_Schluessel");

            string land = "", nr = "";

            if (gem != null)
            {
                land = GetXmlValue(gem, "land");
                nr = GetXmlValue(gem, "gemarkungsnummer");
            }

            kvp.Add(attributes.Where(f => f.Name == "Gemarkung_Land").Single(), land);
            kvp.Add(attributes.Where(f => f.Name == "Gemarkung_Nummer").Single(), nr);

            //Flurstücksnummer

            XElement flst = GetXmlParent(p, "AX_Flurstuecksnummer");

            string z = "", n = "";

            if (flst != null)
            {
                z = GetXmlValue(flst, "zaehler");
                n = GetXmlValue(flst, "nenner");
            }

            kvp.Add(attributes.Where(f => f.Name == "Flurstuecksnummer").Single(), z + "_" + n);

            kvp.Add(attributes.Where(f => f.Name == "Flurstueckskennzeichen").Single(), GetXmlValue(p, "flurstueckskennzeichen"));
            kvp.Add(attributes.Where(f => f.Name == "Amtliche_Flaeche").Single(), GetXmlValue(p, "amtlicheFlaeche"));
            kvp.Add(attributes.Where(f => f.Name == "Flurnummer").Single(), GetXmlValue(p, "flurnummer"));
            kvp.Add(attributes.Where(f => f.Name == "Flurstuecksfolge").Single(), GetXmlValue(p, "flurstuecksfolge"));
            kvp.Add(attributes.Where(f => f.Name == "Abweichender_Rechtszustand").Single(), GetXmlValue(p, "abweichenderRechtszustand"));
            kvp.Add(attributes.Where(f => f.Name == "Zweifelhafter_Flurstuecksnachweis").Single(), GetXmlValue(p, "zweifelhafterFlurstuecksnachweis"));
            kvp.Add(attributes.Where(f => f.Name == "Rechtsbehelfsverfahren").Single(), GetXmlValue(p, "rechtsbehelfsverfahren"));
            kvp.Add(attributes.Where(f => f.Name == "Zeitpunkt_der_Entstehung").Single(), GetXmlValue(p, "zeitpunktDerEntstehung"));

            //Gemeindezugehörigkeit

            XElement gmd = GetXmlParent(p, "AX_Gemeindekennzeichen");

            string landG = "", bezirk = "", kreis = "", gemeinde = "", teil = "";

            if (gmd != null)
            {
                landG = GetXmlValue(gmd, "land");
                bezirk = GetXmlValue(gmd, "regierungsbezirk");
                kreis = GetXmlValue(gmd, "kreis");
                gemeinde = GetXmlValue(gmd, "gemeinde");
                teil = GetXmlValue(gmd, "gemeindeteil");
            }

            kvp.Add(attributes.Where(f => f.Name == "Gemeinde_Land").Single(), landG);
            kvp.Add(attributes.Where(f => f.Name == "Gemeinde_Bezirk").Single(), bezirk);
            kvp.Add(attributes.Where(f => f.Name == "Gemeinde_Kreis").Single(), kreis);
            kvp.Add(attributes.Where(f => f.Name == "Gemeinde").Single(), gemeinde);
            kvp.Add(attributes.Where(f => f.Name == "Gemeindeteil").Single(), teil);

            //Zuständige Stelle

            XElement stelle = GetXmlParent(p, "AX_Dienststelle_Schluessel");

            //if not specified at parcel level the global attribute will be used

            if (stelle == null)
                stelle = this.Dienststelle;

            string landD = "", stelleNr = "";

            if (stelle != null)
            {
                landD = GetXmlValue(stelle, "land");
                stelleNr = GetXmlValue(stelle, "stelle");
            }

            kvp.Add(attributes.Where(f => f.Name == "Zustaendige_Stelle_Land").Single(), landD);
            kvp.Add(attributes.Where(f => f.Name == "Zustaendige_Stelle").Single(), stelleNr);

            //Eigentümer

            string name = "", vorname = "", ort = "", plz = "", strasse = "", hausNr = "";

            XElement istGebucht = GetXmlParent(p, "istGebucht");

            if (istGebucht != null)
            {
                XAttribute hrefBuchung = istGebucht.Attributes(nsp["xlink"] + "href").FirstOrDefault();

                if (hrefBuchung != null)
                {
                    string[] idLink = hrefBuchung.Value.Split(':');
                    string idBuchung = idLink.LastOrDefault();

                    if (idBuchung != null)
                    {
                        XElement bStelle = xDoc.Descendants(nsp[""] + "AX_Buchungsstelle").Where(a => a.Attribute(nsp["gml"] + "id").Value == idBuchung).FirstOrDefault();

                        if (bStelle != null)
                        {
                            XElement istBestandteilVon = GetXmlParent(bStelle, "istBestandteilVon");

                            if (istBestandteilVon != null)
                            {
                                XAttribute hrefBlatt = istBestandteilVon.Attributes(nsp["xlink"] + "href").FirstOrDefault();

                                if (hrefBlatt != null)
                                {
                                    string[] idLink2 = hrefBlatt.Value.Split(':');
                                    string idBlatt = idLink2.LastOrDefault();

                                    if (idBlatt != null)
                                    {
                                        XElement namensNr = xDoc.Descendants(nsp[""] + "AX_Namensnummer").
                                            Where(a => a.Elements(nsp[""] + "istBestandteilVon").FirstOrDefault().
                                            Attribute(nsp["xlink"] + "href").Value.
                                            Split(':').LastOrDefault() == idBlatt).FirstOrDefault();

                                        if (namensNr != null)
                                        {
                                            string[] pId = namensNr.Descendants(nsp[""] + "benennt").FirstOrDefault().Attributes(nsp["xlink"] + "href").FirstOrDefault().Value.Split(':');

                                            string personId = "";

                                            if (pId != null)
                                                personId = pId.LastOrDefault();

                                            XElement person = xDoc.Descendants(nsp[""] + "AX_Person").
                                                Where(a => a.Attribute(nsp["gml"] + "id").Value.Split(':').LastOrDefault() == personId).FirstOrDefault();

                                            if (person != null)
                                            {
                                                name = GetXmlValue(person, "nachnameOderFirma");
                                                vorname = GetXmlValue(person, "vorname");

                                                string[] adrId = person.Descendants(nsp[""] + "hat").FirstOrDefault().Attributes(nsp["xlink"] + "href").FirstOrDefault().Value.Split(':');

                                                string addressId = "";

                                                if (adrId != null)
                                                    addressId = adrId.LastOrDefault();

                                                XElement address = xDoc.Descendants(nsp[""] + "AX_Anschrift").Where(a => a.Attribute(nsp["gml"] + "id").Value.Split(':').LastOrDefault() == addressId).FirstOrDefault();

                                                if (address != null)
                                                {
                                                    ort = GetXmlValue(address, "ort_Post");
                                                    plz = GetXmlValue(address, "postleitzahlPostzustellung");
                                                    strasse = GetXmlValue(address, "strasse");
                                                    hausNr = GetXmlValue(address, "hausnummer");

                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            kvp.Add(attributes.Where(f => f.Name == "Eigentuemer_Name_Firma").Single(), name);
            kvp.Add(attributes.Where(f => f.Name == "Eigentuemer_Vorname").Single(), vorname);
            kvp.Add(attributes.Where(f => f.Name == "Eigentuemer_Ort").Single(), ort);
            kvp.Add(attributes.Where(f => f.Name == "Eigentuemer_Plz").Single(), plz);
            kvp.Add(attributes.Where(f => f.Name == "Eigentuemer_Strasse").Single(), strasse);
            kvp.Add(attributes.Where(f => f.Name == "Eigentuemer_Hausnummer").Single(), hausNr);

            return kvp;
        }

        /// <summary>
        /// Gets Xml element where searched value is stored in a child xml element
        /// </summary>
        /// <param name="parcel">Parcel element</param>
        /// <param name="tagName">Name of element which has child element with value</param>
        /// <returns></returns>
        private XElement GetXmlParent(XElement parcel, string tagName)
        {
            var child = parcel.Descendants(nsp[""] + tagName).FirstOrDefault();

            if (child == null)
                return null;

            return child;
        }

        /// <summary>
        /// Gets the searched value for an attribute
        /// </summary>
        /// <param name="parentTag">Xml parent</param>
        /// <param name="childName">Xml child</param>
        /// <returns>Value as string</returns>
        private string GetXmlValue(XElement parentTag, string childName)
        {
            var child = parentTag.Elements(nsp[""] + childName).FirstOrDefault();

            if (child == null)
                return "";

            return child.Value;
        }
    }
}