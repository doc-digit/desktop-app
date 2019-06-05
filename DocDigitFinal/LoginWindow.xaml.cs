using DocDigitFinal.Helpers;
using DocDigitFinal.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DocDigitFinal
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (PinTextBox.Text.Length > 2)
            {
                LoadingPanel.Visibility = Visibility.Visible;
                ErrorLabel.Visibility = Visibility.Hidden;
                PinBorder.BorderBrush = (Brush)FindResource("grayBrush");
                PinBorder.BorderThickness = new Thickness(1);
                try
                {
                    var userName = JsonConvert.DeserializeObject<User>(await WebRequestHelper.PostAsync("/user/login", $"{{ \"pin\": {PinTextBox.Text}}}", "application/json"));
                    var docTypes = JsonConvert.DeserializeObject<ObservableCollection<DocType>>(await WebRequestHelper.GetAsync("/document/types"));
                    new MainWindow(userName, docTypes).Show();
                    Close();
                }
                catch (Exception ex)
                {
                    LoadingPanel.Visibility = Visibility.Hidden;
                    if (ex.Message.Contains("Not Found"))
                    {
                        ErrorLabel.Content = "Błędny PIN";
                        PinBorder.BorderBrush = Brushes.Red;
                        PinBorder.BorderThickness = new Thickness(2);
                        ErrorLabel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ErrorLabel.Content = "Błąd połączenia. Spróbuj ponownie później.";
                        PinBorder.BorderBrush = Brushes.Red;
                        PinBorder.BorderThickness = new Thickness(2);
                        ErrorLabel.Visibility = Visibility.Visible;
                        Console.WriteLine(ex.Message);
                    }
                    PinTextBox.Focus();
                }
            }
        }

        private void PinTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = !(e.Key >= Key.D0 && e.Key <= Key.D9) && !(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) && !(e.Key == Key.Enter);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Button_Click(sender, e);
            }
            else if (!PinTextBox.IsFocused)
            {
                PinTextBox.Focus();
            }
        }
    }
}
