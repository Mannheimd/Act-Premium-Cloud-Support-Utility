﻿using Message_Handler;
using Microsoft.Win32;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using Act__Premium_Cloud_Support_Utility;
using System.Collections.Generic;
using System.ComponentModel;

namespace Jenkins_Tasks
{
    static class JenkinsInfo
    {
        public static List<JenkinsServer> jenkinsServerList { get; set; }
        public static List<APCLookupType> lookupTypeList { get; set; }
    }

    class JenkinsTasks
    {
        public static byte[] additionalEntropy = { 5, 8, 3, 4, 7 }; // Used to further encrypt Jenkins authentication information

        /// <summary>
        /// Secures the user's Jenkins credentials against the Windows user profile and stores them in the registry under HKCU
        /// </summary>
        /// <param name="username">Username entered in login window</param>
        /// <param name="apiToken">API token entered in login window</param>
        /// <param name="server">Short name for server, e.g. "USP1"</param>
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
            catch (CryptographicException error)
            {
                MessageHandler.handleMessage(false, 3, error, "Encrypting Jenkins login credentials");
            }
        }

        /// <summary>
        /// Pulls stored Jenkins credentials from registry and decrypts them
        /// </summary>
        /// <param name="server">Short name for Jenkins server (e.g. USP1)</param>
        /// <returns>Returns unsecured utf8-encrypted byte array representing username:password</returns>
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
                    catch (CryptographicException error)
                    {
                        MessageHandler.handleMessage(false, 3, error, "Decrypting stored Jenkins login credentials"); ;
                    }
                }
                catch(Exception error)
                {
                    MessageHandler.handleMessage(false, 3, error, "Locating reg key to get Jenkins credentials");
                }

                return utf8Creds;
            }
            return null;
        }

        /// <summary>
        /// Verifies that the registry key to store Jenkins credentials exists, and creates it if not
        /// </summary>
        /// <returns>true if key is now created and valid, false if not</returns>
        public static bool CheckOrCreateJenkinsRegPath()
        {
            MessageHandler.handleMessage(false, 6, null, "Verifying Jenkins Login registry key path");
            RegistryKey key = null;

            // Check if subkey "HKCU\Software\Swiftpage Support" exists
            key = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support", false);
            if (key == null)
            {
                MessageHandler.handleMessage(false, 5, null, @"Creating registry key 'HKCU\Software\Swiftpage Support'");
                
                try
                {
                    key = Registry.CurrentUser.OpenSubKey(@"Software", true);
                    key.CreateSubKey("Swiftpage Support");
                }
                catch (Exception error)
                {
                    MessageHandler.handleMessage(false, 3, error, @"Attempting to create registry key 'HKCU\Software\Swiftpage Support'");
                    return false;
                }
            }

            // Check if subkey HKCU\Software\Swiftpage Support\JenkinsLogins exists
            key = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support\Jenkins Logins", false);
            if (key == null)
            {
                MessageHandler.handleMessage(false, 5, null, @"Creating registry key 'HKCU\Software\Swiftpage Support'");

                try
                {
                    key = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support", true);
                    key.CreateSubKey("Jenkins Logins");
                }
                catch (Exception error)
                {
                    MessageHandler.handleMessage(false, 3, error, @"Attempting to create registry key 'HKCU\Software\Swiftpage Support\Jenkins Logins'");
                    return false;
                }
            }

            // Confirm that full subkey exists
            key = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support\Jenkins Logins", false);
            if (key != null)
            {
                MessageHandler.handleMessage(false, 6, null, "Jenkins Login reg key exists");
                return true;
            }
            else
            {
                MessageHandler.handleMessage(false, 3, null, @"Testing access to key HKCU\Swiftpage Support\Jenkins Logins");
                return false;
            }
        }

        public static async Task<HttpResponseMessage> jenkinsPostRequest(JenkinsServer server, string request)
        {
            // Create HttpClient with base URL
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(server.url); // Update to get the URL from selected server

            // Adding accept header for XML format
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            // Getting the encrypted authentication details
            byte[] creds = UnsecureJenkinsCreds(server.id); // Update to use server, when server passes correct string

            // If no authentication details, return blank message with Unauthorized status code
            if (creds == null)
            {
                HttpResponseMessage blankResponse = new HttpResponseMessage();
                blankResponse.StatusCode = HttpStatusCode.Unauthorized;

                return blankResponse;
            }
            else
            {
                // Add authentication details to HTTP request
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(creds));

                // Run a Get request with the provided request path
                HttpResponseMessage response = new HttpResponseMessage();
                try
                {
                    response = await client.PostAsync(request, new StringContent(""));
                }
                catch (Exception error)
                {
                    MessageBox.Show("POST request failed in 'jenkinsPostRequest(" + server.url + request + ")'.\n\n" + error);

                    HttpResponseMessage blankResponse = new HttpResponseMessage();
                    blankResponse.StatusCode = HttpStatusCode.Unauthorized;

                    return blankResponse;
                }

                return response;
            }
        }

        public static async Task<HttpResponseMessage> jenkinsGetRequest(JenkinsServer server, string request)
        {
            // Create HttpClient with base URL
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(server.url);

            // Adding accept header for XML format
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            // Getting the encrypted authentication details
            byte[] creds = UnsecureJenkinsCreds(server.id); // Update to use server, when server passes correct string

            // If no authentication details, return blank message with Unauthorized status code
            if (creds == null)
            {
                HttpResponseMessage blankResponse = new HttpResponseMessage();
                blankResponse.StatusCode = HttpStatusCode.Unauthorized;

                return blankResponse;
            }
            else
            {
                // Add authentication details to HTTP request
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(creds));

                // Run a Get request with the provided request path
                HttpResponseMessage response = new HttpResponseMessage();
                try
                {
                    response = await client.GetAsync(request);
                }
                catch (Exception error)
                {
                    MessageBox.Show("GET request failed in 'jenkinsGetRequest(" + server.url + request + ")'.\n\n" + error);

                    HttpResponseMessage blankResponse = new HttpResponseMessage();
                    blankResponse.StatusCode = HttpStatusCode.Unauthorized;

                    return blankResponse;
                }

                return response;
            }
        }

        public static async Task<bool> checkServerLogin(JenkinsServer server)
        {
            HttpResponseMessage response = await jenkinsGetRequest(server, "/me/api/xml");
            if (response != null & response.IsSuccessStatusCode)
                return true;
            else
                return false;
        }

        public static async Task<string> runJenkinsBuild(JenkinsServer server, string request)
        {
            // Post a request to build job and wait for a response
            HttpResponseMessage postBuildRequest = await jenkinsPostRequest(server, request);

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
                        break;
                    }

                    await Task.Delay(1000); // Checking status every 1 second

                    // Use the URL returned in the Location header from the above POST response to GET the build status
                    string queuedBuildURL = postBuildRequest.Headers.Location.AbsoluteUri;
                    HttpResponseMessage getQueuedBuild = await jenkinsGetRequest(server, queuedBuildURL + @"\api\xml");

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
                    await Task.Delay(1000);

                    HttpResponseMessage getFinalBuildOutput = await jenkinsGetRequest(server, finalBuildUrl + @"\api\xml");

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
                    HttpResponseMessage finalBuildOutput = await jenkinsGetRequest(server, finalBuildUrl + @"logText/progressiveText?start=0");

                    return await finalBuildOutput.Content.ReadAsStringAsync();
                }
                return null;
            }
            else
            {
                return null;
            }
        }

        public static async Task<string> runJenkinsGet(JenkinsServer server, string request)
        {
            // Post a GET request to build LookupCustomer and wait for a response
            HttpResponseMessage getRequest = await jenkinsGetRequest(server, request);

            return await getRequest.Content.ReadAsStringAsync();
        }

        public static List<JenkinsServer> getJenkinsServerList()
        {
            List<JenkinsServer> jenkinsServerList = new List<JenkinsServer>();

            Stream jenkinsServersXmlStream = Application.Current.GetType().Assembly.GetManifestResourceStream("Act__Premium_Cloud_Support_Utility.JenkinsServers.xml");
            XmlDocument jenkinsServerXml = loadXmlFromXmlStream(jenkinsServersXmlStream);

            XmlNodeList serverNodeList = jenkinsServerXml.SelectNodes("servers/server");
            foreach (XmlNode serverNode in serverNodeList)
            {
                JenkinsServer mew = new JenkinsServer(); //JenkinsServer server = mew JenkinsServer();
                mew.id = serverNode.Attributes["id"].Value;
                mew.name = serverNode.Attributes["name"].Value;
                mew.url = serverNode.InnerText;

                jenkinsServerList.Add(mew);
            }

            return jenkinsServerList;
        }

        public static List<APCLookupType> buildAPCLookupTypeList()
        {
            List <APCLookupType> lookupTypeList = new List<APCLookupType>();

            lookupTypeList.Add(new APCLookupType { internalName = "ZuoraAccount", friendlyName = "Account Number" });
            lookupTypeList.Add(new APCLookupType { internalName = "EmailAddress", friendlyName = "Email Address" });
            lookupTypeList.Add(new APCLookupType { internalName = "SiteName", friendlyName = "Site Name" });
            lookupTypeList.Add(new APCLookupType { internalName = "ZuoraSubscription", friendlyName = "Subscription Number" });
            lookupTypeList.Add(new APCLookupType { internalName = "IITID", friendlyName = "IITID" });

            return lookupTypeList;
        }

        public static XmlDocument loadXmlFromXmlStream(Stream xmlStream)
        {
            // Loads the Jenkins Server XML from embedded resources. Later update will also store this locally and check a server for an updated version.
            XmlDocument jenkinsServerXml = new XmlDocument();
            try
            {
                string xmlString = null;

                // Open the XML file from embedded resources
                using (xmlStream)
                {
                    using (StreamReader sr = new StreamReader(xmlStream))
                    {
                        xmlString = sr.ReadToEnd();
                    }
                }

                // Add the text to the Jenkins Servers XmlDocument
                jenkinsServerXml.LoadXml(xmlString);
            }
            catch (Exception error)
            {
                MessageBox.Show("Unable to load Jenkins servers - failure when loading XML document from XML Stream.\n\n" + error.Message);

                return null;
            }

            return jenkinsServerXml;
        }

        public static async Task RunAPCAccountLookup(APCAccount account)
        {
            account.LookupStatus = APCAccountLookupStatus.InProgress;

            // Post a request to build LookupCustomer and wait for a response
            string lookupCustomerOutput = null;
            try
            {
                lookupCustomerOutput = await runJenkinsBuild(account.JenkinsServer, @"/job/CloudOps1-LookupCustomerMachine/buildWithParameters?LookupCustomerBy="
                    + account.LookupType.internalName
                    + "&LookupValue="
                    + account.LookupValue.Trim()
                    + "&delay=0sec");
            }
            catch
            {
                account.LookupStatus = APCAccountLookupStatus.Failed;
            }

            string lookupData = SearchString(lookupCustomerOutput, "[STARTDATA]", "[ENDDATA]");

            // Check that the output is valid
            if (SearchString(lookupData, "[LookupValue=", "]") != account.LookupValue.Trim()
                || SearchString(lookupData, "[LookupCustomerBy=", "]") != account.LookupType.internalName)
            {
                account.LookupStatus = APCAccountLookupStatus.Failed;

                return;
            }

            // Check if customer couldn't be found
            if (SearchString(lookupData, "[LookupResult=", "]") == "NotFound")
            {
                account.LookupStatus = APCAccountLookupStatus.NotFound;

                return;
            }

            // Pulling strings out of output (lines end with return, null value doesn't do the trick. Stupid humans.)
            account.IITID = SearchString(lookupData, "[IITID=", "]").Trim();
            account.AccountName = SearchString(lookupData, "[AccountName=", "]").Trim();
            account.Email = SearchString(lookupData, "[Email=", "]").Trim();
            account.CreateDate = SearchString(lookupData, "[CreateDate=", "]").Trim();
            account.TrialOrPaid = SearchString(lookupData, "[TrialOrPaid=", "]").Trim();
            account.SerialNumber = SearchString(lookupData, "[SerialNumber=", "]").Trim();
            account.SeatCount = SearchString(lookupData, "[SeatCount=", "]").Trim();
            account.SuspendStatus = SearchString(lookupData, "[SuspendStatus=", "]").Trim();
            account.ArchiveStatus = SearchString(lookupData, "[ArchiveStatus=", "]").Trim();
            account.DeleteStatus = SearchString(lookupData, "[DeleteStatus=", "]").Trim();
            account.ZuoraAccount = SearchString(lookupData, "[ZuoraAccount=", "]").Trim();
            account.AccountType = SearchString(lookupData, "[Product=", "]").Trim();

            if (SearchString(lookupData, "[SiteInfoFound=", "]") == "true")
            {
                string siteInfo = SearchString(lookupData, "[SITEINFOSTART]", "[SITEINFOEND]");
                string site = SearchString(siteInfo, "[SiteInfo=", "]");
                account.SiteName = SearchString(lookupData, "{SiteName=", "}").Trim();
                account.IISServer = SearchString(lookupData, "{IISServer=", "}").Trim();
                account.LoginUrl = SearchString(lookupData, "{URL=", "}").Trim();
                account.UploadUrl = SearchString(lookupData, "{UploadURL=", "}").Trim();
            }

            // Get a list of databases from the output
            account.Databases = ParseForDatabases(lookupData);

            account.LookupTime = DateTime.Now;

            account.LookupStatus = APCAccountLookupStatus.Successful;
        }

        public async Task<bool> unlockDatabase(string databaseName, string sqlServer, JenkinsServer server)
        {
            // Post a request to build LookupCustomer and wait for a response
            if (UnsecureJenkinsCreds(server.id) != null)
            {
                string output = await runJenkinsBuild(server, @"/job/CloudOps1-UnlockDatabase/buildWithParameters?&SQLServer="
                    + sqlServer
                    + "&DatabaseName="
                    + databaseName
                    + "&delay=0sec");

                // Pulling strings out of output (lines end with return, null value doesn't do the trick)
                string outputSqlServer = SearchString(output, "Found SQL Server: ", @"
");
                string outputDatabaseName = SearchString(output, "Unlocking database: ", @"
");

                if (outputSqlServer == sqlServer && outputDatabaseName == databaseName)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> getDatabaseUsers(APCDatabase database, JenkinsServer server)
        {
            // Clear the current database user list
            database.users.Clear();

            // Post a request to build LookupCustomer and wait for a response
            if (UnsecureJenkinsCreds(server.id) != null)
            {
                string output = await runJenkinsBuild(server, @"/job/CloudOps1-ListCustomerDatabaseUsers-Machine/buildWithParameters?&SQLServer="
                    + database.server
                    + "&DatabaseName="
                    + database.name
                    + "&delay=0sec");

                // Pulling strings out of output (lines end with return, null value doesn't do the trick)
                string outputDatabaseName = SearchString(output, "Changed database context to '", "'.");

                if (outputDatabaseName.ToLower() == database.name.ToLower())
                {
                    database.users = ParseForDatabaseUsers(output);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> resendWelcomeEmail(string accountIITID, string accountEmail, JenkinsServer server)
        {
            bool sendType = false; // true is alt email, false is default email
            string sendTo = null; // alt email address
            bool send = false; // output from send or cancel

            ResendWelcomeEmail resendWelcomeEmail = new ResendWelcomeEmail(accountEmail);
            resendWelcomeEmail.resultBool += value => sendType = value;
            resendWelcomeEmail.resultString += value => sendTo = value;
            resendWelcomeEmail.resultSend += value => send = value;
            resendWelcomeEmail.ShowDialog();

            if (send)
            {
                // set accountEmail to null if not needed, else set it to specified address
                if (sendType)
                {
                    accountEmail = sendTo;
                }
                else
                {
                    accountEmail = null;
                }

                // Post a request to build LookupCustomer and wait for a response
                if (UnsecureJenkinsCreds(server.id) != null)
                {
                    string output = await runJenkinsBuild(server, @"/job/CloudOps1-ResendWelcomeEmail/buildWithParameters?&IITID="
                        + accountIITID
                        + "&AltEmailAddress="
                        + accountEmail
                        + "&delay=0sec");

                    // Pulling strings out of output (lines end with return, null value doesn't do the trick)
                    string outputIITID = SearchString(output, "IITID: ", @"
");
                    string outputEmail = SearchString(output, "Email Address: ", @"
");

                    if (outputIITID == accountIITID && outputEmail == accountEmail)
                    {
                        return true;
                    }
                    else if (outputIITID == accountIITID && !sendType)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> getTimeout(APCAccount account, JenkinsServer server)
        {
            // Post a request to build LookupCustomer and wait for a response
            if (UnsecureJenkinsCreds(server.id) != null)
            {
                string output = await runJenkinsBuild(server, @"/job/CloudOps1-ListExistingClientTimeout/buildWithParameters?&SiteName="
                    + account.SiteName
                    + "&IISServer="
                    + account.IISServer
                    + "&delay=0sec");

                // Pulling strings out of output (lines end with return, null value doesn't do the trick)
                string outputSiteName = SearchString(output, "Site ", " on server");
                string outputIISServer = SearchString(output, "on server ", @"
");
                string outputCurrentTimeout = SearchString(output, "Current Timeout: ", @"
");

                if (outputSiteName == account.SiteName && outputIISServer == account.IISServer)
                {
                    account.TimeoutValue = Convert.ToInt32(outputCurrentTimeout);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> updateTimeout(APCAccount account, JenkinsServer server)
        {
            string newValue = null; // new timeout value to set
            bool proceed = false; // output from send or cancel

            UpdateTimeoutValue updateTimeoutValue = new UpdateTimeoutValue();
            updateTimeoutValue.resultValue += value => newValue = value;
            updateTimeoutValue.resultProceed += value => proceed = value;
            updateTimeoutValue.ShowDialog();

            if (proceed)
            {
                // Post a request to build LookupCustomer and wait for a response
                if (UnsecureJenkinsCreds(server.id) != null)
                {
                    string output = await runJenkinsBuild(server, @"/job/CloudOps1-UpdateExistingClientTimeout/buildWithParameters?&SiteName="
                        + account.SiteName
                        + "&IISServer="
                        + account.IISServer
                        + "&Timeout="
                        + newValue
                        + "&delay=0sec");

                    // Pulling strings out of output (lines end with return, null value doesn't do the trick)
                    string outputSiteName = SearchString(output, "Updating customer ", " on server ");
                    string outputIISServer = SearchString(output, "on server ", @" 
");
                    string outputTimeout = SearchString(output, "Changing Timeout to: ", @"
 ");

                    if (outputSiteName == account.SiteName && outputIISServer == account.IISServer && outputTimeout == newValue)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> resetUserPassword(APCDatabase database, APCDatabaseUser user, JenkinsServer server)
        {
            // Post a request to build ResetPassword and wait for a response
            if (UnsecureJenkinsCreds(server.id) != null)
            {
                string output = await runJenkinsBuild(server, @"/job/CloudOps1-ResetCustomerLoginPassword/buildWithParameters?&SQLServer="
                    + database.server
                    + "&DatabaseName="
                    + database.name
                    + "&UserName="
                    + user.loginName
                    + "&delay=0sec");

                string outputDatabaseName = SearchString(output, "Changed database context to '", "'.");
                bool oneRowAffected = output.Contains("(1 rows affected)");

                if (outputDatabaseName == database.name && oneRowAffected == true)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static string SearchString(string mainText, string startString, string endString)
        {
            try
            {
                string split1 = mainText.Split(new string[] { startString }, StringSplitOptions.None)[1];
                return split1.Split(new string[] { endString }, StringSplitOptions.None)[0];
            }
            catch
            {
                return null;
            }
        }

        public static List<APCDatabase> ParseForDatabases(string mainText)
        {
            string workingText = mainText;
            List<APCDatabase> list = new List<APCDatabase>();
            try
            {
                // Get lines with databases on
                string[] lines = workingText.Split(new string[] { "Database: " }, StringSplitOptions.None);

                // For each line, separate the name and server
                // Loop starts at line 1 rather than 0
                for (int i = 1; i < lines.Length; i++)
                {
                    APCDatabase database = new APCDatabase();

                    database.name = (lines[i].Split(new string[] { " | Server: " }, StringSplitOptions.None)[0]).Split(null)[0];
                    database.server = (lines[i].Split(new string[] { " | Server: " }, StringSplitOptions.None)[1]).Split(null)[0];

                    list.Add(database);
                }
            }
            catch (Exception error)
            {
                MessageBox.Show("Error occurred whilst getting database list:\n\n" + error.Message);
            }

            return list;
        }

        public static List<APCDatabaseUser> ParseForDatabaseUsers(string mainText)
        {
            string workingText = mainText;
            List<APCDatabaseUser> list = new List<APCDatabaseUser>();
            try
            {
                // Cut data to after the line of ----¦----¦----¦---- nonsense
                workingText = workingText.Split(new string[] { @"----------¦----------¦----------¦-------------¦-------------¦----------------¦---------¦----------------¦-----------¦--------------¦---------------
" }, StringSplitOptions.None)[1];

                // Cut data to before "<linebreak><linebreak>(1 rows affected)"
                workingText = workingText.Split(new string[] { @"

(1 rows affected)" }, StringSplitOptions.None)[0];

                // Get all lines
                string[] lines = workingText.Split(new string[] { @"
" }, StringSplitOptions.None);

                // For each line, get the user information
                // Loop starts at line 2 rather than 0
                foreach (string line in lines)
                {
                    APCDatabaseUser user = new APCDatabaseUser();

                    // Split the line up into individual values
                    string[] lineSplit = line.Split('¦');

                    // Grab the needed information
                    user.loginName = lineSplit[0];
                    user.lastLogin = lineSplit[9];
                    user.role = lineSplit[6];

                    list.Add(user);
                }
            }
            catch (Exception error)
            {
                MessageBox.Show("Error occurred whilst getting database users:\n\n" + error.Message);
            }

            return list;
        }
    }

    public class JenkinsServer
    {
        public string id { get; set; }
        public string url { get; set; }
        public string name { get; set; }
    }

    public class APCAccount : INotifyPropertyChanged
    {
        private APCAccountLookupStatus _lookupStatus;
        private APCAccountSelectedTab _selectedTab;
        private string _iitid;
        private string _accountName;
        private string _email;
        private string _createDate;
        private string _trialOrPaid;
        private string _serialNumber;
        private string _seatCount;
        private string _suspendStatus;
        private string _archiveStatus;
        private string _siteName;
        private string _iisServer;
        private string _loginUrl;
        private string _uploadUrl;
        private string _zuoraAccount;
        private string _deleteStatus;
        private string _accountType;
        private string _lookupValue;
        private int _timeoutValue;
        private List<APCDatabase> _databases;
        private APCLookupType _lookupType;
        private DateTime _lookupTime;
        private DateTime _lookupCreateTime;
        private JenkinsServer _jenkinsServer;

        public APCAccountLookupStatus LookupStatus
        {
            get { return _lookupStatus; }
            set { SetPropertyField("LookupStatus", ref _lookupStatus, value); }
        }
        
        public APCAccountSelectedTab SelectedTab
        {
            get { return _selectedTab; }
            set { SetPropertyField("SelectedTab", ref _selectedTab, value); }
        }
        
        public string IITID
        {
            get { return _iitid; }
            set { SetPropertyField("IITID", ref _iitid, value); }
        }

        public string AccountName
        {
            get { return _accountName; }
            set { SetPropertyField("AccountName", ref _accountName, value); }
        }

        public string Email
        {
            get { return _email; }
            set { SetPropertyField("Email", ref _email, value); }
        }

        public string CreateDate
        {
            get { return _createDate; }
            set { SetPropertyField("CreateDate", ref _createDate, value); }
        }

        public string TrialOrPaid
        {
            get { return _trialOrPaid; }
            set { SetPropertyField("TrialOrPaid", ref _trialOrPaid, value); }
        }

        public string SerialNumber
        {
            get { return _serialNumber; }
            set { SetPropertyField("SerialNumber", ref _serialNumber, value); }
        }

        public string SeatCount
        {
            get { return _seatCount; }
            set { SetPropertyField("SeatCount", ref _seatCount, value); }
        }

        public string SuspendStatus
        {
            get { return _suspendStatus; }
            set { SetPropertyField("SuspendStatus", ref _suspendStatus, value); }
        }

        public string ArchiveStatus
        {
            get { return _archiveStatus; }
            set { SetPropertyField("ArchiveStatus", ref _archiveStatus, value); }
        }

        public string SiteName
        {
            get { return _siteName; }
            set { SetPropertyField("SiteName", ref _siteName, value); }
        }

        public string IISServer
        {
            get { return _iisServer; }
            set { SetPropertyField("IISServer", ref _iisServer, value); }
        }

        public string LoginUrl
        {
            get { return _loginUrl; }
            set { SetPropertyField("LoginUrl", ref _loginUrl, value); }
        }

        public string UploadUrl
        {
            get { return _uploadUrl; }
            set { SetPropertyField("UploadUrl", ref _uploadUrl, value); }
        }

        public string ZuoraAccount
        {
            get { return _zuoraAccount; }
            set { SetPropertyField("ZuoraAccount", ref _zuoraAccount, value); }
        }

        public string DeleteStatus
        {
            get { return _deleteStatus; }
            set { SetPropertyField("DeleteStatus", ref _deleteStatus, value); }
        }

        public string AccountType
        {
            get { return _accountType; }
            set { SetPropertyField("AccountType", ref _accountType, value); }
        }

        public string LookupValue
        {
            get { return _lookupValue; }
            set { SetPropertyField("LookupValue", ref _lookupValue, value); }
        }

        public int TimeoutValue
        {
            get { return _timeoutValue; }
            set { SetPropertyField("TimeoutValue", ref _timeoutValue, value); }
        }

        public List<APCDatabase> Databases
        {
            get { return _databases; }
            set { SetPropertyField("Databases", ref _databases, value); }
        }

        public APCLookupType LookupType
        {
            get { return _lookupType; }
            set { SetPropertyField("LookupType", ref _lookupType, value); }
        }

        public DateTime LookupTime
        {
            get { return _lookupTime; }
            set { SetPropertyField("LookupTime", ref _lookupTime, value); }
        }

        public DateTime LookupCreateTime
        {
            get { return _lookupCreateTime; }
            set { SetPropertyField("LookupCreateTime", ref _lookupCreateTime, value); }
        }

        public JenkinsServer JenkinsServer
        {
            get { return _jenkinsServer; }
            set { SetPropertyField("JenkinsServer", ref _jenkinsServer, value); }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected void SetPropertyField<T>(string propertyName, ref T field, T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(field, newValue))
            {
                field = newValue;
                OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;



        // Add function here to return XML node representation of APCAccount - is this better than having it separate?
    }

    public class APCDatabase
    {
        public string name { get; set; }
        public string server { get; set; }
        public List<APCDatabaseUser> users = new List<APCDatabaseUser>();
    }

    public class APCDatabaseUser
    {
        public string loginName { get; set; }
        public string role { get; set; }
        public string lastLogin { get; set; }
    }

    public class APCLookupType
    {
        public string internalName { get; set; }
        public string friendlyName { get; set; }
    }

    public enum APCAccountLookupStatus
    {
        NotStarted,
        InProgress,
        Successful,
        NotFound,
        Failed
    };

    public enum APCAccountSelectedTab
    {
        None,
        Databases,
        Details,
        Activity
    };
}
