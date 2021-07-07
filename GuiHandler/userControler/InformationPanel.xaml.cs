﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace GuiHandler.userControler
{
    /// <summary>
    /// Interaktionslogik für InformationPanel.xaml
    /// </summary>
    public partial class InformationPanel : UserControl
    {
        public InformationPanel()
        {
            InitializeComponent();
            DataContext = this;
        }

        string _fileName;

        public string fileName 
        { 
            get { return _fileName; }
            set
            {
                if (_fileName == value) return;
                _fileName = value;
                var handler = StringPropertyChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        public event EventHandler StringPropertyChanged;
    }

}
