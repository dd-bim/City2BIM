namespace City2BIM.GetSemantics
{
    /// <summary>
    /// Class for semantic attribute representation
    /// </summary>
    public class XmlAttribute
    {
        private AttrNsp xmlNamespace;         //für Parameter-Gruppierung in Revit
        private AttrType xmlType;
        private AttrHierarchy reference;

        private string name;

        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                this.name = value;
            }
        }

        public AttrType XmlType
        {
            get
            {
                return this.xmlType;
            }

            set
            {
                this.xmlType = value;
            }
        }

        public AttrHierarchy Reference
        {
            get
            {
                return this.reference;
            }

            set
            {
                this.reference = value;
            }
        }

        public AttrNsp XmlNamespace
        {
            get
            {
                return this.xmlNamespace;
            }

            set
            {
                this.xmlNamespace = value;
            }
        }

        public XmlAttribute(AttrNsp namesp, string name, AttrType type, AttrHierarchy reference)
        {
            this.XmlNamespace = namesp;
            this.Name = name;
            this.XmlType = type;
            this.Reference = reference;
        }

        /// <summary>
        /// Enum for possible attribute types
        /// Inspired by CityGML Generics module
        /// Adapted for fixed attributes
        /// </summary>
        public enum AttrType { stringAttribute, intAttribute, doubleAttribute, measureAttribute, areaAttribute, uriAttribute, boolAttribute }

        /// <summary>
        /// Enum for possible attribute namespaces
        /// Shortcuts for namespaces as saved in CityGML (+ Alkis) files
        /// </summary>
        public enum AttrNsp { gml, core, xal, bldg, gen, alkis }

        /// <summary>
        /// Enum for possible attribute namespaces
        /// Shortcuts for namespaces as saved in CityGML (+ Alkis) files
        /// </summary>
        public enum AttrHierarchy { bldgCity, surface, wall, ground, roof, closure, outerCeiling, outerFloor, parcel, usage, bldgAlkis }
    }
}