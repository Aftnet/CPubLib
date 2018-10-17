using System.IO;
using System.Threading.Tasks;

namespace CPubLib.Internal
{
    internal class ImageInfo
    {
        public static ImageInfo Bmp(int width, int height)
        {
            return new ImageInfo(".bmp", "image/bmp", width, height);
        }

        public static ImageInfo Gif(int width, int height)
        {
            return new ImageInfo(".gif", "image/gif", width, height);
        }

        public static ImageInfo Jpeg(int width, int height)
        {
            return new ImageInfo(".jpg", "image/jpeg", width, height);
        }

        public static ImageInfo Png(int width, int height)
        {
            return new ImageInfo(".png", "image/png", width, height);
        }

        public string Extension { get; }
        public string MimeType { get; }
        public int Width { get; }
        public int Height { get; }

        private ImageInfo(string extension, string mimeType, int width, int height)
        {
            Extension = extension;
            MimeType = mimeType;
            Width = width;
            Height = height;
        }
    }

    internal struct ImageSize
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    internal interface IImageDecoder
    {
        Task<ImageInfo> DecodeAsync(Stream imageStream);
    }
}
