using IFCGeoRefCheckerGUI.ViewModels;
using Microsoft.Win32;
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
using System.Windows.Shapes;

using Serilog;
using Serilog.Sinks.RichTextBox.Themes;
using Xbim.Common;
using OSGeo.OGR;
using IFCGeorefShared;
using IFCGeorefShared.Levels;
using System.Globalization;
using System.Threading;

namespace IFCGeoRefCheckerGUI
{
    /// <summary>
    /// Interaction logic for MainWindowAllInOne.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //public RichTextBoxConsoleTheme logTheme = new RichTextBoxConsoleTheme(RichTextBoxConsoleTheme.Colored);

        public MainWindow()
        {
            InitializeComponent();

            // Fügen Sie die unterstützten Kulturen hinzu
            LanguageComboBox.Items.Add(new ComboBoxItem { Content = "English", Tag = new CultureInfo("en-US") });
            LanguageComboBox.Items.Add(new ComboBoxItem { Content = "Deutsch", Tag = new CultureInfo("de-DE") });
            LanguageComboBox.Items.Add(new ComboBoxItem { Content = "Español", Tag = new CultureInfo("es-ES") });
            LanguageComboBox.Items.Add(new ComboBoxItem { Content = "Français", Tag = new CultureInfo("fr-FR") });
            LanguageComboBox.Items.Add(new ComboBoxItem { Content = "Italiano", Tag = new CultureInfo("it-IT") });
            LanguageComboBox.Items.Add(new ComboBoxItem { Content = "Português", Tag = new CultureInfo("pt-PT") });
            
            // Setzen Sie die anfängliche Auswahl auf die aktuelle Kultur
            LanguageComboBox.SelectedIndex = 0;

            ((MainWindowViewModel)DataContext).checkViewModel.NoFileSelected += NoFileSelectedMessageBox;
            ((MainWindowViewModel)DataContext).checkViewModel.FileNotYetChecked += NotYetCheckedMessageBox;
            ((MainWindowViewModel)DataContext).OpenUpdateDialog += handleOpenDialogRequest;
            
            Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.RichTextBox(LogBox, theme:RichTextBoxTheme.None).
                MinimumLevel.Debug().CreateLogger();

            Log.Information("GeoRefChecker started");

            Settings.configureOgr();

        }

        public void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var comboBoxItem = (ComboBoxItem)comboBox.SelectedItem;
            if(comboBoxItem.Tag is CultureInfo culture)
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }
            
            UpdateUITexts();
        }

        public void UpdateUITexts()
        {
            LanguageLabel.Content = Properties.Resources.LANGUAGE_LABEL;
            LoadFilesBtn.Content = Properties.Resources.LOAD_IFC_FILES;
            CheckFileBtn.Content = Properties.Resources.CHECK_SELECTED_FILE;
            InputIFCGroupBox.Header = Properties.Resources.INPUT_IFC_FILES;
            StatusLabel.Content = Properties.Resources.STATUS_LABEL;
            LoadedIFCFilesLabel.Content = Properties.Resources.LOADED_IFC_FILES;
            StatusReportGroupBox.Header = Properties.Resources.STATUS_REPORT;
            ShowProtocolBtn.Content = Properties.Resources.SHOW_PROTOCOL;
            LogOutputGroupBox.Header = Properties.Resources.LOG_OUTPUT;
        }   

        public void NoFileSelectedMessageBox(object? sender, EventArgs args)
        {
            MessageBox.Show("No file was selected!", "Missing file", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void NotYetCheckedMessageBox(object? sender, EventArgs args)
        {
            MessageBox.Show("Selected File was not yet checked!", "Missing check result", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void LoadFilesBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "IFC files (*.ifc)|*.ifc|All files (*.*)|*.*";

            var resultDialog = openFileDialog.ShowDialog();

            if (resultDialog == true)
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    ((MainWindowViewModel)DataContext).filePanelViewModel.FilePaths.Add(fileName);
                }
            }
        }

        private void handleOpenDialogRequest(object? sender, EventArgs e)
        {
            var vm = (MainWindowViewModel)DataContext;

            if (string.IsNullOrEmpty(vm.SelectedPath))
            {
                this.NoFileSelectedMessageBox(this, EventArgs.Empty);
            }
            else if (!vm.checkViewModel.CheckerDict.ContainsKey(vm.SelectedPath))
            {
                this.NotYetCheckedMessageBox(this, EventArgs.Empty);
            }
            else
            {
                var dialog = new UpdateGeoRefWindow();
                var updateViewModel = new UpdateViewModel(vm.checkViewModel.CheckerDict[vm.SelectedPath]);
                dialog.DataContext = updateViewModel;
                //dialog.ShowDialog();
                dialog.Title = $"Update {System.IO.Path.GetFileName(vm.SelectedPath)}";
                dialog.Show();
            }

            

        }

        private void LogBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.LogBox.ScrollToEnd();
        }

        private void LevelContextMenu_Opening(object? sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu cm) return;
            if (cm.PlacementTarget is not FrameworkElement fe) return;

            var sourceType = fe.Tag as Type;
            if (sourceType == null) return;

            var allowed = Level00.GetAllowedConversions(sourceType); 

            foreach (var item in cm.Items.OfType<MenuItem>())
            {
                if (allowed != null && item.Tag is Type targetType)
                {
                    item.IsEnabled = allowed.Contains(targetType) && !targetType.Equals(sourceType);
                }
                else
                {
                    item.IsEnabled = false;
                }
            }
        }

        private void OnConvertMenuItemClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem mi) return;
            if (mi.Parent is not ContextMenu cm || cm.PlacementTarget is not FrameworkElement placementTarget) return;

            var sourceType = placementTarget.Tag as Type;
            var targetType = mi.Tag as Type;
            if (sourceType == null || targetType == null) return;

            var vm = this.DataContext as ViewModels.MainWindowViewModel;
            vm?.ChangeLevelCommand?.Execute((sourceType, targetType));
        }
    }
}
