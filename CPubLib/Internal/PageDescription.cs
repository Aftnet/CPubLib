namespace CPubLib.Internal
{
    internal class PageDescription : ItemDescription
    {
        public bool IsLandscape { get; }
        public string NavigationLabel { get; set; }

        public PageDescription(string id, string path, bool isLandscape, string navigationLabel = null) :
            base(id, path, "application/xhtml+xml", null)
        {
            IsLandscape = isLandscape;
            NavigationLabel = navigationLabel;
        }
    }
}
