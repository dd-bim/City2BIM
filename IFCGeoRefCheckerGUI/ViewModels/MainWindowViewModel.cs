using IFCGeoRefCheckerGUI.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public MainWindowViewModel() {
            
            workingDirViewModel = new WorkingDirViewModel(this.eventAggregator);
            filePanelViewModel = new FilePanelViewModel(this.eventAggregator);
            checkViewModel = new CheckViewModel(this.eventAggregator);

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
