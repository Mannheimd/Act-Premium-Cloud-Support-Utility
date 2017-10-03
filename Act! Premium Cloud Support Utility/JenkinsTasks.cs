using Message_Handler;
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
    public class JenkinsInfo : DependencyObject
    {
        public static readonly JenkinsInfo Instance = new JenkinsInfo();
        public JenkinsInfo() { }

        public List<JenkinsServer> AvailableJenkinsServers
        {
            get
            {
                return (List<JenkinsServer>)GetValue(AvailableJenkinsServersProperty);
            }
            set
            {
                SetValue(AvailableJenkinsServersProperty, value);
            }
        }

        public List<JenkinsServer> ConfiguredJenkinsServers
        {
            get
            {
                return (List<JenkinsServer>)GetValue(ConfiguredJenkinsServersProperty);
            }
            set
            {
                SetValue(ConfiguredJenkinsServersProperty, value);
            }
        }

        public List<APCLookupType> LookupTypeList
        {
            get
            {
                return (List<APCLookupType>)GetValue(lookupTypeListProperty);
            }
            set
            {
                SetValue(lookupTypeListProperty, value);
            }
        }

        public static readonly DependencyProperty AvailableJenkinsServersProperty = DependencyProperty.Register("AvailableJenkinsServers", typeof(List<JenkinsServer>), typeof(JenkinsInfo), new UIPropertyMetadata());
        public static readonly DependencyProperty ConfiguredJenkinsServersProperty = DependencyProperty.Register("ConfiguredJenkinsServers", typeof(List<JenkinsServer>), typeof(JenkinsInfo), new UIPropertyMetadata());
        public static readonly DependencyProperty lookupTypeListProperty = DependencyProperty.Register("lookupTypeList", typeof(List<APCLookupType>), typeof(JenkinsInfo), new UIPropertyMetadata());
    }

    class JenkinsTasks
    {
        public static byte[] additionalEntropy = { 5, 8, 3, 4, 7 }; // Used to further encrypt Jenkins authentication information, changing this will cause any currently stored Jenkins login details on the client machine to be invalid

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

        /// <summary>
        /// Main lookup function, will run CloudOps1-LookupCustomerMachine and add results to the account
        /// </summary>
        /// <param name="account">The APC account to be looked up - could be a newly created account or one we're refreshing</param>
        /// <returns>Nothing</returns>
        public static async Task RunAPCAccountLookup(APCAccount account)
        {
            if (account.LookupStatus == APCAccountLookupStatus.Successful)
                account.LookupStatus = APCAccountLookupStatus.Refreshing;
            else
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
            account.Databases = ParseForDatabases(lookupData, account);

            // Get account activity from the output
            account.AccountActivity = ParseForActivities(lookupData);

            account.LookupTime = DateTime.Now;

            // Lookup is now a success, even though we're gonna do some more work
            account.LookupStatus = APCAccountLookupStatus.Successful;

            // Get the inactivity timeout
            account.TimeoutValue = await getTimeout(account);
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

        /// <summary>
        /// Builds CloudOps1-ListCustomerDatabaseUsers-Machine to get list of database users
        /// </summary>
        /// <param name="database">APCDatabase to get users for</param>
        /// <param name="server">JenkinsServer to run build on</param>
        /// <returns>Returns a list of APCDatabaseUsers</returns>
        public static async Task<List<APCDatabaseUser>> getDatabaseUsers(APCDatabase database, JenkinsServer server)
        {
            database.UserLoadStatus = JenkinsBuildStatus.InProgress;

            // Check the Jenkins login credentials
            if (UnsecureJenkinsCreds(server.id) == null)
            {
                database.UserLoadStatus = JenkinsBuildStatus.Failed;
                return null;
            }

            // Post a request to build LookupCustomer and wait for a response
            string BuildOutput = await runJenkinsBuild(server, @"/job/CloudOps1-ListCustomerDatabaseUsers-Machine/buildWithParameters?&SQLServer="
                + database.Server
                + "&DatabaseName="
                + database.Name
                + "&delay=0sec");

            // Get the actual data from the output
            string Data = SearchString(BuildOutput, "[STARTDATA]", "[ENDDATA]");

            // Check if successful
            string BuildStatus = SearchString(BuildOutput, "[UserInfoFound=", "]");
            if (BuildStatus != "true")
            {
                database.UserLoadStatus = JenkinsBuildStatus.Failed;
                return null;
            }
                
            // Create a new list
            List<APCDatabaseUser> UserList = new List<APCDatabaseUser>();

            // Get UserInfo block
            string UserInfo = SearchString(Data, "[USERINFOSTART]", "[USERINFOEND]");

            // Get User lines
            string[] Users = UserInfo.Split(new string[] { "[User=" }, StringSplitOptions.None);

            // For each line, build a user object
            foreach (string User in Users)
            {
                if (!User.Contains("{")) // This prevents it throwing an empty user into the list, caused by the 0 value being nothing helpful
                    continue;

                APCDatabaseUser NewUser = new APCDatabaseUser();

                NewUser.Database = database;

                NewUser.ContactName = SearchString(User, "{fullname=", "}");
                NewUser.LoginName = SearchString(User, "{userlogin=", "}");
                NewUser.Role = SearchString(User, "{displayname=", "}");

                string LastLoginRaw = SearchString(User, "{logondate=", "}");
                if (LastLoginRaw != null && LastLoginRaw != "" && LastLoginRaw != "NULL")
                {
                    int LastLoginYear = Convert.ToInt32(LastLoginRaw.Substring(0, 4));
                    int LastLoginMonth = Convert.ToInt32(LastLoginRaw.Substring(5, 2));
                    int LastLoginDay = Convert.ToInt32(LastLoginRaw.Substring(8, 2));
                    int LastLoginHour = Convert.ToInt32(LastLoginRaw.Substring(11, 2));
                    int LastLoginMinute = Convert.ToInt32(LastLoginRaw.Substring(14, 2));
                    int LastLoginSecond = Convert.ToInt32(LastLoginRaw.Substring(17, 2));
                    NewUser.LastLogin = new DateTime(LastLoginYear, LastLoginMonth, LastLoginDay, LastLoginHour, LastLoginMinute, LastLoginSecond);
                }

                UserList.Add(NewUser);
            }

            database.UserLoadStatus = JenkinsBuildStatus.Successful;
            return UserList;
        }

        /// <summary>
        /// Builds [job not yet made] to get list of database backups
        /// </summary>
        /// <param name="database">APCDatabase to get users for</param>
        /// <param name="server">JenkinsServer to run build on</param>
        /// <returns>Returns a list of APCDatabaseUsers</returns>
        public static async Task<List<APCDatabaseBackup>> getDatabaseBackups(APCDatabase database, JenkinsServer server)
        {
            database.BackupLoadStatus = JenkinsBuildStatus.InProgress;

            // Check the Jenkins login credentials
            if (UnsecureJenkinsCreds(server.id) == null)
            {
                database.BackupLoadStatus = JenkinsBuildStatus.Failed;
                return null;
            }

            // Post a request to build LookupCustomer and wait for a response
            string BuildOutput = await runJenkinsBuild(server, @"/job/[GetTheJobLink]/buildWithParameters?&SQLServer="
                + database.Server
                + "&DatabaseName="
                + database.Name
                + "&delay=0sec");

            // Get the actual data from the output
            string Data = SearchString(BuildOutput, "[STARTDATA]", "[ENDDATA]");

            // Check if successful
            string BuildStatus = SearchString(BuildOutput, "[BackupInfoFound=", "]");
            if (BuildStatus != "true")
            {
                database.BackupLoadStatus = JenkinsBuildStatus.Failed;
                return null;
            }

            // Create a new list
            List<APCDatabaseBackup> BackupList = new List<APCDatabaseBackup>();

            // Get BackupInfo block
            string BackupInfo = SearchString(Data, "[BACKUPINFOSTART]", "[BACKUPINFOEND]");

            // Get Backup lines
            string[] Backups = BackupInfo.Split(new string[] { "[Backup=" }, StringSplitOptions.None);

            // For each line, build a user object
            foreach (string Backup in Backups)
            {
                if (!Backup.Contains("{")) // This prevents it throwing an empty user into the list, caused by the 0 value being nothing helpful
                    continue;

                APCDatabaseBackup NewBackup = new APCDatabaseBackup(database);

                // File name format:
                // <environment>-<sql server>-<database name>-<datetime>-<full/diff>.bak
                // Note: Some SQL servers contain hyphens

                string TimeString = SearchString(Backup, database.Name + "-", "-");
                NewBackup.Date = new DateTime(
                    Convert.ToInt32(TimeString.Substring(0, 4)), // Year
                    Convert.ToInt32(TimeString.Substring(4, 2)), // Month
                    Convert.ToInt32(TimeString.Substring(6, 2)), // Day
                    Convert.ToInt32(TimeString.Substring(8, 2)), // Hour
                    Convert.ToInt32(TimeString.Substring(10, 2)), // Minute
                    00 // Second
                    );

                NewBackup.Filename = SearchString(Backup, "{filename=", "}");
                NewBackup.Type = SearchString(Backup, TimeString + "-", ".bak");

                BackupList.Add(NewBackup);
            }

            database.UserLoadStatus = JenkinsBuildStatus.Successful;
            return BackupList;
        }

        public static List<APCDatabaseBackupRestorable> GetRestorableBackupsFromFiles(List<APCDatabaseBackup> BackupFiles)
        {
            List<APCDatabaseBackupRestorable> RestorableBackups = new List<APCDatabaseBackupRestorable>();

            // Create list of available Full backups
            Dictionary<DateTime, APCDatabaseBackup> FullBackups = new Dictionary<DateTime, APCDatabaseBackup>();
            foreach (APCDatabaseBackup Backup in BackupFiles)
            {
                if (Backup.Type == "full")
                {
                    FullBackups.Add(Backup.Date.Date, Backup);
                }
            }

            // Begin assessing files for restorable things
            foreach (APCDatabaseBackup Backup in BackupFiles)
            {
                // If type is Full, add to RestorableBackup
                if (Backup.Type == "full")
                {
                    APCDatabaseBackupRestorable RestorableBackup = new APCDatabaseBackupRestorable(Backup.Backup_Database);
                    RestorableBackup.BackupFiles.Add(Backup);
                    RestorableBackup.Date = Backup.Date;
                }

                // If Diff, check for Full on the same day - if there is one, add to RestorableBackup with both files
                if (Backup.Type == "diff" && FullBackups.ContainsKey(Backup.Date.Date))
                {
                    APCDatabaseBackupRestorable RestorableBackup = new APCDatabaseBackupRestorable(Backup.Backup_Database);
                    RestorableBackup.BackupFiles.Add(FullBackups[Backup.Date.Date]);
                    RestorableBackup.BackupFiles.Add(Backup);
                    RestorableBackup.Date = Backup.Date;
                }
            }

            RestorableBackups.Sort(delegate(APCDatabaseBackupRestorable x, APCDatabaseBackupRestorable y)
            {
                if (x.Date == null && y.Date == null) return 0;
                else if (x.Date == null) return -1;
                else if (y.Date == null) return 1;
                else return x.Date.CompareTo(y.Date);
            });

            return RestorableBackups;
        }

        /// <summary>
        /// Builds [get job name] to copy backup files
        /// </summary>
        /// <param name="database">APCDatabase to get users for</param>
        /// <param name="server">JenkinsServer to run build on</param>
        /// <returns>Returns a list of APCDatabaseUsers</returns>
        public static async Task<bool> RetainDatabaseBackup(APCDatabaseBackupRestorable backup)
        {
            backup.RestoreBackupStatus = JenkinsBuildStatus.InProgress;

            // Check the Jenkins login credentials
            if (UnsecureJenkinsCreds(backup.Backup_APCDatabase.Database_APCAccount.JenkinsServer.id) == null)
            {
                backup.RestoreBackupStatus = JenkinsBuildStatus.Failed;
                return false;
            }

            foreach (APCDatabaseBackup BackupFile in backup.BackupFiles)
            {
                // Post a request to build LookupCustomer and wait for a response
                string BuildOutput = await runJenkinsBuild(backup.Backup_APCDatabase.Database_APCAccount.JenkinsServer, @"/job/[GetJobName]/buildWithParameters?&DestinationServer="
                    + backup.Backup_APCDatabase.Server
                    + "&Backup="
                    + BackupFile.Filename
                    + "&delay=0sec");

                // Get the actual data from the output
                string Data = SearchString(BuildOutput, "[STARTDATA]", "[ENDDATA]");

                // Check if successful
                string BuildStatus = SearchString(Data, "[CopySuccessful=", "]");
                if (BuildStatus != "true")
                {
                    backup.RestoreBackupStatus = JenkinsBuildStatus.Failed;
                    return false;
                }
            }

            backup.RestoreBackupStatus = JenkinsBuildStatus.Successful;
            return true;
        }

        /// <summary>
        /// Builds CloudOps1-ResendWelcomeEmail to resend a customer's welcome email
        /// </summary>
        /// <param name="Account">APC Account to resend welcome email for</param>
        /// <param name="SendType">Specifies if to send to primary account email, or the user specified</param>
        /// <param name="SpecifiedEmail">Can be null - User specified email address to send to</param>
        /// <returns></returns>
        public static async Task resendWelcomeEmail(APCAccount Account, WelcomeEmailSendTo SendType, string SpecifiedEmail)
        {
            Account.ResendWelcomeEmailStatus = JenkinsBuildStatus.InProgress;

            string SendEmailTo;

            if (SendType == WelcomeEmailSendTo.PrimaryAccountEmail)
                SendEmailTo = Account.Email;
            else
                SendEmailTo = SpecifiedEmail.Trim();

            // Post a request to build LookupCustomer and wait for a response
            if (UnsecureJenkinsCreds(Account.JenkinsServer.id) != null)
            {
                string output = await runJenkinsBuild(Account.JenkinsServer, @"/job/CloudOps1-ResendWelcomeEmail/buildWithParameters?&IITID="
                    + Account.IITID
                    + "&AltEmailAddress="
                    + SendEmailTo
                    + "&delay=0sec");

                // Pulling strings out of output (lines end with return, null value doesn't do the trick)
                string outputIITID = SearchString(output, "IITID: ", @"
");
                string outputEmail = SearchString(output, "Email Address: ", @"
");

                if (outputIITID == Account.IITID && outputEmail == SendEmailTo)
                {
                    Account.ResendWelcomeEmailStatus = JenkinsBuildStatus.Successful;
                }
                else if (outputIITID == Account.IITID && SendType == WelcomeEmailSendTo.PrimaryAccountEmail)
                {
                    Account.ResendWelcomeEmailStatus = JenkinsBuildStatus.Successful;
                }
                else
                {
                    Account.ResendWelcomeEmailStatus = JenkinsBuildStatus.Failed;
                }
            }
            else
            {
                Account.ResendWelcomeEmailStatus = JenkinsBuildStatus.Failed;
            }
        }

        /// <summary>
        /// Runs CloudOps1-ListExistingClientTimeout and returns the current timeout value as a string. Would be better as an int, but Jenkins returns a string and that's how I coded the UI.
        /// The converter for timeout value only works because the value can be null, and because the value could be 0 or negative (because someone messed up) I don't want to take that as "is null".
        /// </summary>
        /// <param name="account">APCAccount you want the inactivity timeout for</param>
        /// <returns>String representing account inactivity timeout, null, or "undetermined"</returns>
        public static async Task<string> getTimeout(APCAccount account)
        {
            // Post a request to build LookupCustomer and wait for a response
            if (UnsecureJenkinsCreds(account.JenkinsServer.id) != null)
            {
                string output = await runJenkinsBuild(account.JenkinsServer, @"/job/CloudOps1-ListExistingClientTimeout/buildWithParameters?&SiteName="
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
                    if (outputCurrentTimeout.Trim() == "")
                        return "undetermined";

                    return outputCurrentTimeout.Trim();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Builds CloudOps1-UpdateExistingClientTimeout to change the account's inactivity timeout value, then re-checks the timeout value
        /// </summary>
        /// <param name="Account">APCAccount to run the timeout change on</param>
        /// <param name="NewTimeoutValue">New timeout value, integers only please - I know it's a string, don't be a smartarse</param>
        /// <returns></returns>
        public static async Task updateTimeout(APCAccount Account, string NewTimeoutValue)
        {
            Account.ChangeInactivityTimeoutStatus = JenkinsBuildStatus.InProgress;

            // Post a request to build LookupCustomer and wait for a response
            if (UnsecureJenkinsCreds(Account.JenkinsServer.id) != null)
            {
                string output = await runJenkinsBuild(Account.JenkinsServer, @"/job/CloudOps1-UpdateExistingClientTimeout/buildWithParameters?&SiteName="
                    + Account.SiteName
                    + "&IISServer="
                    + Account.IISServer
                    + "&Timeout="
                    + NewTimeoutValue
                    + "&delay=0sec");

                // Pulling strings out of output (lines end with return, null value doesn't do the trick)
                string outputSiteName = SearchString(output, "Updating customer ", " on server ");
                string outputIISServer = SearchString(output, "on server ", @" 
");
                string outputTimeout = SearchString(output, "Changing Timeout to: ", @"
 ");

                if (outputSiteName == Account.SiteName && outputIISServer == Account.IISServer && outputTimeout == NewTimeoutValue)
                {
                    Account.ChangeInactivityTimeoutStatus = JenkinsBuildStatus.Successful;
                }
                else
                {
                    Account.ChangeInactivityTimeoutStatus = JenkinsBuildStatus.Failed;
                }
            }
            else
            {
                Account.ChangeInactivityTimeoutStatus = JenkinsBuildStatus.Failed;
            }

            if (Account.ChangeInactivityTimeoutStatus == JenkinsBuildStatus.Successful)
            {
                // Re-check the timeout value
                Account.TimeoutValue = null;

                Account.TimeoutValue = await getTimeout(Account);
            }
        }

        public static async Task<bool> resetUserPassword(APCDatabaseUser User)
        {
            User.ResetPasswordStatus = JenkinsBuildStatus.InProgress;

            // Post a request to build ResetPassword and wait for a response
            if (UnsecureJenkinsCreds(User.Database.Database_APCAccount.JenkinsServer.id) != null)
            {
                string output = await runJenkinsBuild(User.Database.Database_APCAccount.JenkinsServer, @"/job/CloudOps1-ResetCustomerLoginPassword/buildWithParameters?&SQLServer="
                    + User.Database.Server
                    + "&DatabaseName="
                    + User.Database.Name
                    + "&UserName="
                    + User.LoginName
                    + "&delay=0sec");

                string outputDatabaseName = SearchString(output, "Changed database context to '", "'.");
                bool oneRowAffected = output.Contains("(1 rows affected)");

                if (outputDatabaseName == User.Database.Name && oneRowAffected == true)
                {
                    User.ResetPasswordStatus = JenkinsBuildStatus.Successful;
                    return true;
                }
                else
                {
                    User.ResetPasswordStatus = JenkinsBuildStatus.Failed;
                    return false;
                }
            }
            else
            {
                User.ResetPasswordStatus = JenkinsBuildStatus.Failed;
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

        public static List<APCDatabase> ParseForDatabases(string lookupData, APCAccount Account)
        {
            // Get the Database information block
            string DatabaseInfo = SearchString(lookupData, "[DATABASEINFOSTART]", "[DATABASEINFOEND]");

            List<APCDatabase> DatabaseList = new List<APCDatabase>();
            try
            {
                // Get database lines
                string[] Databases = DatabaseInfo.Split(new string[] { "[Database=" }, StringSplitOptions.None);

                // For each line, build a database object
                foreach (string Database in Databases)
                {
                    if (!Database.Contains("{")) // This prevents it throwing an empty database into the list, caused by the 0 value being nothing helpful
                        continue;

                    APCDatabase NewDatabase = new APCDatabase(Account);

                    NewDatabase.Name = SearchString(Database, "{Name=", "}");
                    NewDatabase.Server = SearchString(Database, "{Server=", "}");

                    DatabaseList.Add(NewDatabase);
                }
            }
            catch
            {
                // await Tumbleweed()
            }

            return DatabaseList;
        }

        public static List<APCAccountActivity> ParseForActivities(string lookupData)
        {
            // Get the Database information block
            string ActivityData = SearchString(lookupData, "[PROVISIONINGINFOSTART]", "[PROVISIONINGINFOEND]");

            List<APCAccountActivity> ActivityList = new List<APCAccountActivity>();
            try
            {
                // Get database lines
                string[] Activities = ActivityData.Split(new string[] { "[Activity=" }, StringSplitOptions.None);

                // For each line, build a database object
                foreach (string Activity in Activities)
                {
                    if (!Activity.Contains("{")) // This prevents it throwing an empty database into the list, caused by the 0 value being nothing helpful
                        continue;

                    APCAccountActivity NewActivity = new APCAccountActivity();

                    NewActivity.Date = SearchString(Activity, "{Date=", "}");
                    NewActivity.Type = SearchString(Activity, "{Type=", "}");
                    NewActivity.Status = SearchString(Activity, "{Status=", "}");
                    NewActivity.Detail = SearchString(Activity, "{Detail=", "}");

                    ActivityList.Add(NewActivity);
                }
            }
            catch
            {
                // await Tumbleweed()
            }

            return ActivityList;
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
        private APCDatabasesSubItemSelectedTab _databasesSubItemSelected = APCDatabasesSubItemSelectedTab.Users;
        private JenkinsBuildStatus _resendWelcomeEmailStatus;
        private JenkinsBuildStatus _changeInactivityTimeoutStatus;
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
        private string _timeoutValue;
        private List<APCDatabase> _databases;
        private List<APCAccountActivity> _accountActivity;
        private APCLookupType _lookupType;
        private DateTime _lookupTime;
        private DateTime _lookupCreateTime;
        private TimeZone _selectedTimeZoneModifier;
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

        public APCDatabasesSubItemSelectedTab DatabasesSubItemSelected
        {
            get { return _databasesSubItemSelected; }
            set { SetPropertyField("SelectedTab", ref _databasesSubItemSelected, value); }
        }

        public JenkinsBuildStatus ResendWelcomeEmailStatus
        {
            get { return _resendWelcomeEmailStatus; }
            set { SetPropertyField("ResendWelcomeEmailStatus", ref _resendWelcomeEmailStatus, value); }
        }

        public JenkinsBuildStatus ChangeInactivityTimeoutStatus
        {
            get { return _changeInactivityTimeoutStatus; }
            set { SetPropertyField("ChangeInactivityTimeoutStatus", ref _changeInactivityTimeoutStatus, value); }
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

        public string TimeoutValue
        {
            get { return _timeoutValue; }
            set { SetPropertyField("TimeoutValue", ref _timeoutValue, value); }
        }

        public List<APCDatabase> Databases
        {
            get { return _databases; }
            set { SetPropertyField("Databases", ref _databases, value); }
        }

        public List<APCAccountActivity> AccountActivity
        {
            get { return _accountActivity; }
            set { SetPropertyField("AccountActivity", ref _accountActivity, value); }
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

        public TimeZone SelectedTimeZoneModifier
        {
            get { return _selectedTimeZoneModifier; }
            set { SetPropertyField("SelectedTimeZoneModifier", ref _selectedTimeZoneModifier, value); }
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

    public class APCDatabase : INotifyPropertyChanged
    {
        private string _name;
        private string _server;
        private List<APCDatabaseUser> _users;
        private List<APCDatabaseBackup> _backups;
        private List<APCDatabaseBackupRestorable> _restorableBackups;
        private JenkinsBuildStatus _userLoadStatus;
        private JenkinsBuildStatus _backupLoadStatus;
        public APCAccount Database_APCAccount { get; set; }

        public APCDatabase (APCAccount Account)
        {
            Database_APCAccount = Account;
        }

        public string Name
        {
            get { return _name; }
            set { SetPropertyField("Name", ref _name, value); }
        }

        public string Server
        {
            get { return _server; }
            set { SetPropertyField("Server", ref _server, value); }
        }

        public List<APCDatabaseUser> Users
        {
            get { return _users; }
            set { SetPropertyField("Users", ref _users, value); }
        }

        public List<APCDatabaseBackup> Backups
        {
            get { return _backups; }
            set { SetPropertyField("Backups", ref _backups, value); }
        }

        public List<APCDatabaseBackupRestorable> RestoreableBackups
        {
            get { return _restorableBackups; }
            set { SetPropertyField("RestoreableBackups", ref _restorableBackups, value); }
        }

        public JenkinsBuildStatus UserLoadStatus
        {
            get { return _userLoadStatus; }
            set { SetPropertyField("UserLoadStatus", ref _userLoadStatus, value); }
        }

        public JenkinsBuildStatus BackupLoadStatus
        {
            get { return _backupLoadStatus; }
            set { SetPropertyField("BackupLoadStatus", ref _backupLoadStatus, value); }
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
    }

    public class APCDatabaseUser : INotifyPropertyChanged
    {
        private string _contactName;
        private string _loginName;
        private string _role;
        private DateTime _lastLogin;
        private APCDatabase _database;
        private JenkinsBuildStatus _resetPasswordStatus;
        
        public string ContactName
        {
            get { return _contactName; }
            set { SetPropertyField("ContactName", ref _contactName, value); }
        }

        public string LoginName
        {
            get { return _loginName; }
            set { SetPropertyField("LoginName", ref _loginName, value); }
        }

        public string Role
        {
            get { return _role; }
            set { SetPropertyField("Role", ref _role, value); }
        }

        public DateTime LastLogin
        {
            get { return _lastLogin; }
            set { SetPropertyField("LastLogin", ref _lastLogin, value); }
        }

        public APCDatabase Database
        {
            get { return _database; }
            set { SetPropertyField("Database", ref _database, value); }
        }

        public JenkinsBuildStatus ResetPasswordStatus
        {
            get { return _resetPasswordStatus; }
            set { SetPropertyField("ResetPasswordStatus", ref _resetPasswordStatus, value); }
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
    }

    public class APCDatabaseBackup : INotifyPropertyChanged
    {
        private string _type;
        private string _filename;
        private DateTime _date;
        public APCDatabase Backup_Database { get; set; }

        public APCDatabaseBackup(APCDatabase Database)
        {
            Backup_Database = Database;
        }

        public string Type
        {
            get { return _type; }
            set { SetPropertyField("Type", ref _type, value); }
        }

        public string Filename
        {
            get { return _filename; }
            set { SetPropertyField("Filename", ref _filename, value); }
        }

        public DateTime Date
        {
            get { return _date; }
            set { SetPropertyField("Date", ref _date, value); }
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
    }

    public class APCDatabaseBackupRestorable : INotifyPropertyChanged
    {
        private DateTime _date;
        private List<APCDatabaseBackup> _backupFiles = new List<APCDatabaseBackup>();
        private JenkinsBuildStatus _restoreBackupStatus;
        public APCDatabase Backup_APCDatabase { get; set; }

        public APCDatabaseBackupRestorable(APCDatabase Database)
        {
            Backup_APCDatabase = Database;
        }

        public DateTime Date
        {
            get { return _date; }
            set { SetPropertyField("Date", ref _date, value); }
        }

        public List<APCDatabaseBackup> BackupFiles
        {
            get { return _backupFiles; }
            set { SetPropertyField("BackupFiles", ref _backupFiles, value); }
        }

        public JenkinsBuildStatus RestoreBackupStatus
        {
            get { return _restoreBackupStatus; }
            set { SetPropertyField("RestoreBackupStatus", ref _restoreBackupStatus, value); }
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
    }

    public class APCAccountActivity : INotifyPropertyChanged
    {
        private string _date;
        private string _type;
        private string _status;
        private string _detail;

        public string Date
        {
            get { return _date; }
            set { SetPropertyField("LookupTime", ref _date, value); }
        }

        public string Type
        {
            get { return _type; }
            set { SetPropertyField("LookupTime", ref _type, value); }
        }

        public string Status
        {
            get { return _status; }
            set { SetPropertyField("LookupTime", ref _status, value); }
        }

        public string Detail
        {
            get { return _detail; }
            set { SetPropertyField("LookupTime", ref _detail, value); }
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
        Refreshing,
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

    public enum JenkinsBuildStatus
    {
        NotStarted,
        InProgress,
        Failed,
        Successful
    };

    public enum WelcomeEmailSendTo
    {
        PrimaryAccountEmail,
        SpecifiedEmail
    };

    public enum APCDatabasesSubItemSelectedTab
    {
        Users,
        Backups
    };
}