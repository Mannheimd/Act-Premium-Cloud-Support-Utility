using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace Act__Premium_Cloud_Support_Utility
{
    public partial class MainWindow : Window
    {
        XmlDocument jenkinsServerXml = new XmlDocument();

        public MainWindow()
        {
            InitializeComponent();

            PopulateDropDowns();
        }

        private async void runLookupCustomer()
        {
            if (textBox_LookupValue.Text != "" & jenkinsServerSelect_ComboBox.Text != "")
            {
                // Get the server ID of the selected server
                string server = getValuesFromXml(jenkinsServerXml, "servers/server[@name='" + jenkinsServerSelect_ComboBox.Text + "']")[0];

                // Post a request to build LookupCustomer and wait for a response
                if (JenkinsTasks.UnsecureJenkinsCreds(server) != null)
                {
                    string lookupCustomerOutput = await JenkinsTasks.runJenkinsBuild(server, @"/job/CloudOps1-LookupCustomer/buildWithParameters?LookupCustomerBy="
                        + comboBox_LookupBy.SelectedValue.ToString()
                        + "&LookupValue="
                        + textBox_LookupValue.Text);

                    string iitid = SearchString(lookupCustomerOutput, "IITID: ");
                    string accountName = SearchString(lookupCustomerOutput, "Account Name: ");
                    string email = SearchString(lookupCustomerOutput, "Email: ");
                    string createDate = SearchString(lookupCustomerOutput, "Create Date: ");
                    string trialOrPaid = SearchString(lookupCustomerOutput, "Trial or Paid: ");
                    string serialNumber = SearchString(lookupCustomerOutput, "Serial Number: ");
                    string seatCount = SearchString(lookupCustomerOutput, "Seat Count: ");
                    string suspendStatus = SearchString(lookupCustomerOutput, "Suspend status: ");
                    string archiveStatus = SearchString(lookupCustomerOutput, "Archive status: ");
                    string siteName = SearchString(lookupCustomerOutput, "Site Name: ");
                    string iisServer = SearchString(lookupCustomerOutput, "IIS Server: ");
                    string loginUrl = SearchString(lookupCustomerOutput, "URL: ");
                    string uploadUrl = SearchString(lookupCustomerOutput, "Upload: ");

                    List<Database> databaseList = SearchForDatabases(lookupCustomerOutput);

                    MessageBox.Show(siteName);
                }
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

        private bool loadJenkinsServersXml()
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

        private List<string> getValuesFromXml(XmlDocument xmlDoc, string path)
        {
            XmlNodeList xmlNodes = xmlDoc.SelectNodes(path);

            List<string> resultList = new List<string>();

            foreach (XmlNode node in xmlNodes)
            {
                resultList.Add(node.InnerXml);
            }

            return resultList;
        }

        private List<string> getAttributesFromXml(XmlDocument xmlDoc, string path, string attribute)
        {
            XmlNodeList xmlNodes = xmlDoc.SelectNodes(path);

            List<string> resultList = new List<string>();

            foreach (XmlNode node in xmlNodes)
            {
                resultList.Add(node.Attributes[attribute].Value);
            }

            return resultList;
        }

        private string SearchString(string mainText, string searchFor)
        {
            try
            {
                string split1 = mainText.Split(new string[] { searchFor }, StringSplitOptions.None)[1];
                return split1.Split(null)[0];
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

                    string split1 = workingText.Split(new string[] { "Database: " }, StringSplitOptions.None)[1];
                    database.name = split1.Split(null)[0];
                    string split2 = workingText.Split(new string[] { "| Server: " }, StringSplitOptions.None)[1];
                    database.server = split2.Split(null)[0];

                    list.Add(database);

                    workingText = split2.Split(null)[1];
                }
                catch
                {
                    stillGoing = false;
                }
            }

            return list;
        }

        async Task Delay(int time)
        {
            await Task.Delay(time);
        }
    }

    public class JenkinsServer
    {
        public string name { get; set; }
        public string id { get; set; }
        public string url { get; set; }
    }

    public class Database
    {
        public string name { get; set; }
        public string server { get; set; }
    }
}
