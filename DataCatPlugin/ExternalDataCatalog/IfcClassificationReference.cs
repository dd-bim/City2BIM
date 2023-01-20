using System.Collections.Generic;

namespace DataCatPlugin.ExternalDataCatalog
{
    public class IfcClassificationReference
    {
        public string ID { get; set; }
        public string Location { get; set; }
        public string Name { get; set; }
        public string ReferencedSource { get; set; }
        public string Description { get; set; }
        public string Sort { get; set; }
        public List<IfcClassificationReference> RefList { get; set; }

        
        public IfcClassificationReference()
        {

        }

        public IfcClassificationReference(string ID = null, string Location = null, string Name = null, string ReferencedSource = null, string Description = null, string Sort = null, List<IfcClassificationReference> RefList = null)
        {
            this.ID = ID;
            this.Location = Location;
            this.Name = Name;
            this.ReferencedSource = ReferencedSource;
            this.Description = Description;
            this.Sort = Sort;
            this.RefList = RefList;
        }
    }
}