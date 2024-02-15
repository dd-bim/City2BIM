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
            ((MainWindowViewModel)DataContext).checkViewModel.NoWorkingDirSelected += NoWorkingDirSelectedMessageBox;
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
            SetWorkinDirBtn.Content = Properties.Resources.SET_WORKING_DIRECTORY;
            LoadFilesBtn.Content = Properties.Resources.LOAD_IFC_FILES;
            CheckFileBtn.Content = Properties.Resources.CHECK_SELECTED_FILE;
            SetWorkDirGroupBox.Header = Properties.Resources.SET_WORKING_DIRECTORY;
            WorkingDirLabel.Content = Properties.Resources.WORKING_DIRECTORY_LABEL;
            //PathTextBox.Text = Properties.Resources.PATH_TEXTBOX;
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

        public void NoWorkingDirSelectedMessageBox(object? sender, EventArgs args)
        {
            MessageBox.Show("No valid working directory was selected!", "Missing Working Directory", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void SetWorkinDirBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            var dialogResult = dialog.ShowDialog();

            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                ((MainWindowViewModel)DataContext).workingDirViewModel.WorkingDirPath = dialog.SelectedPath;
                Log.Information($"Working Directory changed to {dialog.SelectedPath}");
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

        private void CheckFileBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        /*private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = ((MainWindowViewModel)DataContext);

            var selectedFile = vm.SelectedPath;

            if ( selectedFile == null )
            {
                this.NoFileSelectedMessageBox(this, EventArgs.Empty);
            }

            else if (!vm.checkViewModel.CheckerDict.ContainsKey(selectedFile))
            {
                this.NotYetCheckedMessageBox(this, EventArgs.Empty);
            }

            ((MainWindowViewModel)DataContext).OpenUpdateDialog += (s, ev) =>
            {
                UpdateGeoRefWindow dialog = new UpdateGeoRefWindow();
                dialog.Show();
            };
        }*/

        /*
        private RichTextBoxConsoleTheme customTheme = new RichTextBoxConsoleTheme
        (
            new Dictionary<RichTextBoxThemeStyle, RichTextBoxConsoleThemeStyle>
            {
                [RichTextBoxThemeStyle.Text] = new RichTextBoxConsoleThemeStyle { Foreground = "#000000" },
                [RichTextBoxThemeStyle.SecondaryText] = new RichTextBoxConsoleThemeStyle { Foreground = "#808080" },
                [RichTextBoxThemeStyle.TertiaryText] = new RichTextBoxConsoleThemeStyle { Foreground = "#808080" },
                [RichTextBoxThemeStyle.Invalid] = new RichTextBoxConsoleThemeStyle { Foreground = "#ffff00" },
                [RichTextBoxThemeStyle.Null] = new RichTextBoxConsoleThemeStyle { Foreground = "#696969" },
                [RichTextBoxThemeStyle.Name] = new RichTextBoxConsoleThemeStyle { Foreground = "#696969" },
                [RichTextBoxThemeStyle.String] = new RichTextBoxConsoleThemeStyle { Foreground = "#696969" },
                [RichTextBoxThemeStyle.Number] = new RichTextBoxConsoleThemeStyle { Foreground = "#696969" },
                [RichTextBoxThemeStyle.Boolean] = new RichTextBoxConsoleThemeStyle { Foreground = "#696969" },
                [RichTextBoxThemeStyle.Scalar] = new RichTextBoxConsoleThemeStyle { Foreground = "#696969" },
                [RichTextBoxThemeStyle.LevelVerbose] = new RichTextBoxConsoleThemeStyle { Foreground = "#c0c0c0", Background = "#808080" },
                [RichTextBoxThemeStyle.LevelDebug] = new RichTextBoxConsoleThemeStyle { Foreground = "#ffffff", Background = "#808080" },
                [RichTextBoxThemeStyle.LevelInformation] = new RichTextBoxConsoleThemeStyle { Foreground = "#ffffff", Background = "#0000ff" },
                [RichTextBoxThemeStyle.LevelWarning] = new RichTextBoxConsoleThemeStyle { Foreground = "#808080", Background = "#ffff00" },
                [RichTextBoxThemeStyle.LevelError] = new RichTextBoxConsoleThemeStyle { Foreground = "#ffffff", Background = "#ff0000" },
                [RichTextBoxThemeStyle.LevelFatal] = new RichTextBoxConsoleThemeStyle { Foreground = "#ffffff", Background = "#ff0000" },
            }
        );
        */

    }
}
