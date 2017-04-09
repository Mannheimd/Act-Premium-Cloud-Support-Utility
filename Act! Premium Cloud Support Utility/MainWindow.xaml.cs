using System;
using System.Collections.Generic;
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
        public MainWindow()
        {
            InitializeComponent();

            PopulateDropDowns();
        }

        private async void RunLookupCustomer()
        {
            if (textBox_LookupValue.Text != "")
            {
                // Post a request to build LookupCustomer and wait for a response
                string lookupCustomerOutput = await JenkinsTasks.runJenkinsBuild("UST1", @"/job/CloudOps1-LookupCustomer/buildWithParameters?LookupCustomerBy="
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

        private void button_RunLookupCustomer_Click(object sender, RoutedEventArgs e)
        {
            RunLookupCustomer();
        }

        private void PopulateDropDowns()
        {
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

    public class Database
    {
        public string name { get; set; }
        public string server { get; set; }
    }
}
