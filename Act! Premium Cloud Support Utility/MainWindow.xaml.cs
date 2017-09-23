using MahApps.Metro.IconPacks;
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
using System.Runtime.CompilerServices;

namespace Act__Premium_Cloud_Support_Utility
{
    public class CurrentWindowState : DependencyObject
    {
        public static readonly CurrentWindowState Instance = new CurrentWindowState();
        private CurrentWindowState() { }

        public WindowDisplayMode DisplayMode
        {
            get
            {
                return (WindowDisplayMode)GetValue(DisplayModeProperty);
            }
            set
            {
                SetValue(DisplayModeProperty, value);
            }
        }

        public static readonly DependencyProperty DisplayModeProperty = DependencyProperty.Register("DisplayMode", typeof(WindowDisplayMode), typeof(CurrentWindowState), new UIPropertyMetadata());
    }

    public static class ApplicationVariables
    {
        public static string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Glacier";
        public static string UserConfigFilePath = AppDataPath + @"\userconfig.xml";
    }

    public partial class MainWindow : Window
    {
        public static ObservableCollection<APCAccount> LookupResults = new ObservableCollection<APCAccount>();

        public MainWindow()
        {
            AddServerToUserConfig(new JenkinsServer()
            {
                id = "DBG1",
                name = "Debug 1",
                url = "http://localhost/"
            });

            JenkinsInfo.AvailableJenkinsServers = JenkinsTasks.getJenkinsServerList();
            JenkinsInfo.ConfiguredJenkinsServers = LoadConfiguredServersFromUserConfig();
            JenkinsInfo.lookupTypeList = JenkinsTasks.buildAPCLookupTypeList();

            if (JenkinsInfo.ConfiguredJenkinsServers.Count < 1)
                CurrentWindowState.Instance.DisplayMode = WindowDisplayMode.Config;
            else
                CurrentWindowState.Instance.DisplayMode = WindowDisplayMode.Lookup;

            LoadConfiguredServersFromUserConfig();

            InitializeComponent();

            LookupListPane_Lookups_ListBox.ItemsSource = LookupResults;
            CollectionView LookupListDisplayOrder = (CollectionView)CollectionViewSource.GetDefaultView(LookupListPane_Lookups_ListBox.ItemsSource);
            LookupListDisplayOrder.SortDescriptions.Add(new SortDescription("lookupCreateTime", ListSortDirection.Descending));

            APCAccount DebugAccount = new APCAccount()
            {
                LookupStatus = APCAccountLookupStatus.Successful,
                ResendWelcomeEmailStatus = JenkinsBuildStatus.Failed,
                ChangeInactivityTimeoutStatus = JenkinsBuildStatus.Successful,
                IITID = "12345",
                AccountName = "DebugTest",
                Email = "debugtest@invalid.com",
                CreateDate = "2000BC",
                TrialOrPaid = "Trial",
                SerialNumber = "12345-ABCDE-67890-FGHIJ",
                SeatCount = "3",
                SuspendStatus = "NotSuspended",
                ArchiveStatus = "NotArchived",
                SiteName = "DebugSite",
                IISServer = "DBG1-DBGIIS-01",
                LoginUrl = "http://localhost/",
                UploadUrl = "http://localhost",
                ZuoraAccount = "A00123456",
                DeleteStatus = "NotDeleted",
                AccountType = "ActPremiumCloudPlus",
                TimeoutValue = "60",
                LookupTime = DateTime.Now,
                LookupCreateTime = DateTime.Now,
                JenkinsServer = new JenkinsServer()
                {
                    id = "DBG1",
                    name = "Debug 1",
                    url = "http://localhost/"
                }
            };

            LookupResults.Add(DebugAccount);
        }

