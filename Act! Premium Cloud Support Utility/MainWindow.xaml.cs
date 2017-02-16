using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace Act__Premium_Cloud_Support_Utility
{
    public partial class MainWindow : Window
    {
        public static string encodedCreds = null; // Encoded version of the user's username and password
        
        public MainWindow()
        {
            InitializeComponent();

            PopulateDropDowns();

            JenkinsLogin loginWindow = new JenkinsLogin();
            loginWindow.Show();
        }

        private async void RunLookupCustomer()
        {
            if (textBox_LookupValue.Text != "")
            {
                // Creating base URL
                string URL = @"https://cloudops-jenkins-ust1.hostedtest.act.com:8443/job/CloudOps1-LookupCustomer/buildWithParameters";
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(URL);

                // Adding accept header for XML format
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                // Adding authentication details
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCreds);

                // Post a request to build LookupCustomer and wait for a response
                HttpResponseMessage response = await client.PostAsync(@"https://cloudops-jenkins-ust1.hostedtest.act.com:8443/job/CloudOps1-LookupCustomer/buildWithParameters?LookupCustomerBy=" + comboBox_LookupBy.SelectedValue.ToString() + "&LookupValue=" + textBox_LookupValue.Text, new StringContent(""));

                if (response.IsSuccessStatusCode)
                {
                    bool buildComplete = false;

                    while (!buildComplete)
                    {
                        await Delay(1000);

                        HttpClient newBuild = new HttpClient();
                        newBuild.BaseAddress = response.Headers.Location;
                        string newBuildURL = response.Headers.Location.AbsoluteUri;
                        HttpResponseMessage newBuildOutput = await client.GetAsync(newBuildURL + @"\api\xml");
                        MessageBox.Show(await newBuildOutput.Content.ReadAsStringAsync());
                    }
                }
                else
                {
                    MessageBox.Show(response.ReasonPhrase);
                }
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

        async Task Delay(int time)
        {
            await Task.Delay(time);
        }
    }
}
