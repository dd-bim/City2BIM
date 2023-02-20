using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

using Newtonsoft.Json;

using CommonRevit.Semantics;

namespace DataCatPlugin.GUI
{
    /// <summary>
    /// Interaction logic for DataCatalogEditorWindow.xaml
    /// </summary>
    public partial class DataCatalogEditorWindow : Window
    {
        public bool saveChanges { get; set; }
        public Dictionary<string, ObservableCollection<AttributeContainer>> data { get; set; }

        public DataCatalogEditorWindow(Dictionary<string, Dictionary<string, string>> ObjectTypeAndAttributesDict)
        {
            this.data = new Dictionary<string, ObservableCollection<AttributeContainer>>();
            this.saveChanges = false;
            InitializeComponent();

            foreach (var key in ObjectTypeAndAttributesDict.Keys)
            {
                var currentTabItem = new TabItem();
                currentTabItem.Header = key;
                var attrList = AttributeContainer.getAttrContainerFromDict(ObjectTypeAndAttributesDict[key]);
                currentTabItem.Content = attrList;
                data.Add(key, attrList);
                tabs.Items.Add(currentTabItem);
            }

        }

        private void save_Click(object sender, RoutedEventArgs e)
        {
            saveChanges = true;
            this.Close();
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            saveChanges = false;
            this.Close();
        }

        public string getDataAsJsonString()
        {
            var dict = new Dictionary<string, string>();

            foreach (KeyValuePair<string, ObservableCollection<AttributeContainer>> pair in this.data)
            {
                foreach (var item in pair.Value)
                {
                    dict.Add(item.attrName, item.attrValue);
                }
            }

            return JsonConvert.SerializeObject(dict, Formatting.None);
        }
    }
}
