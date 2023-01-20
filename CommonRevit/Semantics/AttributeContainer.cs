using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CommonRevit.Semantics
{
    public class AttributeContainer : INotifyPropertyChanged
    {
        public string attrName { get; set; }
        private string attrvalue;
        public string attrValue
        {
            get { return this.attrvalue; }
            set
            {
                if (this.attrvalue != value)
                {
                    this.attrvalue = value;
                    this.NotifyPropertyChanged("attrValue");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        public static ObservableCollection<AttributeContainer> getAttrContainerFromDict(Dictionary<string, string> attributes)
        {
            ObservableCollection<AttributeContainer> collection = new ObservableCollection<AttributeContainer>();

            foreach (KeyValuePair<string, string> entry in attributes)
            {
                var attrCont = new AttributeContainer();
                attrCont.attrName = entry.Key;
                attrCont.attrValue = entry.Value;
                collection.Add(attrCont);
            }

            return collection;
        }
    }
}
