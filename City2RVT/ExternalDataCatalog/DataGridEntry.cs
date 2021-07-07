using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace City2RVT.ExternalDataCatalog
{
    public class DataGridEntry
    {
        public string RevitID { get; set; }
        public string RevitCategory { get; set; }
        public string IfcClassificationReference { get; set; }
        public string IfcClassification { get; set; }
    }
}
