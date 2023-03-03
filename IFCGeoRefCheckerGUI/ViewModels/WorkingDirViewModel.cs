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

        public DelegateCommand? DirChooseCommand { get; set; }

        public WorkingDirViewModel(IEventAggregator eventAggregator)
        {
            //this.WorkingDirPath = "Please choose path!";
            this.eventAggregator = eventAggregator;

            this.DirChooseCommand = new DelegateCommand(
                (o) =>
                {
                    var dialog = new System.Windows.Forms.FolderBrowserDialog();

                    var dialogResult = dialog.ShowDialog();
                    
                    if (dialogResult == System.Windows.Forms.DialogResult.OK)
                    {
                        this.WorkingDirPath = dialog.SelectedPath;
                        this.RaisePropertyChanged(nameof(WorkingDirPath));
                    }
                }
            );
        }

        private void WorkingDirChanged()
        {
            eventAggregator.Publish(new SelectedWorkingDirMessageObject() { WorkingDir = this.WorkingDirPath });
        }
    }
}
