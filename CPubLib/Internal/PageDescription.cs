namespace CPubLib.Internal
{
    internal class PageDescription : ItemDescription
    {
        public string NavigationLabel { get; set; }

        public PageDescription(string id, string path, string navigationLabel = null) :
            base(id, path, "application/xhtml+xml", "svg")
        {
            NavigationLabel = navigationLabel;
        }
    }
}
