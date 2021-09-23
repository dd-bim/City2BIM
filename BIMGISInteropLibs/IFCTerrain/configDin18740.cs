using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BIMGISInteropLibs.Logging; //include for verbosity level
using System.ComponentModel;//interface property changed

namespace BIMGISInteropLibs.IfcTerrain
{
    public enum dataStructureTypes
    {
        mesh,
        tin
    }

    public enum dtmRepresentation
    {
        Shading
    }

    public enum dataAquisitionMethod
    {
        TerrestrialSurvey,
        Photogrammetry,
        AirborneLaserscanning,
        InSAR
    }


    /// <summary>
    /// storage for json settings (according to standard: DIN 18740-6)
    /// </summary>
    public class configDin18740 : INotifyPropertyChanged
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

        
        private string _modelType { get; set; } = "DTM";

        /// <summary>
        /// model typ - here dtm (german: Modelltyp; nur DGM)
        /// </summary>
        public string modelType 
        { 
            get { return _modelType; }
            set
            {
                _modelType = value;
                NotifyPropertyChanged(nameof(modelType));
            } 
        }

        private dataStructureTypes _dataStructure { get; set; }

        /// <summary>
        /// data structure - here mesh (german: Datenstruktur; nur Dreiecksvermaschung)
        /// </summary>
        public dataStructureTypes dataStructure
        {
            get { return _dataStructure; }
            set
            {
                _dataStructure = value;
                NotifyPropertyChanged(nameof(dataStructure));
            }
        }
        private dtmRepresentation _dtmRepresentation { get; set; }

        /// <summary>
        /// data structure - here mesh (german: Datenstruktur; nur Dreiecksvermaschung)
        /// </summary>
        public dtmRepresentation dtmRepresentation
        {
            get { return _dtmRepresentation; }
            set
            {
                _dtmRepresentation = value;
                NotifyPropertyChanged(nameof(dtmRepresentation));
            }
        }

        private dataAquisitionMethod _dataAquisitionMethod { get; set; }

        /// <summary>
        /// data structure - here mesh (german: Datenstruktur; nur Dreiecksvermaschung)
        /// </summary>
        public dataAquisitionMethod dataAquisitionMethod
        {
            get { return _dataAquisitionMethod; }
            set
            {
                _dataAquisitionMethod = value;
                NotifyPropertyChanged(nameof(dataAquisitionMethod));
            }
        }

        private DateTime _topicality { get; set; } = DateTime.Today;

        /// <summary>
        /// date of the dataset (german: Erfassungsdatum)
        /// </summary>
        public DateTime topicality 
        { 
            get { return _topicality; }
            set
            {
                _topicality = value;
                NotifyPropertyChanged(nameof(topicality));
            }
        }

        private int _epsgCode { get; set; } = 25833;

        /// <summary>
        /// position reference system (german: Lagereferenzsystem)
        /// </summary>
        public int epsgCode
        {
            get { return _epsgCode; }
            set
            {
                _epsgCode = value;
                NotifyPropertyChanged(nameof(epsgCode));
            }
        }


        private string _positionReferenceSystem { get; set; }

        /// <summary>
        /// position reference system (german: Lagereferenzsystem)
        /// </summary>
        public string positionReferenceSystem
        {
            get { return _positionReferenceSystem; }
            set
            {
                _positionReferenceSystem = value;
                NotifyPropertyChanged(nameof(positionReferenceSystem));
            }
        }

        private string _altitudeReferenceSystem { get; set; }

        /// <summary>
        /// altitude reference system (german: Höhenreferenzsystem)
        /// </summary>
        public string altitudeReferenceSystem
        {
            get { return _altitudeReferenceSystem; }
            set
            {
                _altitudeReferenceSystem = value;
                NotifyPropertyChanged(nameof(altitudeReferenceSystem));
            }
        }

        private string _projection { get; set; }

        /// <summary>
        /// projection
        /// </summary>
        public string projection
        {
            get { return _projection; }
            set
            {
                _projection = value;
                NotifyPropertyChanged(nameof(projection));
            }
        }
    }
}
