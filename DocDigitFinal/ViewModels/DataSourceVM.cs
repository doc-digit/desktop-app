using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ModernWpf.Messages;
using NTwain;
using NTwain.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;

namespace DocDigitFinal
{
    /// <summary>
    /// Wraps a data source as view model.
    /// </summary>
    class DataSourceVM : ViewModelBase
    {
        public DataSource DS { get; set; }

        public string Name { get { return DS.Name; } }
        public string Version { get { return DS.Version.Info; } }
        public string Protocol { get { return DS.ProtocolVersion.ToString(); } }

        public void Open()
        {
            DS.Open();
        }
    }
}
