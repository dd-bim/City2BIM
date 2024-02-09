using IFCGeoRefCheckerGUI.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using System.Globalization;
using System.Resources;
using System.Windows.Forms;

namespace IFCGeoRefCheckerGUI.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public WorkingDirViewModel workingDirViewModel { get; }
        public FilePanelViewModel filePanelViewModel { get; }

        public CheckViewModel checkViewModel { get; }

        public EventAggregator eventAggregator = new EventAggregator();

        public ICommand OpenUpdateDialogCommand { get; init; }

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

            CultureInfo culture = new CultureInfo("de-DE");
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

            workingDirViewModel = new WorkingDirViewModel(this.eventAggregator);
            filePanelViewModel = new FilePanelViewModel(this.eventAggregator);
            checkViewModel = new CheckViewModel(this.eventAggregator);

            string? value = rm.GetString("LOAD_IFC_FILES");

            this.eventSubscriptions = new List<Subscription>
            {
                eventAggregator.Subscribe<SelectedPathMessageObject>(spmo => this.SelectedPath = spmo.SelectedPath!)
                //eventAggregator.Subscribe<SelectedWorkingDirMessageObject>(swdmo => NewWorkingDirReceived(swdmo.WorkingDir))
            };

            this.OpenUpdateDialogCommand = new DelegateCommand((o) =>
            {
                this.OpenUpdateDialog?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}
