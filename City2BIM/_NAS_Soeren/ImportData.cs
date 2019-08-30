using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace NasImport
{
    public class ImportData
    {
        protected ExternalCommandData m_commandData;
        protected Document m_activeDoc;
        protected String m_importFolder;
        protected String m_importFileFullName;
        protected ImportFormat m_importFormat;
        protected String m_filter;
        protected String m_title;

        public ExternalCommandData CommandData
        {
            get
            {
                return m_commandData;
            }
        }

        public String ImportFileFullName
        {
            get
            {
                return m_importFileFullName;
            }
            set
            {
                m_importFileFullName = value;
            }
        }

        public ImportFormat ImportFormat
        {
            get
            {
                return m_importFormat;
            }
            set
            {
                m_importFormat = value;
            }
        }
        public ImportData(ExternalCommandData commandData, ImportFormat importFormat)
        {
            m_commandData = commandData;
            m_activeDoc = commandData.Application.ActiveUIDocument.Document;
            m_importFormat = importFormat;
            m_filter = String.Empty;
            Initialize();
        }
        private void Initialize()
        {
            //The directory into which the file will be imported
            String dllFilePath = Assembly.GetExecutingAssembly().Location;
            m_importFolder = Path.GetDirectoryName(dllFilePath);
            m_importFileFullName = String.Empty;
        }
        public virtual bool Import()
        {
            if (m_importFileFullName == null)
            {
                throw new NullReferenceException();
            }

            return true;
        }
        public String Filter
        {
            get
            {
                return m_filter;
            }
        }
        public String ImportFolder
        {
            get
            {
                return m_importFolder;
            }
            set
            {
                m_importFolder = value;
            }
        }
        public String Title
        {
            get
            {
                return m_title;
            }
        }
    }
}
