using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace Act__Premium_Cloud_Support_Utility
{
    public partial class MainWindow : Window
    {
        private const string encodedPassword = ""; // Encoded version of the user's password - gets set by login window, or stored in settings.xml
        private static string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\APC Support Utility\"; // Build the file path to a location within the user's AppData folder, will contain settings.xml

        public MainWindow()
        {
            InitializeComponent();

            // If settings.xml exists, load it - if not, create it.
            if (File.Exists(appDataFolderPath + "settings.xml"))
            {
                LoadSettingsXml();
            }
            else
            {
                CreateSettingsXml();
            }
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
            byte[] creds = UTF8Encoding.UTF8.GetBytes("Redacted");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(creds));

            // List data response
            var response = await client.PostAsync(@"https://cloudops-jenkins-ust1.hostedtest.act.com:8443/job/CloudOps1-LookupCustomer/buildWithParameters?LookupCustomerBy=SiteName&LookupValue=keithV19Test1", new StringContent(""));
        }

        private void CreateSettingsXml()
        {
            // Creating an XML document
            XmlDocument settingsXml = new XmlDocument();
            string dataToCreate =
                @"<settings>
	                <user>""""</user>
                </settings>";
            settingsXml.LoadXml(dataToCreate);

            try
            {
                // Creating the directory in AppData
                if (!Directory.Exists(appDataFolderPath))
                {
                    Directory.CreateDirectory(appDataFolderPath);
                }

                // Creating and writing to settings.xml
                File.Create(appDataFolderPath + "settings.xml");
                settingsXml.Save(appDataFolderPath + "settings.xml");
            }
            catch (Exception error)
            {
                // Oops, shit got broke
                MessageBox.Show("Failed to create settings.xml in location '" + appDataFolderPath + "'. \n \n" +
                    "Application will terminate. \n \n" +
                    "Error returned: \n" +
                    error.Message);

                Application.Current.Shutdown();
            }
        }

        private void LoadSettingsXml()
        {

        }
    }
}
