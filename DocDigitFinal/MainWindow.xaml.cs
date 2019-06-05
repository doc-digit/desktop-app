using DocDigitFinal.ViewModels;
using GalaSoft.MvvmLight.Messaging;
using ModernWpf;
using ModernWpf.Controls;
using ModernWpf.Messages;
using Newtonsoft.Json;
using NTwain;
using NTwain.Data;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace DocDigitFinal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TwainVM _twainVM;
        String apiUrl;

        public MainWindow(User user)
        {
            InitializeComponent();

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _twainVM = this.DataContext as TwainVM;
                _twainVM.PropertyChanged += _twainVM_PropertyChanged;
                _twainVM.CurrentUser = user;
                apiUrl = ConfigurationManager.AppSettings["api_uri"];
                Messenger.Default.Register<RefreshCommandsMessage>(this, m => m.HandleIt());
                Messenger.Default.Register<ChooseFileMessage>(this, m =>
                {
                    m.HandleWithPlatform(this);
                });
                Messenger.Default.Register<MessageBoxMessage>(this, msg =>
                {
                    if (Dispatcher.CheckAccess())
                    {
                        msg.HandleWithModern(this);
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            msg.HandleWithModern(this);
                        }));
                    }
                });
            }
        }

        private void _twainVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "State")
            {
                if (_twainVM.State == 5)
                {
                    Theme.ApplyTheme(ThemeColor.Light, Accent.Orange);
                }
                else if (_twainVM.State == 4)
                {
                    Theme.ApplyTheme(ThemeColor.Light, Accent.Gold);
                }
            }
            if (e.PropertyName == "SelectedImage")
            {
                if (_twainVM.CapturedImages.Count > 0)
                {
                    ImageControls.Visibility = Visibility.Visible;
                    SendButton.Visibility = Visibility.Visible;
                    ScansListBox.ScrollIntoView(_twainVM.SelectedImage);
                }
                else
                {
                    ImageControls.Visibility = Visibility.Hidden;
                    SendButton.Visibility = Visibility.Hidden;
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = _twainVM.State > 4;
            base.OnClosing(e);
        }
        protected override void OnClosed(EventArgs e)
        {
            Messenger.Default.Unregister(this);
            _twainVM.CloseDown();
            base.OnClosed(e);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _twainVM.WindowHandle = new WindowInteropHelper(this).Handle;

        }

        private async void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var student = (sender as TextBox).Text;
            if (student.Length > 2)
            {
                await GetStudentAsync(student);
            }
        }

        public async Task GetStudentAsync(string student)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{apiUrl}/student/find?q={HttpUtility.UrlEncodeUnicode(student)}");
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                Console.WriteLine(request.RequestUri);
                Console.WriteLine(reader.ReadToEnd());
                _twainVM.Students = JsonConvert.DeserializeObject<ObservableCollection<Student>>(await reader.ReadToEndAsync());
            }
        }

        private void TheWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.F)
            {
                SearchStudentBox.Focus();
            }
        }

        private void UserNameButton_Click(object sender, RoutedEventArgs e)
        {
            LogoutButton.Visibility = Visibility.Visible;
        }

        bool isOverLogout = false;
        private void LogoutButton_MouseEnter(object sender, MouseEventArgs e)
        {
            isOverLogout = true;
        }

        private void LogoutButton_MouseLeave(object sender, MouseEventArgs e)
        {
            isOverLogout = false;
        }

        private void UserNameButton_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!isOverLogout) LogoutButton.Visibility = Visibility.Hidden;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _twainVM.CloseDown();
            new LoginWindow().Show();
            Close();
        }

    }
}
