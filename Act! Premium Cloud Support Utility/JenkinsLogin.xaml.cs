using System;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Act__Premium_Cloud_Support_Utility
{
    public partial class JenkinsLogin : Window
    {
        public JenkinsLogin()
        {
            InitializeComponent();
        }

        private void loginWindowOK_Click(object sender, RoutedEventArgs e)
        {
            GetUserPass();

            Close();
        }

        private void loginWindowPWord_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                loginWindowOK_Click(sender, e);
            }
        }

        private void GetUserPass()
        {
            byte[] creds = UTF8Encoding.UTF8.GetBytes(loginWindowUName.Text + ":" + loginWindowPWord.Password);
            MainWindow.encodedCreds = Convert.ToBase64String(creds);
        }
    }
}
