using IFCGeoRefCheckerGUI.Messaging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
            // attach to initial collection so last-added item becomes selected
            this.filePaths.CollectionChanged += FilePaths_CollectionChanged;
        }

        private void FilePaths_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e == null) return;

            // Wenn Elemente hinzugefügt wurden: wähle das zuletzt hinzugefügte
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
            {
                var lastAdded = e.NewItems[e.NewItems.Count - 1] as string;
                if (!string.IsNullOrEmpty(lastAdded))
                    SelectedPath = lastAdded;
            }
            // Bei Reset (z.B. Clear) wähle das letzte Element oder null
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                SelectedPath = filePaths.LastOrDefault();
            }
        }

        private void SelectedPathChanged()
        {
            eventAggregator.Publish(new SelectedPathMessageObject() { SelectedPath = this.selectedPath });
        }

    }
}
