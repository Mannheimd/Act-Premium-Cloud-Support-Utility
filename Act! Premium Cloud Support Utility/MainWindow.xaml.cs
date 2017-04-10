﻿using System;
using System.Collections;
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
        public static XmlDocument jenkinsServerXml = new XmlDocument();

        public MainWindow()
        {
            InitializeComponent();

            PopulateDropDowns();
        }

        private async void runLookupCustomer()
        {
            if (textBox_LookupValue.Text != "" & jenkinsServerSelect_ComboBox.Text != "")
            {
                lookupAccount_LookupStatus_TextBox.Content = "Lookup running...";

                // Get the server ID of the selected server
                string server = getAttributesFromXml(jenkinsServerXml, "servers/server[@name='" + jenkinsServerSelect_ComboBox.Text + "']", "id")[0];

                // Post a request to build LookupCustomer and wait for a response
                if (JenkinsTasks.UnsecureJenkinsCreds(server) != null)
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
                    lookupResults_Databases_ListView.Items.Clear();
                    foreach (Database database in databaseList)
                    {
                        lookupResults_Databases_ListView.Items.Add("Database: " + database.name + " | Server: " + database.server);
                    }

                    lookupAccount_LookupStatus_TextBox.Content = "Lookup complete";
                }
                else
                {
                    lookupAccount_LookupStatus_TextBox.Content = "Required information missing";
                }
            }
            else
            {
                lookupAccount_LookupStatus_TextBox.Content = "Required information missing";
            }
        }

        private void button_RunLookupCustomer_Click(object sender, RoutedEventArgs e)
        {
            runLookupCustomer();
        }

        private void PopulateDropDowns()
        {
            // Load the Jenkins servers
            if (loadJenkinsServersXml())
            {
                jenkinsServerSelect_ComboBox.ItemsSource = getAttributesFromXml(jenkinsServerXml, "servers/server", "name");
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
                using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("Act__Premium_Cloud_Support_Utility.JenkinsServers.xml"))
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
            bool stillGoing = true;
            string workingText = mainText;
            List<Database> list = new List<Database>();
            while(stillGoing)
            {
                try
                {
                    Database database = new Database();

                    // Cut off everything before database name
                    workingText = workingText.Split(new string[] { "Database: " }, StringSplitOptions.None)[1];
                    MessageBox.Show(workingText);

                    // Database name is everything before the next null character
                    database.name = workingText.Split(null)[0];

                    // Cut off everything before server name
                    workingText = workingText.Split(new string[] { "| Server: " }, StringSplitOptions.None)[1];
                    MessageBox.Show(workingText);

                    // Server name  is everything before the next null character
                    database.server = workingText.Split(null)[0];
                    
                    list.Add(database);

                    if (!workingText.Contains("Database: "))
                    {
                        stillGoing = false;
                    }
                }
                catch
                {
                    // stillGoing = false;
                }
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
                string selectedItem = e.AddedItems[0].ToString();
                string server = getAttributesFromXml(jenkinsServerXml, "servers/server[@name='" + selectedItem + "']", "id")[0];

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
    }

    public class Database
    {
        public string name { get; set; }
        public string server { get; set; }
    }
}
