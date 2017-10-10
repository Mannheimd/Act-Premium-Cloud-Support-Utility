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
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Xml;

namespace Act__Premium_Cloud_Support_Utility
{
    public class CurrentWindowState : DependencyObject
    {
        public static readonly CurrentWindowState Instance = new CurrentWindowState();
        private CurrentWindowState() { }

        public static string LocationTop { get; set; }
        public static string LocationLeft { get; set; }
        public static string Height { get; set; }
        public static string Width { get; set; }
        public static WindowState WindowStateStaging { get; set; }

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

        public WindowState State
        {
            get
            {
                return (WindowState)GetValue(StateProperty);
            }
            set
            {
                SetValue(StateProperty, value);
            }
        }

        public static readonly DependencyProperty DisplayModeProperty = DependencyProperty.Register("DisplayMode", typeof(WindowDisplayMode), typeof(CurrentWindowState), new UIPropertyMetadata());
        public static readonly DependencyProperty StateProperty = DependencyProperty.Register("State", typeof(WindowState), typeof(CurrentWindowState), new UIPropertyMetadata());
    }

    public static class ApplicationVariables
    {
        public static string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Glacier";
        public static string UserConfigFilePath = AppDataPath + @"\userconfig.xml";
        public static ReadOnlyCollection<TimeZoneInfo> TimeZones = TimeZoneInfo.GetSystemTimeZones();
    }

    static class ApplicationSettings
    {
        public static void Load()
        {
            XmlDocument UserConfig = new XmlDocument();
            try
            {
                UserConfig.Load(ApplicationVariables.UserConfigFilePath);

                CurrentWindowState.Height = UserConfig.SelectSingleNode(@"/userconfig/application/height").InnerText;
                CurrentWindowState.Width = UserConfig.SelectSingleNode(@"/userconfig/application/width").InnerText;
                CurrentWindowState.LocationTop = UserConfig.SelectSingleNode(@"/userconfig/application/locationtop").InnerText;
                CurrentWindowState.LocationLeft = UserConfig.SelectSingleNode(@"/userconfig/application/locationleft").InnerText;

                // This has to be after Top and Left, otherwise the maximisation doesn't work to maximum correctness
                WindowState State;
                Enum.TryParse(UserConfig.SelectSingleNode(@"/userconfig/application/windowstate").InnerText, out State);
                CurrentWindowState.WindowStateStaging = State;
            }
            catch
            {

            }
        }

        public static void Save(WindowState WindowStateEnum)
        {
            XmlDocument UserConfig = new XmlDocument();
            try
            {
                UserConfig.Load(ApplicationVariables.UserConfigFilePath);

                XmlNode ApplicationSettings = UserConfig.SelectSingleNode(@"userconfig/application");

                if (ApplicationSettings.SelectSingleNode(@"height") != null)
                    ApplicationSettings.SelectSingleNode(@"height").InnerText = CurrentWindowState.Height;
                else
                {
                    XmlElement NewNode = UserConfig.CreateElement("height");
                    XmlText NewText = UserConfig.CreateTextNode(CurrentWindowState.Height);
                    NewNode.AppendChild(NewText);
                    ApplicationSettings.AppendChild(NewNode);
                }

                if (ApplicationSettings.SelectSingleNode(@"width") != null)
                    ApplicationSettings.SelectSingleNode(@"width").InnerText = CurrentWindowState.Width;
                else
                {
                    XmlElement NewNode = UserConfig.CreateElement("width");
                    XmlText NewText = UserConfig.CreateTextNode(CurrentWindowState.Width);
                    NewNode.AppendChild(NewText);
                    ApplicationSettings.AppendChild(NewNode);
                }

                if (ApplicationSettings.SelectSingleNode(@"locationtop") != null)
                    ApplicationSettings.SelectSingleNode(@"locationtop").InnerText = CurrentWindowState.LocationTop;
                else
                {
                    XmlElement NewNode = UserConfig.CreateElement("locationtop");
                    XmlText NewText = UserConfig.CreateTextNode(CurrentWindowState.LocationTop);
                    NewNode.AppendChild(NewText);
                    ApplicationSettings.AppendChild(NewNode);
                }

                if (ApplicationSettings.SelectSingleNode(@"locationleft") != null)
                    ApplicationSettings.SelectSingleNode(@"locationleft").InnerText = CurrentWindowState.LocationLeft;
                else
                {
                    XmlElement NewNode = UserConfig.CreateElement("locationleft");
                    XmlText NewText = UserConfig.CreateTextNode(CurrentWindowState.LocationLeft);
                    NewNode.AppendChild(NewText);
                    ApplicationSettings.AppendChild(NewNode);
                }

                if (ApplicationSettings.SelectSingleNode(@"windowstate") != null)
                    ApplicationSettings.SelectSingleNode(@"windowstate").InnerText = WindowStateEnum.ToString();
                else
                {
                    XmlElement NewNode = UserConfig.CreateElement("windowstate");
                    XmlText NewText = UserConfig.CreateTextNode(WindowStateEnum.ToString());
                    NewNode.AppendChild(NewText);
                    ApplicationSettings.AppendChild(NewNode);
                }
            }
            catch
            {
                return;
            }

            try
            {
                UserConfig.Save(ApplicationVariables.UserConfigFilePath);
            }
            catch
            {
                return;
            }
        }
    }