        private static bool CreateDefaultUserConfigFile()
        {
            try
            {
                if (!Directory.Exists(ApplicationVariables.AppDataPath))
                    Directory.CreateDirectory(ApplicationVariables.AppDataPath);

                File.Create(ApplicationVariables.UserConfigFilePath).Close();
            }
            catch
            {
                return false;
            }

            XmlDocument DefaultConfig = new XmlDocument();
            XmlDeclaration DeclarationOfIndependence = DefaultConfig.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement Root = DefaultConfig.DocumentElement;
            DefaultConfig.InsertBefore(DeclarationOfIndependence, Root);

            XmlElement UserConfig = DefaultConfig.CreateElement("userconfig");
            DefaultConfig.AppendChild(UserConfig);

            XmlElement Application = DefaultConfig.CreateElement("application");
            UserConfig.AppendChild(Application);

            XmlElement ConfiguredServers = DefaultConfig.CreateElement("configuredservers");
            UserConfig.AppendChild(ConfiguredServers);

            try
            {
                DefaultConfig.Save(ApplicationVariables.UserConfigFilePath);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static List<JenkinsServer> LoadConfiguredServersFromUserConfig()
        {
            List<JenkinsServer> Servers = new List<JenkinsServer>();

            XmlDocument UserConfig = new XmlDocument();
            try
            {
                UserConfig.Load(ApplicationVariables.UserConfigFilePath);
            }
            catch
            {
                return Servers;
            }

            XmlNodeList ServerNodes = UserConfig.SelectNodes(@"userconfig/configuredservers/server");

            if (ServerNodes == null || ServerNodes.Count == 0)
                return Servers;

            foreach (XmlNode ServerNode in ServerNodes)
            {
                JenkinsServer Server = new JenkinsServer()
                {
                    id = ServerNode.Attributes["id"].Value,
                    name = ServerNode.Attributes["name"].Value,
                    url = ServerNode.Attributes["url"].Value
                };

                Servers.Add(Server);
            }

            return Servers;
        }

        private static bool AddServerToUserConfig(JenkinsServer Server)
        {
            XmlDocument UserConfig = new XmlDocument();
            try
            {
                UserConfig.Load(ApplicationVariables.UserConfigFilePath);
            }
            catch
            {
                return false;
            }

            XmlElement ServerElement = UserConfig.CreateElement("server");
            XmlAttribute IdAttribute = UserConfig.CreateAttribute("id");
            IdAttribute.Value = Server.id;
            XmlAttribute NameAttribute = UserConfig.CreateAttribute("name");
            NameAttribute.Value = Server.name;
            XmlAttribute UrlAttribute = UserConfig.CreateAttribute("url");
            UrlAttribute.Value = Server.url;

            ServerElement.Attributes.Append(IdAttribute);
            ServerElement.Attributes.Append(NameAttribute);
            ServerElement.Attributes.Append(UrlAttribute);

            XmlNode ConfiguredServers = UserConfig.SelectSingleNode(@"userconfig/configuredservers");
            ConfiguredServers.AppendChild(ServerElement);

            try
            {
                UserConfig.Save(ApplicationVariables.UserConfigFilePath);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static bool RemoveServerFromUserConfig(JenkinsServer Server)
        {
            XmlDocument UserConfig = new XmlDocument();
            try
            {
                UserConfig.Load(ApplicationVariables.UserConfigFilePath);
            }
            catch
            {
                return false;
            }

            XmlNodeList ServerNodes = UserConfig.SelectNodes(@"userconfig/configuredservers/server");

            if (ServerNodes == null || ServerNodes.Count == 0)
                return false;

            foreach (XmlNode ServerNode in ServerNodes)
            {
                if (ServerNode.Attributes["id"].Value == Server.id
                    && ServerNode.Attributes["name"].Value == Server.name
                    && ServerNode.Attributes["url"].Value == Server.url)
                {
                    XmlNode ConfiguredServers = UserConfig.SelectSingleNode(@"userconfig/configuredservers");
                    ConfiguredServers.RemoveChild(ServerNode);

                    try
                    {
                        UserConfig.Save(ApplicationVariables.UserConfigFilePath);

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            return false;
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

        private async void Button_ResendWelcomeEmail_Click(object sender, RoutedEventArgs e)
        {
            APCAccount Account = (APCAccount)(sender as Button).DataContext;
            WelcomeEmailSendTo SendTo = WelcomeEmailSendTo.PrimaryAccountEmail;
            if (specifyEmail_RadioButton.IsChecked == true)
            {
                SendTo = WelcomeEmailSendTo.SpecifiedEmail;
            }
            string SpecifiedEmail = specifyEmail_TextBox.Text.Trim();

            await JenkinsTasks.resendWelcomeEmail(Account, SendTo, SpecifiedEmail);
        }

        private async void Button_ChangeInactivityTimeout_Click(object sender, RoutedEventArgs e)
        {
            APCAccount Account = (APCAccount)(sender as Button).DataContext;
            string NewTimeoutValue = newTimeoutValue_TextBox.Text.Trim();

            await JenkinsTasks.updateTimeout(Account, NewTimeoutValue);
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

            if (Database.UserLoadStatus == JenkinsBuildStatus.Failed || Database.UserLoadStatus == JenkinsBuildStatus.NotStarted)
                Database.Users = await JenkinsTasks.getDatabaseUsers(Database, Account.JenkinsServer);
        }

        private void ValidateTextInputNumbersOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ConfigPane_Back_Button_Click(object sender, RoutedEventArgs e)
        {
            if (JenkinsInfo.ConfiguredJenkinsServers.Count < 0)
            {
                if (MessageBox.Show("You have no configured servers. Are you sure you want to leave?", "No Configured Servers", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return;
            }

            CurrentWindowState.Instance.DisplayMode = WindowDisplayMode.Lookup;
        }

        private void LookupListPane_Configure_Button_Click(object sender, RoutedEventArgs e)
        {
            CurrentWindowState.Instance.DisplayMode = WindowDisplayMode.Config;
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

    /// <summary>
    /// Intended to convert to a progress icon, but I couldn't get that to work in reasonable time. Converts to a text representation of the status instead.
    /// </summary>
    public class JenkinsBuildStatusToProgressIndicatorIcon_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || (value as string) == "")
                return null;

            if ((value as string) == JenkinsBuildStatus.NotStarted.ToString())
                return null;

            if ((value as string) == JenkinsBuildStatus.InProgress.ToString())
            {
                return "In progress...";
            }

            if ((value as string) == JenkinsBuildStatus.Successful.ToString())
            {
                return "Complete";
            }

            if ((value as string) == JenkinsBuildStatus.Failed.ToString())
            {
                return "Task failed. Account may be suspended.";
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            throw new Exception("This method is not implemented.");
        }
    }

    /// <summary>
    /// Returns a Visibility state for the pane.
    /// Parameter must contain the WindowDisplayModes that the pane wants to display in.
    /// "reverse" in parameter will reverse the option.
    /// "collapsible" in parameter will return "Collapsed" instead of "Hidden".
    /// Uses String.Contains() to find args, so don't need to delimit values but it's a good idea to anyway.
    /// Parameters and DisplayModes are made ToLower() to prevent caps from causing any issues.
    /// </summary>
    public class WindowDisplayModeToVisibility_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string Parameters = null;
            bool VisibilityBool = false;

            if (parameter != null && parameter is string)
                Parameters = (parameter as string).ToLower();

            if (Parameters.Contains(CurrentWindowState.Instance.DisplayMode.ToString().ToLower()))
                VisibilityBool = true;

            if (Parameters.Contains("reverse"))
                VisibilityBool = !VisibilityBool;

            if (VisibilityBool)
                return Visibility.Visible;

            if (Parameters.Contains("collapsible"))
                return Visibility.Collapsed;
            else
                return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            throw new Exception("This method is not implemented.");
        }
    }

    public enum WindowDisplayMode
    {
        Lookup,
        Config
    }
}