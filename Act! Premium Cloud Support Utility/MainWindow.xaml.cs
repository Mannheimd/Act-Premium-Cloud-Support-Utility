using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
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

                // Get the build number of the last run job
                HttpResponseMessage lastBuild = await client.GetAsync(@"https://cloudops-jenkins-ust1.hostedtest.act.com:8443/job/CloudOps1-LookupCustomer/lastBuild/buildNumber");
                int buildNumber = (Convert.ToInt32(await lastBuild.Content.ReadAsStringAsync())) + 1; // Incrementing the build number by 1

                // Post a request to build LookupCustomer and wait for a response
                HttpResponseMessage response = await client.PostAsync(@"https://cloudops-jenkins-ust1.hostedtest.act.com:8443/job/CloudOps1-LookupCustomer/buildWithParameters?LookupCustomerBy=" + comboBox_LookupBy.SelectedValue.ToString() + "&LookupValue=" + textBox_LookupValue.Text, new StringContent(""));

                if (response.IsSuccessStatusCode)
                {
                    // Get the output from buildNumber +1, then check against the parameters we used to ensure it's the correct one (in case of overlap)
                    HttpResponseMessage newBuild = await client.GetAsync(@"https://cloudops-jenkins-ust1.hostedtest.act.com:8443/job/CloudOps1-LookupCustomer/" + buildNumber.ToString() + @"/api/xml");
                    XmlDocument buildOutputXml = new XmlDocument();
                    buildOutputXml.LoadXml(await newBuild.Content.ReadAsStringAsync());

                    // Getting the parameters into an XmlNodeList and comparing them with what we expect to see
                    XmlNodeList parameterNodes = buildOutputXml.SelectNodes(@"freeStyleBuild/action/parameter");
                    int criteriaMet = 0;
                    foreach (XmlNode parameter in parameterNodes)
                    {
                        XmlNode name = parameter.SelectSingleNode("name");
                        XmlNode value = parameter.SelectSingleNode("value");
                        // Increment criteriaMet for each property that matches with what was expected - should result in CriteriaMet being 2
                        if (name.InnerXml == "LookupCustomerBy" & value.InnerXml == comboBox_LookupBy.SelectedValue.ToString()) criteriaMet++;
                        if (name.InnerXml == "LookupValue" & value.InnerXml == textBox_LookupValue.Text) criteriaMet++;
                    }

                    if (criteriaMet == 2)
                    {
                        MessageBox.Show("Success!!!");
                    }
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
    }
}
