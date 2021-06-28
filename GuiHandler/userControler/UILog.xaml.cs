using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;

//logging
using BIMGISInteropLibs.Logging; //access to log writer
using LogWriter = BIMGISInteropLibs.Logging.LogWriterIfcTerrain; //to set log messages

//shortcut to set json settings
using init = GuiHandler.InitClass;


namespace GuiHandler.userControler
{
    /// <summary>
    /// Interaktionslogik für UILog.xaml
    /// </summary>
    public partial class UILog : UserControl
    {
        //init collection
        private static readonly ObservableCollection<string> _LogMessages = new ObservableCollection<string>();

        //data binding - bind log messages
        public static ObservableCollection<string> LogMessages
        {
            get { return _LogMessages; }
        }

        public UILog()
        {
            InitializeComponent();

            DataContext = this;
        }

        /// <summary>
        /// swtiching verbosity levels
        /// </summary>
        private void selectVerbosityLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //check and set the choosen verbo level into json settings
            if (minVerbose.IsSelected)
            {
                init.config.verbosityLevel = LogType.verbose;
            }
            else if (minDebug.IsSelected)
            {
                init.config.verbosityLevel = LogType.debug;
            }
            else if (minInformation.IsSelected)
            {
                init.config.verbosityLevel = LogType.info;
            }
            else if (minWarning.IsSelected)
            {
                init.config.verbosityLevel = LogType.warning;
            }

            //logging
            LogWriter.Entries.Add(new LogPair(LogType.verbose, "Verbosity level changed to: " + init.config.verbosityLevel.ToString()));
        }

        //source: https://stackoverflow.com/questions/2337822/wpf-listbox-scroll-to-end-automatically
        /// <summary>
        /// update list box (scroll at the end)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <param name="root"></param>
        /// <returns></returns>
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


        
    }
}
