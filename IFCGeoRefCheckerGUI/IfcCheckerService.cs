using System;
using System.Resources;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xbim.Ifc;

using IFCGeorefShared;


namespace IFCGeoRefCheckerGUI
{    
    class IfcCheckerService
    {
        private readonly ITranslator _translator;

        public IfcCheckerService(ITranslator translator)
        {
            _translator = translator ?? throw new ArgumentNullException(nameof(translator));
        }
        public GeoRefChecker CheckIFC(string filePath) 
        {
            using (var model = IfcStore.Open(filePath))
            {
                //var checker = new GeoRefChecker(model);
                var checker = new GeoRefChecker(model, _translator);
                return checker;
            }
        }
    }
}
