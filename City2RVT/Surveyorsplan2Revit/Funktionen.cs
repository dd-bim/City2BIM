using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace lageplanImport
{
    public class Funktionen
    {
        public static Element FindElementByName(
          Document doc,
          Type targetType,
          string targetName)
        {
            return new FilteredElementCollector(doc)
              .OfClass(targetType)
              .FirstOrDefault<Element>(
                e => e.Name.Equals(targetName));
        }
        public double feetToMeter = 1 / 0.3048;

    }
}
