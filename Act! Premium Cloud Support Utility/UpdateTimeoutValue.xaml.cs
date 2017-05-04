using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Act__Premium_Cloud_Support_Utility
{
    /// <summary>
    /// Interaction logic for UpdateTimeoutValue.xaml
    /// </summary>
    public partial class UpdateTimeoutValue : Window
    {
        public UpdateTimeoutValue()
        {
            InitializeComponent();
        }
        
        public event Action<string> resultValue;
        public event Action<bool> resultProceed;

        private void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            resultValue(newValue_TextBox.Text);
            resultProceed(true);

            Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            resultProceed(false);

            Close();
        }

        private void verifyNumbersOnly_TypedText(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !isTextNumerical(e.Text);
        }

        private void verifyNumbersOnly_PastedText(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                if (!isTextNumerical(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private bool isTextNumerical(string text)
        {
            Regex regex = new Regex("[^0-9]+");
            return !regex.IsMatch(text);
        }
    }
}
