using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;

using System.ComponentModel; //interface property changed

namespace GuiHandler.userControler
{
    /// <summary>
    /// store log entries with different verbosity level (has no connection with the output config file) 
    /// </summary>
    public class logEntry : INotifyPropertyChanged
    {
        /// <summary>
        /// do not rename (otherwise whole 'logEntry' interface is not valid)
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// function to check if property is really changed
        /// </summary>
        /// <param name="info"></param>
        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private BIMGISInteropLibs.Logging.LogType _logType { get; set; }

        /// <summary>
        /// store log type via enumeration
        /// </summary>
        public BIMGISInteropLibs.Logging.LogType logType
        {
            get { return _logType; }
            set
            {
                _logType = value;
                NotifyPropertyChanged(nameof(logType));
            }
        }
        
        private string _logMessage { get; set; }

        /// <summary>
        /// message which will be logged
        /// </summary>
        public string logMessage
        {
            get { return _logMessage; }
            set
            {
                _logMessage = value;
                NotifyPropertyChanged(nameof(logMessage));
            }
        }
    }

    /// <summary>
    /// Interaktionslogik für UILog.xaml
    /// </summary>
    public partial class UILog : UserControl
    {
        /// <summary>
        /// create obs collection to store log entries
        /// </summary>
        public static ObservableCollection<logEntry> logEntries { get; set; }

        /// <summary>
        /// init ui and apply data context
        /// </summary>
        public UILog()
        {
            InitializeComponent();

            //init new observable collection
            logEntries = new ObservableCollection<logEntry>();

            //set data context 'log entries' to list view ('container')
            lvGuiLogging.DataContext = logEntries;
        }

        #region auto scroll in logging field
        //source: https://stackoverflow.com/questions/2337822/wpf-listbox-scroll-to-end-automatically
        /// <summary>
        /// update list box (scroll at the end)
        /// </summary>
        private void ListBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox)sender;

            var scrollViewer = FindScrollViewer(listBox);

            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += (o, args) =>
                {
                    if (args.ExtentHeightChange > 0)
                        scrollViewer.ScrollToBottom();
                };
            }
        }

        /// <summary>
        /// search for scroll viewer
        /// </summary>
        private static ScrollViewer FindScrollViewer(DependencyObject root)
        {
            var queue = new Queue<DependencyObject>(new[] { root });

            do
            {
                var item = queue.Dequeue();

                if (item is ScrollViewer)
                    return (ScrollViewer)item;

                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(item); i++)
                    queue.Enqueue(VisualTreeHelper.GetChild(item, i));
            } while (queue.Count > 0);

            return null;
        }
        #endregion

        /// <summary>
        /// function to set some user feedback / gui logging 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void selectVerbosityLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //get config
            var config = DataContext as BIMGISInteropLibs.IfcTerrain.Config;

            //gui log
            GuiSupport.setLog(BIMGISInteropLibs.Logging.LogType.info, "Verbosity level set to: " + config.verbosityLevel);
        }
    }
}
