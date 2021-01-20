namespace CPubLib.Internal
{
    internal class PageDescription : ItemDescription
    {
        public string NavigationLabel { get; set; }

        public PageDescription(string id, string path, string navigationLabel, string properties) :
            base(id, path, "application/xhtml+xml", string.IsNullOrEmpty(properties) ? "svg" : $"svg {properties}")
        {
            NavigationLabel = navigationLabel;
        }
    }
}
