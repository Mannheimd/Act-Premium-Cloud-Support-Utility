using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Act__Premium_Cloud_Support_Utility
{
    public partial class MainWindow : Window
    {
        private const string URL = @"https://cloudops-jenkins-ust1.hostedtest.act.com:8443/job/CloudOps1-LookupCustomer/buildWithParameters";
        private string urlParameters = "?LookupCustomerBy=SiteName&LookupValue=keithV19Test1";

        public MainWindow()
        {
            InitializeComponent();

            // Creating base URL
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(URL);

            // Adding accept header for XML format
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/xml"));

            // Adding authentication details
            byte[] creds = UTF8Encoding.UTF8.GetBytes("Redacted");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(creds));

            // List data response
            var response = client.PostAsync(@"https://cloudops-jenkins-ust1.hostedtest.act.com:8443/job/CloudOps1-LookupCustomer/buildWithParameters?LookupCustomerBy=SiteName&LookupValue=keithV19Test1", new StringContent(""));
        }
    }
}
