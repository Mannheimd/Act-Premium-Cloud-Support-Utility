using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
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
                string URL = @"https://cloudops-jenkins-ust1.hostedtest.act.com:8443/job/CloudOps1-LookupCustomer";
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(URL);

                // Adding accept header for XML format
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                // Adding authentication details
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCreds);

                // Post a request to build LookupCustomer and wait for a response
                HttpResponseMessage response = await client.PostAsync(@"https://cloudops-jenkins-ust1.hostedtest.act.com:8443/job/CloudOps1-LookupCustomer/buildWithParameters?LookupCustomerBy=" + comboBox_LookupBy.SelectedValue.ToString() + "&LookupValue=" + textBox_LookupValue.Text, new StringContent(""));

                string newBuildUrl = "";

                if (response.IsSuccessStatusCode)
                {
                    bool queuedBuildComplete = false;
                    int failCount = 0;

                    // Keep checking for the completed job
                    while (!queuedBuildComplete)
                    {
                        if (failCount > 30)
                        {
                            MessageBox.Show("Looks like a problem occurred. Please try running your lookup again. If the problem persists, please report it through your usual channels.");
                            break;
                        }

                        await Delay(1000); // Checking status every 1 second

                        // Use the URL returned in the Location header from the above POST response to GET the build status
                        string queuedBuildURL = response.Headers.Location.AbsoluteUri;
                        HttpResponseMessage queuedBuildOutput = await client.GetAsync(queuedBuildURL + @"\api\xml");

                        // Throw the build status output into an XML document
                        XmlDocument queuedBuildXml = new XmlDocument();
                        queuedBuildXml.LoadXml(await queuedBuildOutput.Content.ReadAsStringAsync());

                        // Try getting the completed build URL from the output
                        try
                        {
                            XmlNode buildURLNode = queuedBuildXml.SelectSingleNode("leftItem/executable/url");
                            newBuildUrl = buildURLNode.InnerXml;
                            queuedBuildComplete = true;
                        }
                        catch
                        {
                            // Expecting this to fail for a couple of attempts while the build is queued, no need to do special stuff with the catch. It throws an Object Reference error until the correct output is seen, but this should never surface for the user.
                            failCount++;
                        }
                    }

                    // Now that we have the new build URL, let's start checking the output and looking for completion.
                    bool newBuildComplete = false;
                    while (!newBuildComplete)
                    {
                        await Delay(1000);

                        HttpResponseMessage newBuildOutput = await client.GetAsync(newBuildUrl + @"\api\xml");

                        // Throw the output into an XML document
                        XmlDocument newBuildXml = new XmlDocument();
                        newBuildXml.LoadXml(await newBuildOutput.Content.ReadAsStringAsync());

                        // Try getting the completed build URL from the output
                        try
                        {
                            XmlNode building = newBuildXml.SelectSingleNode("freeStyleBuild/building");
                            XmlNode result = newBuildXml.SelectSingleNode("freeStyleBuild/result");
                            if (building.InnerXml == "false" & result.InnerXml == "SUCCESS")
                            {
                                newBuildComplete = true;
                            }
                            else if (building.InnerXml == "false" & result.InnerXml != "SUCCESS")
                            {
                                MessageBox.Show("Looks like a problem occurred. Please try running your lookup again. If the problem persists, please report it through your usual channels.");
                                break;
                            }
                        }
                        catch
                        {
                            // Expecting this to fail for a couple of attempts while the build is queued, no need to do special stuff with the catch. It throws an Object Reference error until the correct output is seen, but this should never surface for the user.
                        }
                    }

                    if (newBuildComplete)
                    {
                        HttpResponseMessage finalBuildOutput = await client.GetAsync(newBuildUrl + @"logText/progressiveText?start=0");

                        MessageBox.Show(await finalBuildOutput.Content.ReadAsStringAsync());
                    }
                }
                else
                {
                    MessageBox.Show("Build creation failed with reason: " + response.ReasonPhrase);
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
