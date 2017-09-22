using Jenkins_Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Xml;

namespace Act__Premium_Cloud_Support_Utility
{
    public partial class MainWindow : Window
    {
        public static ObservableCollection<APCAccount> LookupResults = new ObservableCollection<APCAccount>();

        public MainWindow()
        {
            JenkinsInfo.jenkinsServerList = JenkinsTasks.getJenkinsServerList();
            JenkinsInfo.lookupTypeList = JenkinsTasks.buildAPCLookupTypeList();

            InitializeComponent();

            LookupListPane_Lookups_ListBox.ItemsSource = LookupResults;
            CollectionView LookupListDisplayOrder = (CollectionView)CollectionViewSource.GetDefaultView(LookupListPane_Lookups_ListBox.ItemsSource);
            LookupListDisplayOrder.SortDescriptions.Add(new SortDescription("lookupCreateTime", ListSortDirection.Descending));
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

        private void LookupListPane_NewLookup_Click(object sender, RoutedEventArgs e)
        {
            APCAccount account = new APCAccount();
            account.LookupCreateTime = DateTime.Now;
            LookupResults.Add(account);
            LookupListPane_Lookups_ListBox.SelectedIndex = 0;
        }

        private void LookupListPane_RemoveLookup_Click(object sender, RoutedEventArgs e)
        {
            APCAccount Account = (APCAccount)(sender as Button).DataContext;
            LookupResults.Remove(Account);
        }

        private async void NewLookupPane_LookupButton_Click(object sender, RoutedEventArgs e)
        {
            APCAccount account = (APCAccount)(sender as Button).DataContext;
            if (account.JenkinsServer != null
                && account.LookupType != null
                && account.LookupValue != null
                && account.LookupValue.Trim() != "")
            {
                await JenkinsTasks.RunAPCAccountLookup(account);
            }
        }

        private async void LookupResults_DatabaseList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            APCAccount Account = (APCAccount)(sender as ListBox).DataContext;
            APCDatabase Database = e.AddedItems[0] as APCDatabase;

            if (Database.UserLoadStatus == APCUserLoadStatus.Failed || Database.UserLoadStatus == APCUserLoadStatus.NotStarted)
                Database.Users = await JenkinsTasks.getDatabaseUsers(Database, Account.JenkinsServer);
        }

        private void ValidateTextInputNumbersOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
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

    public class AccountLookupStatus_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value.ToString() == APCAccountLookupStatus.Successful.ToString())
                return "Lookup Complete";

            if (value != null && value.ToString() == APCAccountLookupStatus.NotStarted.ToString())
                return "";

            if (value != null && value.ToString() == APCAccountLookupStatus.NotFound.ToString())
                return "Account not found";

            if (value != null && value.ToString() == APCAccountLookupStatus.InProgress.ToString())
                return "Locating account...";

            if (value != null && value.ToString() == APCAccountLookupStatus.Failed.ToString())
                return "Lookup failed";

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns friendly version of APC Account Type (Act! Premium Cloud or APC+)
    /// </summary>
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
    /// Converts 1 to Visible, 0 to either Hidden by default or Collapsed is parameter contains "Collapsible". Reverses this result if parameter contains "Reverse"
    /// </summary>
    public class BooleanToVisibility_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool BooleanOfAwesome = false;
            if (value != null && value.ToString() == "True")
                BooleanOfAwesome = true;

            if (parameter != null && parameter.ToString().Contains("Reverse"))
                BooleanOfAwesome = !BooleanOfAwesome;

            if (BooleanOfAwesome)
                return Visibility.Visible;
            else
            {
                if (parameter != null && parameter.ToString().Contains("Collapsible"))
                    return Visibility.Collapsed;

                return Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            throw new Exception("This method is not implemented.");
        }
    }

    /// <summary>
    /// Takes APCAccount. If lookupStatus isn't Successful, account name reflects lookup status
    /// </summary>
    public class LookupListAccountName_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is string)
            {
                return value;
            }
            return "New Lookup";
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            throw new Exception("This method is not implemented.");
        }
    }

    /// <summary>
    /// Converts APCAccountSelectedTab to a ListBox SelectedItem integer
    /// </summary>
    public class LookupResultSelectedTab_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return -1;

            if (value.ToString() == APCAccountSelectedTab.None.ToString())
                return -1;

            if (value.ToString() == APCAccountSelectedTab.Databases.ToString())
                return 0;

            if (value.ToString() == APCAccountSelectedTab.Details.ToString())
                return 1;

            if (value.ToString() == APCAccountSelectedTab.Activity.ToString())
                return 2;

            return -1;
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            if (value == null)
                return APCAccountSelectedTab.None;

            if (!(value is int))
                return APCAccountSelectedTab.None;

            if (value.ToString() == "-1")
                return APCAccountSelectedTab.None;

            if (value.ToString() == "0")
                return APCAccountSelectedTab.Databases;

            if (value.ToString() == "1")
                return APCAccountSelectedTab.Details;

            if (value.ToString() == "2")
                return APCAccountSelectedTab.Activity;

            return APCAccountSelectedTab.None;
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

    public class AccountTimeout_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is string)
            {
                if ((value as string) == "undetermined")
                    return "Unable to obtain. Account may be suspended.";

                return (value as string) + " minutes";
            }
            else return "Fetching...";
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            throw new Exception("This method is not implemented.");
        }
    }
}