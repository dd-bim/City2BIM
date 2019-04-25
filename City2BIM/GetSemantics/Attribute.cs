using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace City2BIM.GetSemantics
{
    public class Attribute
    {
        private string gmlNamespace;         //für Parameter-Gruppierung in Revit
        private string gmlType;

        private string name;
        private string description;
        private int? code;

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

        public string GmlType
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

        public string GmlNamespace
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

        public int? Code
        {
            get
            {
                return this.code;
            }

            set
            {
                this.code = value;
            }
        }

        public Attribute(string namesp, string name, string type )
        {
            this.GmlNamespace = namesp;
            this.Name = name;
            this.GmlType = type;
        }
    }
}
