namespace CPubLib.Internal
{
    internal class ItemDescription
    {
        public string ID { get; }
        public string Path { get; }
        public string MIMEType { get; }
        public string Properties { get; }

        public ItemDescription(string id, string path, string mimeType, string properties = null)
        {
            ID = id;
            Path = path;
            MIMEType = mimeType;
            Properties = properties;
        }
    }
}
