using Microsoft.Win32;
using System;
using System.Security;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Act__Premium_Cloud_Support_Utility
{
    class JenkinsTasks
    {
        public static byte[] additionalEntropy = { 5, 8, 3, 4, 7 }; // Used to further encrypt Jenkins authentication information

        public static void SecureJenkinsCreds(string username, string apiToken, string server)
        {
            byte[] utf8Creds = UTF8Encoding.UTF8.GetBytes(username + ":" + apiToken);

            byte[] securedCreds = null;

            // Encrypt credentials
            try
            {
                securedCreds = ProtectedData.Protect(utf8Creds, additionalEntropy, DataProtectionScope.CurrentUser);

                // Check if registry path exists
                if (CheckOrCreateJenkinsRegPath())
                {
                    // Save encrypted key to registry
                    RegistryKey jenkinsCredsKey = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support\Jenkins Logins", true);
                    jenkinsCredsKey.SetValue(server, securedCreds);
                }
            }
            catch (CryptographicException e)
            {
                MessageBox.Show("Unable to encrypt Jenkins login credentials:\n\n" + e.ToString());
            }
        }

        public static byte[] UnsecureJenkinsCreds(string server)
        {
            // Check if registry path exists
            if (CheckOrCreateJenkinsRegPath())
            {
                byte[] securedCreds = null;
                byte[] utf8Creds = null;

                // Get encrypted key from registry
                try
                {
                    RegistryKey jenkinsCredsKey = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support\Jenkins Logins", false);
                    securedCreds = (byte[])jenkinsCredsKey.GetValue(server);

                    // Un-encrypt credentials
                    try
                    {
                        utf8Creds = ProtectedData.Unprotect(securedCreds, additionalEntropy, DataProtectionScope.CurrentUser);
                    }
                    catch (CryptographicException e)
                    {
                        MessageBox.Show("Unable to unencrypt Jenkins login credentials:\n\n" + e.ToString());
                    }
                }
                catch(Exception error)
                {
                    MessageBox.Show("Unable to get stored Jenkins credentials:\n\n" + error.Message);
                }

                return utf8Creds;
            }
            return null;
        }

        public static bool CheckOrCreateJenkinsRegPath()
        {
            // Check if SubKey HKCU\Software\Swiftpage Support\JenkinsLogins exists
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support\Jenkins Logins", false);
            if (key == null)
            {
                // Doesn't exist, let's see if HKCU\Software\Swiftpage Support exists
                key = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support", false);
                if (key == null)
                {
                    // Doesn't exist, try to create 'Swiftpage Support' SubKey
                    key = Registry.CurrentUser.OpenSubKey(@"Software", true);
                    try
                    {
                        key.CreateSubKey("Swiftpage Support");
                    }
                    catch(Exception error)
                    {
                        MessageBox.Show(@"Unable to create SubKey HKCU\Software\Swiftpage Support:\n\n" + error.Message);
                        return false;
                    }
                }

                // 'Swiftpage Support' subkey exists (or has just been created), try creating 'Jenkins Logins'
                key = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support", true);
                try
                {
                    key.CreateSubKey("Jenkins Logins");
                }
                catch(Exception error)
                {
                    MessageBox.Show(@"Unable to create SubKey HKCU\Software\Swiftpage Support\Jenkins Logins:\n\n" + error.Message);
                    return false;
                }
            }
            return true;
        }

        public static async Task<HttpResponseMessage> jenkinsPostRequest(string server, string request)
        {
            // Create HttpClient with base URL
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(@"https://cloudops-jenkins-ust1.hostedtest.act.com:8443"); // Update to get the URL from selected server

            // Adding accept header for XML format
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            // Adding authentication details
            byte[] creds = UnsecureJenkinsCreds(server); // Update to use server, when server passes correct string
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(creds));

            // Run a Get request with the provided request path
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await client.PostAsync(request, new StringContent(""));
            }
            catch (Exception error)
            {
                MessageBox.Show("POST request failed in 'jenkinsPostRequest(" + server + "," + request + ")'.\n\n" + error);
            }

            return response;
        }

        public static async Task<HttpResponseMessage> jenkinsGetRequest(string server, string request)
        {
            // Create HttpClient with base URL
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(@"https://cloudops-jenkins-ust1.hostedtest.act.com:8443"); // Update to get the URL from selected server

            // Adding accept header for XML format
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            // Adding authentication details
            byte[] creds = UnsecureJenkinsCreds(server); // Update to use server, when server passes correct string
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(creds));

            // Run a Get request with the provided request path
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await client.GetAsync(request);
            }
            catch(Exception error)
            {
                MessageBox.Show("GET request failed in 'jenkinsGetRequest(" + server + "," + request + ")'.\n\n" + error);
            }

            return response;
        }

        public static async Task<string> runJenkinsBuild(string server, string request)
        {
            // Post a request to build LookupCustomer and wait for a response
            HttpResponseMessage postBuildRequest = await JenkinsTasks.jenkinsPostRequest(server, request);

            string finalBuildUrl = "";

            if (postBuildRequest.IsSuccessStatusCode)
            {
                bool queuedBuildComplete = false;
                int failCount = 0;

                // Keep checking for the completed job
                while (!queuedBuildComplete)
                {
                    if (failCount > 30)
                    {
                        MessageBox.Show("Looks like a problem occurred. Please try running your request again. If the problem persists, please report it through your usual channels.");
                        break;
                    }

                    await Delay(1000); // Checking status every 1 second

                    // Use the URL returned in the Location header from the above POST response to GET the build status
                    string queuedBuildURL = postBuildRequest.Headers.Location.AbsoluteUri;
                    HttpResponseMessage getQueuedBuild = await JenkinsTasks.jenkinsGetRequest("UST1", queuedBuildURL + @"\api\xml");

                    // Throw the build status output into an XML document
                    XmlDocument queuedBuildXml = new XmlDocument();
                    queuedBuildXml.LoadXml(await getQueuedBuild.Content.ReadAsStringAsync());

                    // Try getting the completed build URL from the output
                    try
                    {
                        XmlNode buildURLNode = queuedBuildXml.SelectSingleNode("leftItem/executable/url");
                        finalBuildUrl = buildURLNode.InnerXml;
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

                    HttpResponseMessage getFinalBuildOutput = await JenkinsTasks.jenkinsGetRequest("UST1", finalBuildUrl + @"\api\xml");

                    // Throw the output into an XML document
                    XmlDocument finalBuildXml = new XmlDocument();
                    finalBuildXml.LoadXml(await getFinalBuildOutput.Content.ReadAsStringAsync());

                    // Try getting the completed build URL from the output
                    try
                    {
                        XmlNode building = finalBuildXml.SelectSingleNode("freeStyleBuild/building");
                        XmlNode result = finalBuildXml.SelectSingleNode("freeStyleBuild/result");
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
                    HttpResponseMessage finalBuildOutput = await JenkinsTasks.jenkinsGetRequest("UST1", finalBuildUrl + @"logText/progressiveText?start=0");

                    return await finalBuildOutput.Content.ReadAsStringAsync();
                }
                return null;
            }
            else
            {
                MessageBox.Show("Build creation failed with reason: " + postBuildRequest.ReasonPhrase);

                return null;
            }
        }

        public static async Task Delay(int time)
        {
            await Task.Delay(time);
        }
    }
}