    public partial class MainWindow : Window
    {
        public static ObservableCollection<APCAccount> LookupResults = new ObservableCollection<APCAccount>();

        public MainWindow()
        {
            if (!ValidateUserConfigFile())
                CreateDefaultUserConfigFile();

            ApplicationSettings.Load();

            JenkinsInfo.Instance.AvailableJenkinsServers = JenkinsTasks.getJenkinsServerList();
            JenkinsInfo.Instance.ConfiguredJenkinsServers = LoadConfiguredServersFromUserConfig();
            JenkinsInfo.Instance.LookupTypeList = JenkinsTasks.buildAPCLookupTypeList();

            if (JenkinsInfo.Instance.ConfiguredJenkinsServers.Count < 1)
                CurrentWindowState.Instance.DisplayMode = WindowDisplayMode.Config;
            else
                CurrentWindowState.Instance.DisplayMode = WindowDisplayMode.Lookup;

            LoadConfiguredServersFromUserConfig();

            InitializeComponent();

            LookupListPane_Lookups_ListBox.ItemsSource = LookupResults;

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

            //LookupResults.Add(DebugAccount);
        }

        private static bool ValidateUserConfigFile()
        {
            if (!File.Exists(ApplicationVariables.UserConfigFilePath))
                return false;

            XmlDocument UserConfig = new XmlDocument();
            try
            {
                UserConfig.Load(ApplicationVariables.UserConfigFilePath);
            }
            catch
            {
                return false;
            }

            if (UserConfig.SelectSingleNode(@"userconfig/application") == null)
                return false;

            if (UserConfig.SelectSingleNode(@"userconfig/configuredservers") == null)
                return false;

            return true;
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
            if (LookupResults.Count >= 10)
            {
                MessageBox.Show("Too many lookups");
                return;
            }

            APCAccount Account = new APCAccount();
            Account.LookupCreateTime = DateTime.Now;

            if (JenkinsInfo.Instance.ConfiguredJenkinsServers.Count == 1)
            {
                Account.JenkinsServer = JenkinsInfo.Instance.ConfiguredJenkinsServers[0];
            }

            LookupResults.Insert(0, Account);
            LookupListPane_Lookups_ListBox.SelectedIndex = 0;
        }

        private void LookupListPane_RemoveLookup_Click(object sender, RoutedEventArgs e)
        {
            APCAccount Account = (APCAccount)(sender as Button).DataContext;
            LookupResults.Remove(Account);
        }

        private async void Button_ResendWelcomeEmail_Click(object sender, RoutedEventArgs e)
        {
            string SpecifiedEmail = specifyEmail_TextBox.Text.Trim();

            APCAccount Account = (APCAccount)(sender as Button).DataContext;
            WelcomeEmailSendTo SendTo = WelcomeEmailSendTo.PrimaryAccountEmail;
            if (specifyEmail_RadioButton.IsChecked == true)
            {
                SendTo = WelcomeEmailSendTo.SpecifiedEmail;

                if (SpecifiedEmail == null || SpecifiedEmail == "")
                    return;
            }

            await JenkinsTasks.resendWelcomeEmail(Account, SendTo, SpecifiedEmail);
        }

