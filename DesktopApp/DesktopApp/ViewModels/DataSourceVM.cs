using GalaSoft.MvvmLight;
using NTwain;

namespace DesktopApp
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
            var rc = DS.Open();
        }
    }
}
