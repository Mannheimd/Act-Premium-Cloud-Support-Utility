using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public static XmlDocument jenkinsServerXml = new XmlDocument();

        public MainWindow()
        {
            InitializeComponent();

            PopulateDropDowns();
        }

        private void button_RunLookupCustomer_Click(object sender, RoutedEventArgs e)
        {
            runLookupCustomer();
        }

        private void button_Unlock_Click(object sender, RoutedEventArgs e)
        {
            if (lookupResults_Databases_ListView.SelectedIndex > -1)
            {
                Database database = lookupResults_Databases_ListView.SelectedItem as Database;

                unlockDatabase(database.name, database.server);
            }
        }

        private void button_GetUsers_Click(object sender, RoutedEventArgs e)
        {
            if (lookupResults_Databases_ListView.SelectedIndex > -1)
            {
                Database database = lookupResults_Databases_ListView.SelectedItem as Database;

                getDatabaseUsers(database);
            }
        }

        private void button_GetTimeout_Click(object sender, RoutedEventArgs e)
        {
            if (lookupResults_SiteName_TextBox.Text != "" && lookupResults_IISServer_TextBox.Text != "")
            {
                getTimeout(lookupResults_SiteName_TextBox.Text, lookupResults_IISServer_TextBox.Text);
            }
        }

        private void button_UpdateTimeout_Click(object sender, RoutedEventArgs e)
        {
            if (lookupResults_SiteName_TextBox.Text != "" && lookupResults_IISServer_TextBox.Text != "")
            {
                updateTimeout(lookupResults_SiteName_TextBox.Text, lookupResults_IISServer_TextBox.Text);
            }
        }

        private void button_WelcomeEmail_Click(object sender, RoutedEventArgs e)
        {
            if (lookupResults_PrimaryEmail_TextBox.Text != "" && lookupResults_IITID_TextBox.Text != "")
            {
                resendWelcomeEmail(lookupResults_IITID_TextBox.Text, lookupResults_PrimaryEmail_TextBox.Text);
            }
        }

        private async void runLookupCustomer()
        {
            if (textBox_LookupValue.Text != "" & jenkinsServerSelect_ComboBox.Text != "")
            {
                lookupAccount_LookupStatus_TextBox.Content = "Lookup running...";

                // Get the currently selected server
                JenkinsServer server = jenkinsServerSelect_ComboBox.SelectedItem as JenkinsServer;

                // Post a request to build LookupCustomer and wait for a response
                if (JenkinsTasks.UnsecureJenkinsCreds(server.id) != null)
                {
                    string lookupCustomerOutput = await JenkinsTasks.runJenkinsBuild(server, @"/job/CloudOps1-LookupCustomer/buildWithParameters?LookupCustomerBy="
                        + comboBox_LookupBy.SelectedValue.ToString()
                        + "&LookupValue="
                        + textBox_LookupValue.Text);

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
                    List<Database> databaseList = SearchForDatabases(lookupCustomerOutput);

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
                }
                else
                {
                    lookupAccount_LookupStatus_TextBox.Content = "Login error";
                }
            }
            else
            {
                lookupAccount_LookupStatus_TextBox.Content = "Enter search criteria";
            }
        }

        private async void unlockDatabase(string databaseName, string sqlServer)
        {
            databaseTasks_UnlockStatus_Label.Content = "Unlocking...";

            // Get the currently selected server
            JenkinsServer server = jenkinsServerSelect_ComboBox.SelectedItem as JenkinsServer;

            // Post a request to build LookupCustomer and wait for a response
            if (JenkinsTasks.UnsecureJenkinsCreds(server.id) != null)
            {
                string output = await JenkinsTasks.runJenkinsBuild(server, @"/job/CloudOps1-UnlockDatabase/buildWithParameters?&SQLServer="
                    + sqlServer
                    + "&DatabaseName="
                    + databaseName);

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
        }

        private async void getDatabaseUsers(Database database)
        {
            databaseTasks_GetUsersStatus_Label.Content = "Working...";

            // Clear the current database user list
            database.users.Clear();

            // Get the currently selected server
            JenkinsServer server = jenkinsServerSelect_ComboBox.SelectedItem as JenkinsServer;

            // Post a request to build LookupCustomer and wait for a response
            if (JenkinsTasks.UnsecureJenkinsCreds(server.id) != null)
            {
                string output = await JenkinsTasks.runJenkinsBuild(server, @"/job/CloudOps1-ListCustomerDatabaseUsers/buildWithParameters?&SQLServer="
                    + database.server
                    + "&DatabaseName="
                    + database.name);

                // Pulling strings out of output (lines end with return, null value doesn't do the trick)
                string outputDatabaseName = SearchString(output, "Changed database context to '", "'.");

                // Temp user creation test
                DatabaseUser user = new DatabaseUser();
                user.loginName = database.name;
                database.users.Add(user);

                if (outputDatabaseName == database.name)
                {
                    lookupResults_DatabaseUsers_ListView.ItemsSource = database.users;

                    databaseTasks_GetUsersStatus_Label.Content = "Done Stuff";
                }
                else
                {
                    databaseTasks_GetUsersStatus_Label.Content = "Query failed";
                }
            }
            else
            {
                databaseTasks_GetUsersStatus_Label.Content = "Login error";
            }
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
                        + accountEmail);

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
        }

        private async void getTimeout(string siteName, string iisServer)
        {
            accountTasks_GetTimeoutStatus_Label.Content = "Working...";

            // Get the currently selected server
            JenkinsServer server = jenkinsServerSelect_ComboBox.SelectedItem as JenkinsServer;

            // Post a request to build LookupCustomer and wait for a response
            if (JenkinsTasks.UnsecureJenkinsCreds(server.id) != null)
            {
                string output = await JenkinsTasks.runJenkinsBuild(server, @"/job/CloudOps1-ListExistingClientTimeout/buildWithParameters?&SiteName="
                    + siteName
                    + "&IISServer="
                    + iisServer);

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
                        + newValue);

                    // Pulling strings out of output (lines end with return, null value doesn't do the trick)
                    string outputSiteName = SearchString(output, "Updating customer ", " on server ");
                    string outputIISServer = SearchString(output, "on server ", @" 
");
                    string outputTimeout = SearchString(output, "Changing Timeout to: ", @"
 ");

                    MessageBox.Show(outputSiteName + " | " + outputIISServer + " | " + outputTimeout);

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
        }

        private void PopulateDropDowns()
        {
            // Load the Jenkins servers
            if (loadJenkinsServersXml())
            {
                List<JenkinsServer> serverList = new List<JenkinsServer>();
                XmlNodeList serverNodeList = jenkinsServerXml.SelectNodes("servers/server");
                foreach (XmlNode serverNode in serverNodeList)
                {
                    JenkinsServer mew = new JenkinsServer(); //JenkinsServer server = mew JenkinsServer();
                    mew.id = serverNode.Attributes["id"].Value;
                    mew.name = serverNode.Attributes["name"].Value;
                    mew.url = serverNode.InnerText;

                    serverList.Add(mew);
                }

                jenkinsServerSelect_ComboBox.ItemsSource = serverList;
            }

            // Populating LookupCustomer drop-downs with Key/Value pairs, then setting the selected index to 0
            comboBox_LookupBy.DisplayMemberPath = "Key";
            comboBox_LookupBy.SelectedValuePath = "Value";
            comboBox_LookupBy.Items.Add(new KeyValuePair<string, string>("Site Name", "SiteName"));
            comboBox_LookupBy.Items.Add(new KeyValuePair<string, string>("Email Address", "EmailAddress"));
            comboBox_LookupBy.Items.Add(new KeyValuePair<string, string>("Account Number", "ZuoraAccount"));
            comboBox_LookupBy.Items.Add(new KeyValuePair<string, string>("Subscription Number", "ZuoraSubscription"));
            comboBox_LookupBy.Items.Add(new KeyValuePair<string, string>("IIT ID", "IITID"));
            comboBox_LookupBy.SelectedIndex = 0;
        }

        public bool loadJenkinsServersXml()
        {
            // Loads the configuration XML from embedded resources. Later update will also store this locally and check a server for an updated version.
            try
            {
                string xmlString = null;

                // Open the XML file from embedded resources
                using (Stream stream = GetType().Assembly.GetManifestResourceStream("Act__Premium_Cloud_Support_Utility.JenkinsServers.xml"))
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        xmlString = sr.ReadToEnd();
                    }
                }

                // Add the text to the Jenkins Servers XmlDocument
                jenkinsServerXml.LoadXml(xmlString);
            }
            catch (Exception error)
            {
                MessageBox.Show("Unable to load Jenkins servers - failure in loadJenkinsServersXml().\n\n" + error.Message);

                return false;
            }
            return true;
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

        private List<Database> SearchForDatabases(string mainText)
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

        async Task Delay(int time)
        {
            await Task.Delay(time);
        }

        private async void jenkinsServerSelect_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                // Get server ID of selected server from jenkinsServerXml
                JenkinsServer server = e.AddedItems[0] as JenkinsServer;

                // Trigger update on UI with login status
                jenkinsServerSelect_LoginStatus_Label.Content = "Checking login...";
                jenkinsServerSelect_Grid.ClearValue(BackgroundProperty);

                // Run a GET request to retrieve the user's login data
                bool status = await JenkinsTasks.checkServerLogin(server);
                if (status)
                {
                    jenkinsServerSelect_LoginStatus_Label.Content = "Login succeeded.";
                }
                else
                {
                    jenkinsServerSelect_LoginStatus_Label.Content = "Login failed.";
                    jenkinsServerSelect_Grid.SetValue(BackgroundProperty, new SolidColorBrush(Color.FromRgb(254, 80, 0)));
                }
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

    public class JenkinsServer
    {
        public string id { get; set; }
        public string url { get; set; }
        public string name { get; set; }
    }
}
