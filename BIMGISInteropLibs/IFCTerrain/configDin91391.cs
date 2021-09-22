using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BIMGISInteropLibs.Logging; //include for verbosity level
using System.ComponentModel;//interface property changed

namespace BIMGISInteropLibs.IfcTerrain
{
    /// <summary>
    /// storage for json settings (according to standard: DIN SPEC 91391-2)
    /// </summary>
    public class configDin91391 : INotifyPropertyChanged
    {
        /// <summary>
        /// event needed for interface (DO NOT RENAME)
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// function to check if property has 'really' been changed 
        /// </summary>
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private Guid _guid { get; set; } = Guid.NewGuid();

        /// <summary>
        /// unique Identificator (obligatory)
        /// </summary>
        public Guid guid 
        { 
            get { return _guid; }
            set
            {
                _guid = value;
                NotifyPropertyChanged(nameof(guid));
            }
        }

        /// <summary>
        /// file name (obligatory)
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// type (e.g.: DTM) (obligatory)
        /// </summary>
        public string type { get; set; }

        private string _description { get; set; }

        /// <summary>
        /// supplementary description (opitonal)
        /// </summary>
        public string description 
        {
            get { return _description; }
            set
            {
                _description = value;
                NotifyPropertyChanged(nameof(description));
            }
        }

        /// <summary>
        /// creation date (opitonal)
        /// </summary>
        public string created { get; set; }

        /// <summary>
        /// [leave emtpy] upload date (opitonal)
        /// </summary>
        public string uploaded { get; set; }

        private string _creator { get; set; }

        /// <summary>
        /// refernz to creator of the container (optional)
        /// </summary>
        public string creator
        {
            get { return _creator; }
            set
            {
                _creator = value;
                NotifyPropertyChanged(nameof(creator));
            }
        }

        /// <summary>
        /// [leave empty!] refernz to sender of the container (optional)
        /// </summary>
        public string sender { get; set; }

        /// <summary>
        /// [leave empty]: refenz(es) to to receiver (multiple possible) (optional)
        /// </summary>
        public string[] recipients { get; set; }

        private string _revision { get; set; }

        /// <summary>
        /// revision number (obligatory)
        /// </summary>
        public string revision
        {
            get { return _revision; }
            set
            {
                _revision = value;
                NotifyPropertyChanged(nameof(revision));
            }
        }
        private string _version { get; set; }

        /// <summary>
        /// version number (optional)
        /// </summary>
        public string version
        {
            get { return _version; }
            set
            {
                _version = value;
                NotifyPropertyChanged(nameof(version));
            }
        }

        /// <summary>
        /// [leave empty]: status of container
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// [leave empty]: contents of the container
        /// </summary>
        public string content { get; set; }

        /// <summary>
        /// [leave empty]: reference to location (download address)
        /// </summary>
        public string location { get; set; }

        private string _projectId { get; set; }

        /// <summary>
        /// project id (obligatory)
        /// </summary>
        public string projectId
        {
            get { return _projectId; }
            set
            {
                _projectId = value;
                NotifyPropertyChanged(nameof(projectId));
            }
        }
        private string _metadataSchema { get; set; }
        /// <summary>
        /// reference to schema (for more metadata) (optional)
        /// </summary>
        public string metadataSchema
        {
            get { return _metadataSchema; }
            set
            {
                _metadataSchema = value;
                NotifyPropertyChanged(nameof(metadataSchema));
            }
        }

        /// <summary>
        /// mime type eg.: application/x-step
        /// </summary>
        public string mimeType { get; set; }
    }
}
