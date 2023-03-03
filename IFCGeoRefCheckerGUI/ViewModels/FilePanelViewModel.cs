using IFCGeoRefCheckerGUI.Messaging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace IFCGeoRefCheckerGUI.ViewModels
{
    public class FilePanelViewModel : BaseViewModel
    {
        IEventAggregator eventAggregator;
        //Subscription? eventSubscription;

        private ObservableCollection<String> filePaths = new ObservableCollection<string>();

        public ObservableCollection<String> FilePaths
        {
            get => filePaths;
            set
            {
                if (filePaths != value)
                {
                    filePaths = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private string? selectedPath;
        public string? SelectedPath
        {
            get => selectedPath;
            set
            {
                if (selectedPath != value)
                {
                    selectedPath = value;
                    this.RaisePropertyChanged();
                    this.SelectedPathChanged();
                }
            }
        }

        public FilePanelViewModel(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }
        private void SelectedPathChanged()
        {
            eventAggregator.Publish(new SelectedPathMessageObject() { SelectedPath = this.selectedPath });
        }

    }
}
