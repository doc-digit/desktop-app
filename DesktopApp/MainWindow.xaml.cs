using DesktopApp.Helpers;
using DesktopApp.ViewModels;
using GalaSoft.MvvmLight.Messaging;
using ModernWpf;
using ModernWpf.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace DesktopApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TwainVM twainVM;

        public MainWindow(User user, ObservableCollection<DocType> docTypes)
        {
            InitializeComponent();

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                twainVM = this.DataContext as TwainVM;
                twainVM.PropertyChanged += TwainVM_PropertyChanged;
                twainVM.CurrentUser = user;
                twainVM.DocTypes = docTypes;
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

        private void TwainVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "State")
            {
                if (twainVM.State == 5)
                {
                    Theme.ApplyTheme(ThemeColor.Light, Accent.Orange);
                }
                else if (twainVM.State == 4)
                {
                    Theme.ApplyTheme(ThemeColor.Light, Accent.Gold);
                }
            }
            if (e.PropertyName == "SelectedImage")
            {
                if (twainVM.CapturedImages.Count > 0)
                {
                    ImageControls.Visibility = Visibility.Visible;
                    SendButton.Visibility = Visibility.Visible;
                    ScansListBox.ScrollIntoView(twainVM.SelectedImage);
                }
                else
                {
                    ImageControls.Visibility = Visibility.Hidden;
                    SendButton.Visibility = Visibility.Hidden;
                }
            }
            if (e.PropertyName == "StartUpload") UploadingPDF.Visibility = Visibility.Visible;
            if (e.PropertyName == "Uploaded") UploadingPDF.Visibility = Visibility.Hidden;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = twainVM.State > 4;
            base.OnClosing(e);
        }
        protected override void OnClosed(EventArgs e)
        {
            Messenger.Default.Unregister(this);
            twainVM.CloseDown();
            base.OnClosed(e);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            twainVM.WindowHandle = new WindowInteropHelper(this).Handle;

        }

        private async void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var student = (sender as TextBox).Text;
            if (student.Length > 2)
            {
                try
                {
                    twainVM.Students = JsonConvert.DeserializeObject<ObservableCollection<Student>>(await WebRequestHelper.GetAsync(ConfigurationManager.AppSettings["ApiUri"] + $"/student/find?q={HttpUtility.UrlEncode(student)}"));
                }
                catch { }
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

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private void TheWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!LogoutButton.IsMouseOver && !UserNameButton.IsMouseOver) LogoutButton.Visibility = Visibility.Hidden;
            if (!SettingsButton.IsMouseOver && !SettingsPanel.IsMouseOver) SettingsPanel.Visibility = Visibility.Hidden;
        }
    }
}
