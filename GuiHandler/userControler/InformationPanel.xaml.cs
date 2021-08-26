using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace GuiHandler.userControler
{
    public class infoPanel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private string _fileName { get; set; }

        public string fileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                NotifyPropertyChanged(nameof(fileName));
            }
        }

        private string _fileType { get; set; }

        public string fileType
        {
            get { return _fileType; }
            set
            {
                _fileType = value;
                NotifyPropertyChanged(nameof(fileType));
            }
        }

    } 

    /// <summary>
    /// Interaktionslogik für InformationPanel.xaml
    /// </summary>
    public partial class InformationPanel : UserControl
    {
        public static ObservableCollection<infoPanel> info { get; set; }

        public InformationPanel()
        {
            InitializeComponent();

            info = new ObservableCollection<infoPanel>();

            guiInfo.DataContext = info;

            info.Add(new infoPanel());
            
        }
    }
}
