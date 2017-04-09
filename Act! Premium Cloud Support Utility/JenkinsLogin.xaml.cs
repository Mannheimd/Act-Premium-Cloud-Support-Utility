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
            JenkinsTasks.SecureJenkinsCreds(loginWindowUName.Text, loginWindowPWord.Password, "UST1");

            Close();
        }

        private void loginWindowPWord_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                loginWindowOK_Click(sender, e);
            }
        }
    }
}
