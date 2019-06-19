namespace City2BIM.GetSemantics
{

    /// <summary>
    /// Class for semantic attribute representation
    /// </summary>
    public class Attribute
    {
        private AttrNsp gmlNamespace;         //für Parameter-Gruppierung in Revit
        private AttrType gmlType;

        private string name;
        private string description;

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

        public AttrType GmlType
        {
            get
            {
                return this.gmlType;
            }

            set
            {
                this.gmlType = value;
            }
        }

        public AttrNsp GmlNamespace
        {
            get
            {
                return this.gmlNamespace;
            }

            set
            {
                this.gmlNamespace = value;
            }
        }

        public string Description
        {
            get
            {
                return this.description;
            }

            set
            {
                this.description = value;
            }
        }

        public Attribute(AttrNsp namesp, string name, AttrType type)
        {
            this.GmlNamespace = namesp;
            this.Name = name;
            this.GmlType = type;
        }

        /// <summary>
        /// Enum for possible attribute types
        /// Inspired by CityGML Generics module
        /// Adapted for fixed attributes
        /// </summary>
        public enum AttrType { stringAttribute, intAttribute, doubleAttribute, measureAttribute, uriAttribute }

        /// <summary>
        /// Enum for possible attribute namespaces
        /// Shortcuts for namespaces as saved in CityGML files
        /// </summary>
        public enum AttrNsp { gml, core, xal, bldg, gen }

    }
}