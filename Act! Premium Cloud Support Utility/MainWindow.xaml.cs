using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;

namespace Act__Premium_Cloud_Support_Utility
{
    public partial class MainWindow : Window
    {
        public static string encodedCreds = null; // Encoded version of the user's username and password
        
        public MainWindow()
        {
            InitializeComponent();

            JenkinsLogin loginWindow = new JenkinsLogin();
            loginWindow.Show();
        }

        private async void RunLookupCustomer()
        {
            string URL = @"https://cloudops-jenkins-ust1.hostedtest.act.com:8443/job/CloudOps1-LookupCustomer/buildWithParameters";

            // Creating base URL
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(URL);

            // Adding accept header for XML format
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/xml"));

            // Adding authentication details
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCreds);

            // List data response
            var response = await client.PostAsync(@"https://cloudops-jenkins-ust1.hostedtest.act.com:8443/job/CloudOps1-LookupCustomer/buildWithParameters?LookupCustomerBy=SiteName&LookupValue=keithV19Test1", new StringContent(""));
        }
    }
}
