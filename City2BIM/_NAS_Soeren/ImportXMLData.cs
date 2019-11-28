using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace NasImport
{
    class ImportXMLData : ImportData
    {
        public ImportXMLData(ExternalCommandData commandData, ImportFormat importFormat)
            : base(commandData, importFormat)
        {
            m_filter = "XML Documents (*.xml) | *.xml";
            m_title = "Import XML";
        }

        /*public override bool Import()
        {
            bool imported = false;

            Transaction t = new Transaction(m_activeDoc);
            t.SetName("Import XML");
            t.Start();
            imported = m_activeDoc.Import(m_importFileFullName, options);
            t.Commit();

            return imported;
        }
        */
    }
}
