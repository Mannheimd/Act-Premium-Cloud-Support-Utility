using Microsoft.Win32;
using System;
using System.Security;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Act__Premium_Cloud_Support_Utility
{
    class JenkinsEncryption
    {
        public static byte[] additionalEntropy = { 5, 8, 3, 4, 7 }; // Used to further encrypt Jenkins authentication information

        public static void SecureJenkinsCreds(string username, SecureString apiToken, string server)
        {
            byte[] utf8Creds = UTF8Encoding.UTF8.GetBytes(username + ":" + apiToken);

            byte[] securedCreds = null;

            try
            {
                securedCreds = ProtectedData.Protect(utf8Creds, additionalEntropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException e)
            {
                MessageBox.Show("Unable to encrypt Jenkins login credentials:\n\n" + e.ToString());
            }

            // Check if registry path exists
            if (CheckOrCreateJenkinsRegPath())
            {
                // Save encrypted key to registry
                RegistryKey jenkinsCredsKey = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support\Jenkins Logins", true);
                jenkinsCredsKey.SetValue(server, securedCreds);
            }
        }

        public static byte[] UnsecureJenkinsCreds(string server)
        {
            // Check if registry path exists
            if (CheckOrCreateJenkinsRegPath())
            {
                // Get encrypted key from registry
                try
                {
                    return (byte[])Registry.GetValue(@"Software\Swiftpage Support\Jenkins Logins", server, null);
                }
                catch(Exception error)
                {
                    MessageBox.Show("Unable to get stored Jenkins credentials:\n\n" + error.Message);
                }
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
    }
}
