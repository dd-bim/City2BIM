using IFCGeoRefCheckerGUI.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Serilog;

namespace IFCGeoRefCheckerGUI.ViewModels
{
    public class WorkingDirViewModel : BaseViewModel
    {
        string? workingDirPath;
        IEventAggregator eventAggregator;
        //List<Subscription>? subscriptions;

        public string? WorkingDirPath { 
            get => workingDirPath;
            set
            {
                if (workingDirPath != value) {
                    workingDirPath = value;
                    this.RaisePropertyChanged();
                    this.WorkingDirChanged();
                }
            }
        }

        public WorkingDirViewModel(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        private void WorkingDirChanged()
        {
            eventAggregator.Publish(new SelectedWorkingDirMessageObject() { WorkingDir = this.WorkingDirPath });
        }
    }
}
