using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Autodesk.Revit.UI;

namespace City2RVT.GUI.Properties
{
    /// <summary>
    /// Interaction logic for PropertyWindow.xaml
    /// </summary>
    public partial class PropertyWindow : Window
    {
        public Dictionary<string, ObservableCollection<AttributeContainer>> data { get; set; }
        public bool saveChanges { get; set; }
        public PropertyWindow(Dictionary<string, Dictionary<string, string>> schemaAndAttributesDict)
        {
            this.data = new Dictionary<string, ObservableCollection<AttributeContainer>>();
            this.saveChanges = false;
            InitializeComponent();

            foreach (var key in schemaAndAttributesDict.Keys)
            {
                var currentTabItem = new TabItem();
                currentTabItem.Header = key;
                var attrList = AttributeContainer.getAttrContainerFromDict(schemaAndAttributesDict[key]);
                currentTabItem.Content = attrList;
                data.Add(key, attrList);
                tabs.Items.Add(currentTabItem);
            }
        }

        private void save_Click(object sender, RoutedEventArgs e)
        {
            saveChanges = true;
            TaskDialog.Show("Information", "Will save data after closing window!");
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            saveChanges = false;
            this.Close();
        }
    }

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
