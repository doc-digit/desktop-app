using System.Windows.Media;

namespace DesktopApp.DataModels
{
    public class ScannedPage
    {
        public string Id { get; set; }
        public ImageSource Scan { get; set; }
        public bool IsUploaded { get; set; }

        public ScannedPage(string id, ImageSource scan, bool isUploaded)
        {
            Id = id;
            Scan = scan;
            IsUploaded = isUploaded;
        }
    }
}
