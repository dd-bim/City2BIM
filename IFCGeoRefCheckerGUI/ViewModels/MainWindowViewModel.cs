using IFCGeoRefCheckerGUI.Messaging;
using IFCGeorefShared.Levels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace IFCGeoRefCheckerGUI.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public WorkingDirViewModel workingDirViewModel { get; }
        public FilePanelViewModel filePanelViewModel { get; }

        public CheckViewModel checkViewModel { get; }

        public EventAggregator eventAggregator = new EventAggregator();

        public ICommand OpenUpdateDialogCommand { get; init; }

        public DelegateCommand ChangeLevelCommand { get; }

        public event EventHandler? OpenUpdateDialog;

        List<Subscription>? eventSubscriptions;

        private string? selectedPath;
        public string? SelectedPath {
            get => selectedPath;
            set
            {
                if (selectedPath != value)
                {
                    selectedPath = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        //private static ResourceManager rm = new ResourceManager("IFCGeoRefCheckerGUI.Properties.Resources", Assembly.GetExecutingAssembly());
        private static ResourceManager rm = new ResourceManager("IFCGeoRefCheckerGUI.Properties.Resources", typeof(MainWindowViewModel).Assembly);

        public MainWindowViewModel() {

            //CultureInfo culture = new("de-DE");
            //System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            //System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

            workingDirViewModel = new WorkingDirViewModel(this.eventAggregator);
            filePanelViewModel = new FilePanelViewModel(this.eventAggregator);
            checkViewModel = new CheckViewModel(this.eventAggregator);

            //string? value = rm.GetString("LOAD_IFC_FILES");

            this.eventSubscriptions = new List<Subscription>
            {
                eventAggregator.Subscribe<SelectedPathMessageObject>(spmo => this.SelectedPath = spmo.SelectedPath!)
                //eventAggregator.Subscribe<SelectedWorkingDirMessageObject>(swdmo => NewWorkingDirReceived(swdmo.WorkingDir))
            };

            this.OpenUpdateDialogCommand = new DelegateCommand((o) =>
            {
                this.OpenUpdateDialog?.Invoke(this, EventArgs.Empty);
            });

            this.ChangeLevelCommand = new DelegateCommand((param) =>
            {
                ExecuteChangeLevel(param);
            });
        }

        private void ExecuteChangeLevel(object? parameter)
        {
            Type? sourceType = null;
            Type? targetType = null;

            if (parameter is ValueTuple<Type, Type> vt)
            {
                sourceType = vt.Item1;
                targetType = vt.Item2;
            }
            else if (parameter is object[] arr && arr.Length == 2 && arr[0] is Type a && arr[1] is Type b)
            {
                // if called as object[] from XAML, e.g. CommandParameter="{x:Static local:MainWindowViewModel.ChangeLevelCommandParameter_LoGeoRef30To40}"
                sourceType = a;
                targetType = b;
            }
            else if (parameter is string sParam)
            {
                // string-based signature: "LoGeoRef30|LoGeoRef40"
                var parts = sParam.Split('|');
                if (parts.Length == 2)
                {
                    string src = parts[0].Trim();
                    string tgt = parts[1].Trim();
                    sourceType = src switch
                    {
                        "LoGeoRef10" => typeof(IFCGeorefShared.Levels.Level10),
                        "LoGeoRef20" => typeof(IFCGeorefShared.Levels.Level20),
                        "LoGeoRef30" => typeof(IFCGeorefShared.Levels.Level30),
                        "LoGeoRef40" => typeof(IFCGeorefShared.Levels.Level40),
                        "LoGeoRef50" => typeof(IFCGeorefShared.Levels.Level50),
                        _ => null
                    };
                    targetType = tgt switch
                    {
                        "LoGeoRef10" => typeof(IFCGeorefShared.Levels.Level10),
                        "LoGeoRef20" => typeof(IFCGeorefShared.Levels.Level20),
                        "LoGeoRef30" => typeof(IFCGeorefShared.Levels.Level30),
                        "LoGeoRef40" => typeof(IFCGeorefShared.Levels.Level40),
                        "LoGeoRef50" => typeof(IFCGeorefShared.Levels.Level50),
                        _ => null
                    };
                }
            }

            if (sourceType == null || targetType == null) return;

            var selPath = this.filePanelViewModel.SelectedPath;
            if (string.IsNullOrEmpty(selPath))
                return;

            if (!this.checkViewModel.CheckerDict.ContainsKey(selPath))
                return;

            var checker = this.checkViewModel.CheckerDict[selPath];

            // Get the first fullfilled level instance corresponding to the source type
            Level00 lvl;
            if (sourceType == typeof(IFCGeorefShared.Levels.Level10))
                lvl = checker.LoGeoRef10.First(x => x.IsFullFilled == true);
            else if (sourceType == typeof(IFCGeorefShared.Levels.Level20))
                lvl = checker.LoGeoRef20.First(x => x.IsFullFilled == true);
            else if (sourceType == typeof(IFCGeorefShared.Levels.Level30))
                lvl = checker.LoGeoRef30.First(x => x.IsFullFilled == true);
            else if (sourceType == typeof(IFCGeorefShared.Levels.Level40))
                lvl = checker.LoGeoRef40.First(x => x.IsFullFilled == true);
            else if (sourceType == typeof(IFCGeorefShared.Levels.Level50))
                lvl = checker.LoGeoRef50.First(x => x.IsFullFilled == true);
            else
                return;

            // Convert to target level
            try
            {
                lvl.ConvertToLevel(targetType, checker);
            }
            catch (Exception)
            {
                // Exception handling can be implemented here, e.g. logging the error or showing a message box to the user
            }

            // Update
            this.checkViewModel.CheckerDict[selPath] = checker;
            this.checkViewModel.CheckerResults = checker.getCheckResults();
        }
    }
}
