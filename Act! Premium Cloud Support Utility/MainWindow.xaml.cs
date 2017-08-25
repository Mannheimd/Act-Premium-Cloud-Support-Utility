using Jenkins_Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Xml;

namespace Act__Premium_Cloud_Support_Utility
{
    public partial class MainWindow : Window
    {
        public static Stream jenkinsServersXmlStream = null;

        public MainWindow()
        {
            InitializeComponent();

            jenkinsServersXmlStream = GetType().Assembly.GetManifestResourceStream("Act__Premium_Cloud_Support_Utility.JenkinsServers.xml");

            JenkinsTasks.loadJenkinsServers();

            APCAccount account = new APCAccount()
            {
                accountName = "Test account",
                lookupTime = DateTime.Now,
                lookupStatus = APCAccountLookupStatus.Successful,
                iitid = "biglongnumber",
                email = "test@test.com",
                createDate = "24th Feb 2016",
                trialOrPaid = "Paid",
                serialNumber = "wwwww-xxxxx-yyyyy-zzzzz",
                seatCount = "4",
                suspendStatus = "Not Suspended",
                archiveStatus = "Not Archived",
                siteName = "APCTestSite",
                iisServer = "UST1-ACTTEST-SRV",
                loginUrl = "https://master.hosted1.act.com/swiftpagetestsupport",
                uploadUrl = "http://hosted1.act.com/uploads?client=swiftpagetestsupport",
                zuoraAccount = "A00546894",
                deleteStatus = "Deleted",
                accountType = "ActPremiumCloudPlus"
            };

            List<APCAccount> accounts = new List<APCAccount>();
            accounts.Add(account);
            accounts.Add(account);
            accounts.Add(account);
            accounts.Add(account);
            accounts.Add(account);
            accounts.Add(account);
            accounts.Add(account);

            LookupListPane_Lookups_ListBox.ItemsSource = accounts;
        }

        public static List<string> getValuesFromXml(XmlDocument xmlDoc, string path)
        {
            XmlNodeList xmlNodes = xmlDoc.SelectNodes(path);

            List<string> resultList = new List<string>();

            foreach (XmlNode node in xmlNodes)
            {
                resultList.Add(node.InnerXml);
            }

            return resultList;
        }

        public static List<string> getAttributesFromXml(XmlDocument xmlDoc, string path, string attribute)
        {
            XmlNodeList xmlNodes = xmlDoc.SelectNodes(path);

            List<string> resultList = new List<string>();

            foreach (XmlNode node in xmlNodes)
            {
                resultList.Add(node.Attributes[attribute].Value);
            }

            return resultList;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }

    public class ListBoxSelectedState_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null && parameter.ToString() == "Reverse")
            {
                if (value == null)
                    return Visibility.Visible;
                else
                    return Visibility.Hidden;
            }
            if (value == null)
                return Visibility.Hidden;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            throw new Exception("This method is not implemented.");
        }
    }

    public class ListBoxItemSelectedState_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.ToString() == "False")
                return Visibility.Hidden;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            throw new Exception("This method is not implemented.");
        }
    }

    /// <summary>
    /// Takes a JenkinsTasks.AccountLookupStatus value, returns true if state is "Successful"
    /// </summary>
    public class AccountLookupSuccess_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool IsSuccessful = false;
            if (value != null && value.ToString() == APCAccountLookupStatus.Successful.ToString())
                IsSuccessful = true;
            
            if (parameter != null && parameter.ToString() == "Reverse")
                IsSuccessful = !IsSuccessful;

            if (IsSuccessful)
                return Visibility.Visible;
            else
                return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            throw new Exception("This method is not implemented.");
        }
    }

    public class AccountType_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                if (value as string == "ActPremiumCloudPlus")
                    return "APC+";
                if (value as string == "ActPremiumCloud")
                    return "Act! Premium Cloud";
                return value;
            }
            else return "";
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            throw new Exception("This method is not implemented.");
        }
    }

    /// <summary>
    /// Takes APCAccount. If lookupStatus isn't Successful, account name reflects lookup status.
    /// </summary>
    public class LookupListAccountName_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                APCAccount SelectedAccount = (value as APCAccount);
                if (SelectedAccount.lookupStatus == APCAccountLookupStatus.Successful)
                    return SelectedAccount.accountName;

                if (SelectedAccount.lookupStatus == APCAccountLookupStatus.NotFound)
                    return "Account Not found";

                if (SelectedAccount.lookupStatus == APCAccountLookupStatus.Failed)
                    return "Lookup failed";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            throw new Exception("This method is not implemented.");
        }
    }

    public class AccountTrialOrPaid_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                if (value as string == "TRIAL")
                    return "Trial";
                if (value as string == "PAID")
                    return "Paid";
                return value;
            }
            else return "";
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            throw new Exception("This method is not implemented.");
        }
    }

    public class AccountStatus_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string & parameter is string)
            {
                string archiveStatus = (parameter as string).Split('|')[0];
                string deleteStatus = (parameter as string).Split('|')[1];

                // Assume account is active, then run through options from bad to worse. Don't need to display multiple; if account is Archived, knowing it's also Suspended is redundant.
                string currentStatus = "Active";

                if (value as string == "Suspended")
                    currentStatus = "Suspended";
                if (archiveStatus == "Archived")
                    currentStatus = "Archived";
                if (deleteStatus == "Deleted")
                    currentStatus = "Deleted";

                return currentStatus;
            }
            else return "";
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            throw new Exception("This method is not implemented.");
        }
    }
}
