using DocDigitFinal.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
                    var result = await PostLogin();
                    new MainWindow(JsonConvert.DeserializeObject<User>(result)).Show();
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
                        ErrorLabel.Content = "Błąd serwera. Spróbuj ponownie później.";
                        PinBorder.BorderBrush = Brushes.Red;
                        PinBorder.BorderThickness = new Thickness(2);
                        ErrorLabel.Visibility = Visibility.Visible;
                        Console.WriteLine(ex.Message);
                    }
                    PinTextBox.Focus();
                }
            }
        }

        public async Task<string> PostLogin()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            byte[] dataBytes = Encoding.UTF8.GetBytes($"{{ \"pin\": {PinTextBox.Text}}}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{ConfigurationManager.AppSettings["api_uri"]}/user/login");
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentLength = dataBytes.Length;
            request.ContentType = "application/json";
            request.Method = "POST";

            using (Stream requestBody = request.GetRequestStream())
            {
                await requestBody.WriteAsync(dataBytes, 0, dataBytes.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private void PinTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = !(e.Key >= Key.D0 && e.Key <= Key.D9) && !(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
        }
    }
}
