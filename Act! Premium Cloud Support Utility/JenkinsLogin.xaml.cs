using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Act__Premium_Cloud_Support_Utility
{
    public partial class JenkinsLogin : Window
    {
        public JenkinsLogin()
        {
            InitializeComponent();

            // Load the Jenkins servers
            jenkinsLogin_JenkinsServerSelect_ComboBox.ItemsSource = MainWindow.getAttributesFromXml(MainWindow.jenkinsServerXml, "servers/server", "name");
        }

        private void loginWindowOK_Click(object sender, RoutedEventArgs e)
        {
            if (jenkinsLogin_JenkinsServerSelect_ComboBox.Text != ""
                & loginWindowUName.Text != ""
                & loginWindowPWord.Password != "")
            {
                string selectedItem = jenkinsLogin_JenkinsServerSelect_ComboBox.Text;
                string server = MainWindow.getAttributesFromXml(MainWindow.jenkinsServerXml, "servers/server[@name='" + selectedItem + "']", "id")[0];

                JenkinsTasks.SecureJenkinsCreds(loginWindowUName.Text, loginWindowPWord.Password, server);

                Close();
            }
        }

        private void jenkinsLogin_Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void loginWindowPWord_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                loginWindowOK_Click(sender, e);
            }
        }

        private void jenkinsLogin_JenkinsServerSelect_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedItem = e.AddedItems[0].ToString();
            string url = MainWindow.getValuesFromXml(MainWindow.jenkinsServerXml, "servers/server[@name='" + selectedItem + "']")[0];

            jenkinsLogin_Configure_Hyperlink.NavigateUri = new System.Uri(url + "/me/configure");

            jenkinsLogin_Instructions_Grid.Visibility = Visibility.Visible;
            jenkinsLogin_Credentials_Grid.Visibility = Visibility.Visible;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
