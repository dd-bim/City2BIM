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

namespace CityBIM.GUI.DataCat
{
    /// <summary>
    /// Interaction logic for LoginScreen.xaml
    /// </summary>
    public partial class LoginScreen : Window
    {
        public string UserName;
        public string passWord;
        public string endPointUrl;

        public LoginScreen()
        {
            InitializeComponent();
        }

        private void submit_click(object sender, RoutedEventArgs e)
        {
            UserName = userNameBox.Text;
            passWord = PassWordBox.Password;
            endPointUrl = ServerBox.Text;
            this.DialogResult = true;
            this.Close();
        }
    }
}
