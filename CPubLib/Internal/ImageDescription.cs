namespace CPubLib.Internal
{
    internal class ImageDescription : ItemDescription
    {
        public int Width { get; }
        public int Height { get; }

        public ImageDescription(string id, string path, int width, int height, string mimeType, string properties = null) :
            base(id, path, mimeType, properties)
        {
            Width = width;
            Height = height;
        }
    }
}
