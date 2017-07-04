using Jenkins_Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;

namespace Act__Premium_Cloud_Support_Utility
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            JenkinsTasks.loadJenkinsServers();
        }

        private async void runLookupCustomer()
        {
            // Disable the server select, search and lookup results sections
            setServerSelectEnabledState(false);
            setLookupAccountEnabledState(false);
            setLookupResultsEnabledState(false);

            string searchString = textBox_LookupValue.Text.Trim();

            // Check that search criteria have been entered
            if (searchString == "" || jenkinsServerSelect_ComboBox.Text == "")
            {
                lookupAccount_LookupStatus_TextBox.Content = "Enter search criteria";

                setServerSelectEnabledState(true);
                setLookupAccountEnabledState(true);

                return;
            }

            // Get the currently selected server and confirm there are some stored credentials
            JenkinsServer server = jenkinsServerSelect_ComboBox.SelectedItem as JenkinsServer;

            if (JenkinsTasks.UnsecureJenkinsCreds(server.id) == null)
            {
                lookupAccount_LookupStatus_TextBox.Content = "Login error";

                setServerSelectEnabledState(true);
                setLookupAccountEnabledState(true);

                return;
            }

            lookupAccount_LookupStatus_TextBox.Content = "Lookup running...";

            // Post a request to build LookupCustomer and wait for a response
            string lookupCustomerOutput = await JenkinsTasks.runJenkinsBuild(server, @"/job/CloudOps1-LookupCustomer/buildWithParameters?LookupCustomerBy="
                + comboBox_LookupBy.SelectedValue.ToString()
                + "&LookupValue="
                + searchString
                + "&delay=0sec");

            // Check that the output is valid
            if (SearchString(lookupCustomerOutput, "Searching " + comboBox_LookupBy.SelectedValue.ToString() + " for ", "...") != searchString)
            {
                lookupAccount_LookupStatus_TextBox.Content = "Invalid results, try again";

                setServerSelectEnabledState(true);
                setLookupAccountEnabledState(true);

                return;
            }

            // Check if customer couldn't be found
            if (lookupCustomerOutput.Contains("Unable to find customer by"))
            {
                lookupAccount_LookupStatus_TextBox.Content = "Unable to locate account";

                setServerSelectEnabledState(true);
                setLookupAccountEnabledState(true);

                return;
            }

            // Pulling strings out of output (lines end with return, null value doesn't do the trick)
            string iitid = SearchString(lookupCustomerOutput, "IITID: ", @"
");
            string accountName = SearchString(lookupCustomerOutput, "Account Name: ", @"
");
            string email = SearchString(lookupCustomerOutput, "Email: ", @"
");
            string createDate = SearchString(lookupCustomerOutput, "Create Date: ", @"
");
            string trialOrPaid = SearchString(lookupCustomerOutput, "Trial or Paid: ", @"
");
            string serialNumber = SearchString(lookupCustomerOutput, "Serial Number: ", @"
");
            string seatCount = SearchString(lookupCustomerOutput, "Seat Count: ", @"
");
            string suspendStatus = SearchString(lookupCustomerOutput, "Suspend status: ", @"
");
            string archiveStatus = SearchString(lookupCustomerOutput, "Archive status: ", @"
");
            string siteName = SearchString(lookupCustomerOutput, "Site Name: ", @"
");
            string iisServer = SearchString(lookupCustomerOutput, "IIS Server: ", @"
");
            string loginUrl = SearchString(lookupCustomerOutput, "URL: ", @"
");
            string uploadUrl = SearchString(lookupCustomerOutput, "Upload: ", @"
");
            string zuoraAccount = SearchString(lookupCustomerOutput, "Zuora Account: ", @"
");
            string deleteStatus = SearchString(lookupCustomerOutput, "Delete archive status: ", @"
");

            // Get a list of databases from the output
            List<Database> databaseList = ParseForDatabases(lookupCustomerOutput);

            // Fill in the UI with data
            lookupResults_AccountName_TextBox.Text = accountName;
            lookupResults_SiteName_TextBox.Text = siteName;
            lookupResults_AccountNumber_TextBox.Text = zuoraAccount;
            lookupResults_LoginUrl_TextBox.Text = loginUrl;
            lookupResults_UploadUrl_TextBox.Text = uploadUrl;
            lookupResults_CreateDate_TextBox.Text = createDate;
            lookupResults_PrimaryEmail_TextBox.Text = email;
            lookupResults_TrialPaid_TextBox.Text = trialOrPaid;
            lookupResults_SeatCount_TextBox.Text = seatCount;
            lookupResults_SerialNumber_TextBox.Text = serialNumber;
            lookupResults_SuspendStatus_TextBox.Text = suspendStatus;
            lookupResults_ArchiveStatus_TextBox.Text = archiveStatus;
            lookupResults_DeleteStatus_TextBox.Text = deleteStatus;
            lookupResults_IISServer_TextBox.Text = iisServer;
            lookupResults_IITID_TextBox.Text = iitid;

            // Populate the Database list
            lookupResults_Databases_ListView.ItemsSource = databaseList;

            lookupAccount_LookupStatus_TextBox.Content = "Lookup complete";

            setServerSelectEnabledState(true);
            setLookupAccountEnabledState(true);
            setLookupResultsEnabledState(true);
        }

        private async void unlockDatabase(string databaseName, string sqlServer)
        {
            setServerSelectEnabledState(false);
            setLookupAccountEnabledState(false);
            setLookupResultsEnabledState(false);

            databaseTasks_UnlockStatus_Label.Content = "Unlocking...";

            // Get the currently selected server
            JenkinsServer server = jenkinsServerSelect_ComboBox.SelectedItem as JenkinsServer;

            // Post a request to build LookupCustomer and wait for a response
            if (JenkinsTasks.UnsecureJenkinsCreds(server.id) != null)
            {
                string output = await JenkinsTasks.runJenkinsBuild(server, @"/job/CloudOps1-UnlockDatabase/buildWithParameters?&SQLServer="
                    + sqlServer
                    + "&DatabaseName="
                    + databaseName
                    + "&delay=0sec");

                // Pulling strings out of output (lines end with return, null value doesn't do the trick)
                string outputSqlServer = SearchString(output, "Found SQL Server: ", @"
");
                string outputDatabaseName = SearchString(output, "Unlocking database: ", @"
");

                if (outputSqlServer == sqlServer && outputDatabaseName == databaseName)
                {
                    databaseTasks_UnlockStatus_Label.Content = "Database unlocked";
                }
                else
                {
                    databaseTasks_UnlockStatus_Label.Content = "Unlock failed";
                }
            }
            else
            {
                databaseTasks_UnlockStatus_Label.Content = "Login error";
            }

            setServerSelectEnabledState(true);
            setLookupAccountEnabledState(true);
            setLookupResultsEnabledState(true);
        }

        private async void getDatabaseUsers(Database database)
        {
            setServerSelectEnabledState(false);
            setLookupAccountEnabledState(false);
            setLookupResultsEnabledState(false);

            databaseTasks_GetUsersStatus_Label.Content = "Working...";

            // Clear the current database user list
            database.users.Clear();

            // Get the currently selected server
            JenkinsServer server = jenkinsServerSelect_ComboBox.SelectedItem as JenkinsServer;

            // Post a request to build LookupCustomer and wait for a response
            if (JenkinsTasks.UnsecureJenkinsCreds(server.id) != null)
            {
                string output = await JenkinsTasks.runJenkinsBuild(server, @"/job/CloudOps1-ListCustomerDatabaseUsers-Machine/buildWithParameters?&SQLServer="
                    + database.server
                    + "&DatabaseName="
                    + database.name
                    + "&delay=0sec");

                // Pulling strings out of output (lines end with return, null value doesn't do the trick)
                string outputDatabaseName = SearchString(output, "Changed database context to '", "'.");

                if (outputDatabaseName.ToLower() == database.name.ToLower())
                {
                    database.users = ParseForDatabaseUsers(output);

                    lookupResults_DatabaseUsers_ListView.ItemsSource = database.users;

                    databaseTasks_GetUsersStatus_Label.Content = "Loaded users";
                }
                else
                {
                    databaseTasks_GetUsersStatus_Label.Content = "Request failed";
                }
            }
            else
            {
                databaseTasks_GetUsersStatus_Label.Content = "Login error";
            }

            setServerSelectEnabledState(true);
            setLookupAccountEnabledState(true);
            setLookupResultsEnabledState(true);
        }

        private async void resendWelcomeEmail(string accountIITID, string accountEmail)
        {
            bool sendType = false; // true is alt email, false is default email
            string sendTo = null; // alt email address
            bool send = false; // output from send or cancel

            ResendWelcomeEmail resendWelcomeEmail = new ResendWelcomeEmail(accountEmail);
            resendWelcomeEmail.resultBool += value => sendType = value;
            resendWelcomeEmail.resultString += value => sendTo = value;
            resendWelcomeEmail.resultSend += value => send = value;
            resendWelcomeEmail.ShowDialog();

            if (send)
            {
                setServerSelectEnabledState(false);
                setLookupAccountEnabledState(false);
                setLookupResultsEnabledState(false);

                accountTasks_WelcomeEmailStatus_Label.Content = "Sending...";

                // set accountEmail to null if not needed, else set it to specified address
                if (sendType)
                {
                    accountEmail = sendTo;
                }
                else
                {
                    accountEmail = null;
                }

                // Get the currently selected server
                JenkinsServer server = jenkinsServerSelect_ComboBox.SelectedItem as JenkinsServer;

                // Post a request to build LookupCustomer and wait for a response
                if (JenkinsTasks.UnsecureJenkinsCreds(server.id) != null)
                {
                    string output = await JenkinsTasks.runJenkinsBuild(server, @"/job/CloudOps1-ResendWelcomeEmail/buildWithParameters?&IITID="
                        + accountIITID
                        + "&AltEmailAddress="
                        + accountEmail
                        + "&delay=0sec");

                    // Pulling strings out of output (lines end with return, null value doesn't do the trick)
                    string outputIITID = SearchString(output, "IITID: ", @"
");
                    string outputEmail = SearchString(output, "Email Address: ", @"
");

                    if (outputIITID == accountIITID && outputEmail == accountEmail)
                    {
                        accountTasks_WelcomeEmailStatus_Label.Content = "Send complete";
                    }
                    else if (outputIITID == accountIITID && !sendType)
                    {
                        accountTasks_WelcomeEmailStatus_Label.Content = "Send complete";
                    }
                    else
                    {
                        accountTasks_WelcomeEmailStatus_Label.Content = "Send failed";
                    }
                }
                else
                {
                    accountTasks_WelcomeEmailStatus_Label.Content = "Login error";
                }
            }

            setServerSelectEnabledState(true);
            setLookupAccountEnabledState(true);
            setLookupResultsEnabledState(true);
        }

        private async void getTimeout(string siteName, string iisServer)
        {
            setServerSelectEnabledState(false);
            setLookupAccountEnabledState(false);
            setLookupResultsEnabledState(false);

            accountTasks_GetTimeoutStatus_Label.Content = "Working...";

            // Get the currently selected server
            JenkinsServer server = jenkinsServerSelect_ComboBox.SelectedItem as JenkinsServer;

            // Post a request to build LookupCustomer and wait for a response
            if (JenkinsTasks.UnsecureJenkinsCreds(server.id) != null)
            {
                string output = await JenkinsTasks.runJenkinsBuild(server, @"/job/CloudOps1-ListExistingClientTimeout/buildWithParameters?&SiteName="
                    + siteName
                    + "&IISServer="
                    + iisServer
                    + "&delay=0sec");

                // Pulling strings out of output (lines end with return, null value doesn't do the trick)
                string outputSiteName = SearchString(output, "Site ", " on server");
                string outputIISServer = SearchString(output, "on server ", @"
");
                string outputCurrentTimeout = SearchString(output, "Current Timeout: ", @"
");

                if (outputSiteName == siteName && outputIISServer == iisServer)
                {
                    accountTasks_GetTimeoutStatus_Label.Content = outputCurrentTimeout + " minutes(s)";
                }
                else
                {
                    accountTasks_GetTimeoutStatus_Label.Content = "Query error";
                }
            }
            else
            {
                accountTasks_GetTimeoutStatus_Label.Content = "Login error";
            }

            setServerSelectEnabledState(true);
            setLookupAccountEnabledState(true);
            setLookupResultsEnabledState(true);
        }

        private async void updateTimeout(string siteName, string iisServer)
        {
            string newValue = null; // new timeout value to set
            bool proceed = false; // output from send or cancel

            UpdateTimeoutValue updateTimeoutValue = new UpdateTimeoutValue();
            updateTimeoutValue.resultValue += value => newValue = value;
            updateTimeoutValue.resultProceed += value => proceed = value;
            updateTimeoutValue.ShowDialog();

            if (proceed)
            {
                setServerSelectEnabledState(false);
                setLookupAccountEnabledState(false);
                setLookupResultsEnabledState(false);

                accountTasks_UpdateTimeoutStatus_Label.Content = "Updating...";

                // Get the currently selected server
                JenkinsServer server = jenkinsServerSelect_ComboBox.SelectedItem as JenkinsServer;

                // Post a request to build LookupCustomer and wait for a response
                if (JenkinsTasks.UnsecureJenkinsCreds(server.id) != null)
                {
                    string output = await JenkinsTasks.runJenkinsBuild(server, @"/job/CloudOps1-UpdateExistingClientTimeout/buildWithParameters?&SiteName="
                        + siteName
                        + "&IISServer="
                        + iisServer
                        + "&Timeout="
                        + newValue
                        + "&delay=0sec");

                    // Pulling strings out of output (lines end with return, null value doesn't do the trick)
                    string outputSiteName = SearchString(output, "Updating customer ", " on server ");
                    string outputIISServer = SearchString(output, "on server ", @" 
");
                    string outputTimeout = SearchString(output, "Changing Timeout to: ", @"
 ");

                    if (outputSiteName == siteName && outputIISServer == iisServer && outputTimeout == newValue)
                    {
                        accountTasks_UpdateTimeoutStatus_Label.Content = "Updated";
                    }
                    else
                    {
                        accountTasks_UpdateTimeoutStatus_Label.Content = "Query error";
                    }
                }
                else
                {
                    accountTasks_UpdateTimeoutStatus_Label.Content = "Login error";
                }
            }

            setServerSelectEnabledState(true);
            setLookupAccountEnabledState(true);
            setLookupResultsEnabledState(true);
        }

        private async void resetUserPassword(string databaseName, string databaseServer, string userName, JenkinsServer server)
        {
            setServerSelectEnabledState(false);
            setLookupAccountEnabledState(false);
            setLookupResultsEnabledState(false);

            userTasks_ResetPasswordStatus_Label.Content = "Resetting...";

            // Post a request to build ResetPassword and wait for a response
            if (JenkinsTasks.UnsecureJenkinsCreds(server.id) != null)
            {
                string output = await JenkinsTasks.runJenkinsBuild(server, @"/job/CloudOps1-ResetCustomerLoginPassword/buildWithParameters?&SQLServer="
                    + databaseServer
                    + "&DatabaseName="
                    + databaseName
                    + "&UserName="
                    + userName
                    + "&delay=0sec");

                string outputDatabaseName = SearchString(output, "Changed database context to '", "'.");
                bool oneRowAffected = output.Contains("(1 rows affected)");

                if (outputDatabaseName == databaseName && oneRowAffected == true)
                {
                    userTasks_ResetPasswordStatus_Label.Content = "Reset to 'Actsoftware'";
                }
                else
                {
                    userTasks_ResetPasswordStatus_Label.Content = "Error resetting password";
                }
            }
            else
            {
                userTasks_ResetPasswordStatus_Label.Content = "Login error";
            }

            setServerSelectEnabledState(true);
            setLookupAccountEnabledState(true);
            setLookupResultsEnabledState(true);
        }

        public static List<string> getValuesFromXml(XmlDocument xmlDoc, string path)
        {
            XmlNodeList xmlNodes = xmlDoc.SelectNodes(path);

            List<string> resultList = new List<string>();

            foreach (XmlNode node in xmlNodes)
            {
                resultList.Add(node.InnerXml);
            }

            return resultList;
        }

        public static List<string> getAttributesFromXml(XmlDocument xmlDoc, string path, string attribute)
        {
            XmlNodeList xmlNodes = xmlDoc.SelectNodes(path);

            List<string> resultList = new List<string>();

            foreach (XmlNode node in xmlNodes)
            {
                resultList.Add(node.Attributes[attribute].Value);
            }

            return resultList;
        }

        private string SearchString(string mainText, string startString, string endString)
        {
            try
            {
                string split1 = mainText.Split(new string[] { startString }, StringSplitOptions.None)[1];
                return split1.Split( new string[] { endString }, StringSplitOptions.None)[0];
            }
            catch
            {
                return null;
            }
        }

        private List<Database> ParseForDatabases(string mainText)
        {
            string workingText = mainText;
            List<Database> list = new List<Database>();
            try
            {
                // Get lines with databases on
                string[] lines = workingText.Split(new string[] { "Database: " }, StringSplitOptions.None);

                // For each line, separate the name and server
                // Loop starts at line 1 rather than 0
                for (int i = 1; i < lines.Length; i++)
                {
                    Database database = new Database();

                    database.name = (lines[i].Split(new string[] { " | Server: " }, StringSplitOptions.None)[0]).Split(null)[0];
                    database.server = (lines[i].Split(new string[] { " | Server: " }, StringSplitOptions.None)[1]).Split(null)[0];

                    list.Add(database);
                }
            }
            catch (Exception error)
            {
                MessageBox.Show("Error occurred whilst getting database list:\n\n" + error.Message);
            }

            return list;
        }

        private List<DatabaseUser> ParseForDatabaseUsers(string mainText)
        {
            string workingText = mainText;
            List<DatabaseUser> list = new List<DatabaseUser>();
            try
            {
                // Cut data to after the line of ----¦----¦----¦---- nonsense
                workingText = workingText.Split(new string[] { @"----------¦----------¦----------¦-------------¦-------------¦----------------¦---------¦----------------¦-----------¦--------------¦---------------
" }, StringSplitOptions.None)[1];

                // Cut data to before "<linebreak><linebreak>(1 rows affected)"
                workingText = workingText.Split(new string[] { @"

(1 rows affected)" }, StringSplitOptions.None)[0];

                // Get all lines
                string[] lines = workingText.Split(new string[] { @"
" }, StringSplitOptions.None);

                // For each line, get the user information
                // Loop starts at line 2 rather than 0
                foreach (string line in lines)
                {
                    DatabaseUser user = new DatabaseUser();

                    // Split the line up into individual values
                    string[] lineSplit = line.Split('¦');

                    // Grab the needed information
                    user.loginName = lineSplit[0];
                    user.lastLogin = lineSplit[9];
                    user.role = lineSplit[6];

                    list.Add(user);
                }
            }
            catch (Exception error)
            {
                MessageBox.Show("Error occurred whilst getting database users:\n\n" + error.Message);
            }

            return list;
        }

        async Task Delay(int time)
        {
            await Task.Delay(time);
        }

        private async void jenkinsServerSelect_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                setServerSelectEnabledState(false);
                setLookupResultsEnabledState(false);

                // Get server ID of selected server from jenkinsServerXml
                JenkinsServer server = e.AddedItems[0] as JenkinsServer;

                // Trigger update on UI with login status
                jenkinsServerSelect_LoginStatus_Label.Content = "Checking login...";
                jenkinsServerSelect_Grid.ClearValue(BackgroundProperty);

                // Run a GET request to retrieve the user's login data
                bool loginStatus = await JenkinsTasks.checkServerLogin(server);
                if (loginStatus)
                {
                    jenkinsServerSelect_LoginStatus_Label.Content = "Login succeeded.";
                    
                    setLookupAccountEnabledState(true);
                }
                else
                {
                    jenkinsServerSelect_LoginStatus_Label.Content = "Login failed.";
                    jenkinsServerSelect_Grid.SetValue(BackgroundProperty, new SolidColorBrush(Color.FromRgb(254, 80, 0)));
                    
                    setLookupAccountEnabledState(false);
                }

                setServerSelectEnabledState(true);
            }
        }

        private void jenkinsServerSelect_Configure_Button_Click(object sender, RoutedEventArgs e)
        {
            JenkinsLogin jenkinsLogin = new JenkinsLogin();
            jenkinsLogin.Show();
        }

        private void lookupResults_Databases_ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                Database database = e.AddedItems[0] as Database;

                lookupResults_DatabaseUsers_ListView.ItemsSource = database.users;
            }
        }

        private void setServerSelectEnabledState(bool state)
        {
            jenkinsServerSelect_Grid.IsEnabled = state;
        }

        private void setLookupAccountEnabledState(bool state)
        {
            lookupAccount_Grid.IsEnabled = state;
        }

        private void setLookupResultsEnabledState(bool state)
        {
            lookupResults_Grid.IsEnabled = state;
        }
    }

    public class Database
    {
        public string name { get; set; }
        public string server { get; set; }
        public List<DatabaseUser> users = new List<DatabaseUser>();
    }

    public class DatabaseUser
    {
        public string loginName { get; set; }
        public string role { get; set; }
        public string lastLogin { get; set; }
    }
}
