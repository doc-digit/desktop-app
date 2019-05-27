using ModernWpf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DocDigitFinal
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            UIHooks.EnableHighDpiSupport();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Theme.ApplyTheme(ThemeColor.Light, Accent.Green);
            base.OnStartup(e);
        }
    }
}
