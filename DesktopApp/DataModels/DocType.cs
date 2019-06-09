namespace DesktopApp.ViewModels
{
    public class DocType
    {
        public int id { get; set; }
        public string name { get; set; }

        public override string ToString()
        {
            return name;
        }
    }
}
