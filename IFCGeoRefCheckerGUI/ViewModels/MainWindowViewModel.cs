using IFCGeoRefCheckerGUI.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFCGeoRefCheckerGUI.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public WorkingDirViewModel workingDirViewModel { get; }
        public FilePanelViewModel filePanelViewModel { get; }

        public CheckViewModel checkViewModel { get; }

        public EventAggregator eventAggregator = new EventAggregator();

        public MainWindowViewModel() {
            
            workingDirViewModel = new WorkingDirViewModel(this.eventAggregator);
            filePanelViewModel = new FilePanelViewModel(this.eventAggregator);
            checkViewModel = new CheckViewModel(this.eventAggregator);

        }
    }
}
