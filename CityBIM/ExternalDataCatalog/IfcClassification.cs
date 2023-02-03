using System;
using System.Collections.Generic;
using System.Linq;


namespace CityBIM.ExternalDataCatalog
{
    public class IfcClassification
    {
        public string ID { get; set; }
        public string Source { get; set; }
        public string Edition { get; set; }
        public string EditionDate { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string RefToken { get; set; }

        public List<IfcClassificationReference> RefList { get; set; }
        public IfcClassification()
        {

        }

        public IfcClassification(string ID = null, string Source = null, string Edition = null, string EditionDate = null, string Name = null, string Description = null, string Location = null, string RefToken = null, List<IfcClassificationReference> refList = null)
        {
            this.ID = ID;
            this.Source = Source;
            this.Edition = Edition;
            this.EditionDate = EditionDate;
            this.Name = Name;
            this.Description = Description;
            this.Location = Location;
            this.RefToken = RefToken;
            this.RefList = refList;
        }
    }
}