        private async void Button_ChangeInactivityTimeout_Click(object sender, RoutedEventArgs e)
        {
            APCAccount Account = (APCAccount)(sender as Button).DataContext;
            string NewTimeoutValue = newTimeoutValue_TextBox.Text.Trim();

            if (NewTimeoutValue == null || NewTimeoutValue == "")
                return;

            await JenkinsTasks.updateTimeout(Account, NewTimeoutValue);
        }

        private async void Button_ResetUserPassword_Click(object sender, RoutedEventArgs e)
        {
            if (!((APCDatabaseUser)LookupResults_UserList.SelectedItem is APCDatabaseUser))
                return;

            APCDatabaseUser User = (APCDatabaseUser)LookupResults_UserList.SelectedItem;
            if (User != null)
            {
                await JenkinsTasks.resetUserPassword(User);
            }
        }

        private async void Button_UnlockDatabase_Click(object sender, RoutedEventArgs e)
        {
            if (!((APCDatabase)LookupResults_DatabaseList.SelectedItem is APCDatabase))
                return;

            APCDatabase Database = (APCDatabase)LookupResults_DatabaseList.SelectedItem;
            if (Database != null)
            {
                await JenkinsTasks.unlockDatabase(Database);
            }
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

        private async void NewLookupPane_LookupValueBox_KeyDown(object sender, KeyEventArgs e)
        {
            APCAccount account = (APCAccount)(sender as TextBox).DataContext;

            account.LookupValue = (sender as TextBox).Text; // Force this to apply for every keydown, as the data binding doesn't pass it otherwise until the box is left

            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;

                if (account.JenkinsServer != null
                    && account.LookupType != null
                    && account.LookupValue != null
                    && account.LookupValue.Trim() != "")
                {
                    await JenkinsTasks.RunAPCAccountLookup(account);
                }
            }
        }

        private async void LookupResults_DatabaseBackups_LoadBackups_Click(object sender, RoutedEventArgs e)
        {
            if (!((APCDatabase)LookupResults_DatabaseList.SelectedItem is APCDatabase))
                return;

            APCDatabase Database = (APCDatabase)LookupResults_DatabaseList.SelectedItem;
            if (Database.Server != null
                && Database.Name != null)
            {
                Database.Backups =  await JenkinsTasks.getDatabaseBackups(Database, Database.Database_APCAccount.JenkinsServer);
                Database.RestoreableBackups = JenkinsTasks.GetRestorableBackupsFromFiles(Database.Backups);
            }
        }

        private async void LookupResults_DatabaseBackups_RetainBackup_Click(object sender, RoutedEventArgs e)
        {
            if (!((APCDatabaseBackupRestorable)LookupResults_BackupList.SelectedItem is APCDatabaseBackupRestorable))
                return;

            APCDatabaseBackupRestorable Backup = (APCDatabaseBackupRestorable)LookupResults_BackupList.SelectedItem;
            if (Backup != null)
            {
                await JenkinsTasks.RetainDatabaseBackup(Backup);
            }
        }

        private async void LookupResults_DatabaseList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count < 1 || e.AddedItems[0] == null || !((e.AddedItems[0] as APCDatabase) is APCDatabase))
                return;

