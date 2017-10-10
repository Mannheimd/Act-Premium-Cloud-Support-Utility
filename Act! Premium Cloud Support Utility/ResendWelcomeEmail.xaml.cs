using System;
using System.Windows;

namespace Act__Premium_Cloud_Support_Utility
{
    public partial class ResendWelcomeEmail : Window
    {
        public ResendWelcomeEmail(string accountEmail)
        {
            InitializeComponent();

            primaryEmail_RadioButton.Content = "Send to " + accountEmail;
        }

        public event Action<bool> resultBool;
        public event Action<string> resultString;
        public event Action<bool> resultSend;

        private void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            if (primaryEmail_RadioButton.IsChecked == true || (specifyEmail_RadioButton.IsChecked == true && specifyEmail_TextBox.Text != ""))
            {
                bool selectedRadio = specifyEmail_RadioButton.IsChecked.Value;

                resultBool(selectedRadio);
                resultString(specifyEmail_TextBox.Text);
                resultSend(true);

                Close();
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            resultSend(false);

            Close();
        }
    }
}
