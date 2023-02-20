using System.Windows;

namespace DataCatPlugin.GUI
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