            if ((e.AddedItems[0] as APCDatabase).UserLoadStatus == JenkinsBuildStatus.Failed || (e.AddedItems[0] as APCDatabase).UserLoadStatus == JenkinsBuildStatus.NotStarted)
                (e.AddedItems[0] as APCDatabase).Users = await JenkinsTasks.getDatabaseUsers((e.AddedItems[0] as APCDatabase), (e.AddedItems[0] as APCDatabase).Database_APCAccount.JenkinsServer);
        }

        private void ValidateTextInputNumbersOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ConfigPane_Back_Button_Click(object sender, RoutedEventArgs e)
        {
            if (JenkinsInfo.Instance.ConfiguredJenkinsServers.Count < 1)
            {
                if (MessageBox.Show("You have no configured servers. Are you sure you want to leave?", "No Configured Servers", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return;
            }

            CurrentWindowState.Instance.DisplayMode = WindowDisplayMode.Lookup;
        }

        private async void ConfigPane_AddServer_Go_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigPane_AvailableJenkinsServerList.SelectedItem == null || !(ConfigPane_AvailableJenkinsServerList.SelectedItem is JenkinsServer))
                return;

            // Secure and store the new login credentials
            JenkinsTasks.SecureJenkinsCreds(
                ConfigPane_AddServer_Username_TextBox.Text,
                ConfigPane_AddServer_Token_TextBox.Text,
                (ConfigPane_AvailableJenkinsServerList.SelectedItem as JenkinsServer).id
                );

            // Test login
            if (!await JenkinsTasks.checkServerLogin(ConfigPane_AvailableJenkinsServerList.SelectedItem as JenkinsServer))
            {
                MessageBox.Show("Login failed. Verify you are using the correct user name and API token.", "Login Failed");
                return;
            }

            // Add the server to the config
            AddServerToUserConfig(ConfigPane_AvailableJenkinsServerList.SelectedItem as JenkinsServer);
            JenkinsInfo.Instance.ConfiguredJenkinsServers = LoadConfiguredServersFromUserConfig();

            // For some reason the ConfiguredServers thing won't bind properly, so we have to update it each time we change it
            Config_ConfiguredJenkinsServerList.ItemsSource = JenkinsInfo.Instance.ConfiguredJenkinsServers;

            // Deselect the available server, and empty the text boxes
            ConfigPane_AvailableJenkinsServerList.SelectedIndex = -1;
            ConfigPane_AddServer_Username_TextBox.Text = null;
            ConfigPane_AddServer_Token_TextBox.Text = null;
        }

        private void ConfigPane_RemoveServer_Click(object sender, RoutedEventArgs e)
        {
            if (Config_ConfiguredJenkinsServerList.SelectedItem == null || !(Config_ConfiguredJenkinsServerList.SelectedItem is JenkinsServer))
                return;

            RemoveServerFromUserConfig(Config_ConfiguredJenkinsServerList.SelectedItem as JenkinsServer);
            JenkinsInfo.Instance.ConfiguredJenkinsServers = LoadConfiguredServersFromUserConfig();

            // For some reason the ConfiguredServers thing won't bind properly, so we have to update it each time we change it
            Config_ConfiguredJenkinsServerList.ItemsSource = JenkinsInfo.Instance.ConfiguredJenkinsServers;
        }

        private void LookupListPane_Configure_Button_Click(object sender, RoutedEventArgs e)
        {
            CurrentWindowState.Instance.DisplayMode = WindowDisplayMode.Config;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            WindowState WindowStateToSave = (WindowState)GetValue(MainWindow.WindowStateProperty);

            if (WindowStateToSave == WindowState.Maximized)
            {
                CurrentWindowState.Instance.State = WindowState.Normal;
            }

            ApplicationSettings.Save(WindowStateToSave);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentWindowState.Instance.State = CurrentWindowState.WindowStateStaging;

            // The binding doesn't work properly and I don't have time to fix it, so here.
            SetValue(MainWindow.WindowStateProperty, CurrentWindowState.WindowStateStaging);
        }

        private async void LookupResults_Reset_Button_Click(object sender, RoutedEventArgs e)
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

        private void Label_RightClick_Copy(object sender, MouseButtonEventArgs e)
        {
            if ((sender as Label).Content == null || !(((sender as Label).Content) is string))
                return;

            string Text = (sender as Label).Content as string;

            try
            {
                Clipboard.SetDataObject(Text);

                ((Storyboard)FindResource("showAndFadeAnimation")).Begin(textCopiedAlert);
            }
            catch
            { }
        }

        private void TextBlock_RightClick_Copy(object sender, MouseButtonEventArgs e)
        {
            if ((sender as TextBlock).Text == null || !(((sender as TextBlock).Text) is string))
                return;

            string Text = (sender as TextBlock).Text as string;

            try
            {
                Clipboard.SetDataObject(Text);

                ((Storyboard)FindResource("showAndFadeAnimation")).Begin(textCopiedAlert);
            }
            catch
            { }
        }
    }

    public class ListBoxSelectedToVisibility_Converter : IValueConverter
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

    public class ListBoxSelectedToBool_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool Output = false;

            if (value != null)
                Output = true;

            if (parameter != null && parameter.ToString() == "Reverse")
                Output = !Output;

            return Output;
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
    public class AccountLookupStatusToVisibility_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool VisibilityBool = false;
            string Parameters = null;

            if (parameter != null && parameter is string)
                Parameters = (parameter as string);
            else Parameters = "";

            if (value == null)
                VisibilityBool = false;

            if (Parameters.Contains(((APCAccountLookupStatus)value).ToString()))
                VisibilityBool = true;

            if (Parameters.Contains("Reverse"))
                VisibilityBool = !VisibilityBool;

            if (VisibilityBool)
                return Visibility.Visible;

            if (Parameters.Contains("Collapsible"))
                return Visibility.Collapsed;
            else
                return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            throw new Exception("This method is not implemented.");
        }
    }

    public class AccountLookupStatusToString_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value.ToString() == APCAccountLookupStatus.Successful.ToString())
                return "Lookup complete";

            if (value != null && value.ToString() == APCAccountLookupStatus.NotStarted.ToString())
                return "Not started";

            if (value != null && value.ToString() == APCAccountLookupStatus.NotFound.ToString())
                return "Account not found";

            if (value != null && value.ToString() == APCAccountLookupStatus.InProgress.ToString())
                return "Locating account...";

            if (value != null && value.ToString() == APCAccountLookupStatus.Refreshing.ToString())
                return "Refreshing...";

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
            if (value == null)
                return null;

            JenkinsBuildStatus Status = (JenkinsBuildStatus)value;

            if (Status == JenkinsBuildStatus.NotStarted)
                return "Not started";

            if (Status == JenkinsBuildStatus.InProgress)
                return "Working...";

            if (Status == JenkinsBuildStatus.Successful)
                return "Successful";

            if (Status == JenkinsBuildStatus.Failed)
                return "Failed";

            return null;
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            throw new Exception("This method is not implemented.");
        }
    }

    public class JenkinsBuildStatusToVisibility_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool VisibilityBool = false;
            string Parameters = null;

            if (parameter != null && parameter is string)
                Parameters = (parameter as string);
            else Parameters = "";

            if (value == null)
                VisibilityBool = false;

            if (Parameters.Contains(((JenkinsBuildStatus)value).ToString()))
                VisibilityBool = true;

            if (Parameters.Contains("Reverse"))
                VisibilityBool = !VisibilityBool;

            if (VisibilityBool)
                return Visibility.Visible;

            if (Parameters.Contains("Collapsible"))
                return Visibility.Collapsed;
            else
                return Visibility.Hidden;
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

    /// <summary>
    /// Converts APCAccountSelectedTab to a ListBox SelectedItem integer
    /// </summary>
    public class DatabasesSubItemSelectedTab_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0;

            if (value.ToString() == APCDatabasesSubItemSelectedTab.Info.ToString())
                return 0;

            if (value.ToString() == APCDatabasesSubItemSelectedTab.Users.ToString())
                return 1;

            if (value.ToString() == APCDatabasesSubItemSelectedTab.Backups.ToString())
                return 2;

            return -1;
        }

        public object ConvertBack(object value, Type targetType, object Parameter, CultureInfo culture)
        {
            if (value.ToString() == "-1")
                return APCDatabasesSubItemSelectedTab.Info;

            if (value.ToString() == "0")
                return APCDatabasesSubItemSelectedTab.Info;

            if (value.ToString() == "1")
                return APCDatabasesSubItemSelectedTab.Users;

            if (value.ToString() == "2")
                return APCDatabasesSubItemSelectedTab.Backups;

            return APCDatabasesSubItemSelectedTab.Info;
        }
    }

    public class DatabasesSubItemSelectedToVisibility_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string Parameters = null;
            bool VisibilityBool = false;

            if (parameter != null && parameter is string)
                Parameters = (parameter as string).ToLower();

            if (value != null)
            {
                DatabasesSubItemSelectedTab_Converter Converter = new DatabasesSubItemSelectedTab_Converter();
                APCDatabasesSubItemSelectedTab SelectedTab = (APCDatabasesSubItemSelectedTab)Converter.ConvertBack(value, targetType, parameter, culture);

                if (Parameters.Contains(SelectedTab.ToString().ToLower()))
                    VisibilityBool = true;
            }

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

    public class JenkinsRootUrlToConfigureUrl_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string && value != null)
            {
                string RootUrl = (value as string);

                if (RootUrl.EndsWith(@"/"))
                    return new System.Uri(RootUrl + "me/configure");
                else
                    return new System.Uri(RootUrl + "/me/configure");
            }

            return null;
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