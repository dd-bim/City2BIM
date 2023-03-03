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

namespace IFCGeoRefCheckerGUI
{
    /// <summary>
    /// Interaction logic for MainWindowAllInOne.xaml
    /// </summary>
    public partial class MainWindowAllInOne : Window
    {

        //public RichTextBoxConsoleTheme logTheme = new RichTextBoxConsoleTheme(RichTextBoxConsoleTheme.Colored);

        public MainWindowAllInOne()
        {
            InitializeComponent();
            ((MainWindowViewModel)DataContext).checkViewModel.NoFileSelected += NoFileSelectedMessageBox;
            ((MainWindowViewModel)DataContext).checkViewModel.NoWorkingDirSelected += NoWorkingDirSelectedMessageBox;
            ((MainWindowViewModel)DataContext).checkViewModel.FileNotYetChecked += NotYetCheckedMessageBox;

            Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.RichTextBox(LogBox, theme:RichTextBoxTheme.None).
                MinimumLevel.Debug().CreateLogger();

            Log.Information("GeoRefChecker started");

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
