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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace City2RVT.GUI.Surveyorsplan2BIM
{
    /// <summary>
    /// Interaktionslogik für Surveyorsplan_ImportUI.xaml
    /// </summary>
    public partial class Surveyorsplan_ImportUI : Window
    {
        #region config
        /// <summary>
        /// return to start surv import in cmd 
        /// </summary>
        public bool startSurvImport { get { return startImport; } }
        
        /// <summary>
        /// value to be specified that the import should be started
        /// </summary>
        private bool startImport { get; set; } = false;
        #endregion config

        public Surveyorsplan_ImportUI()
        {
            InitializeComponent();
        }


    }
}
