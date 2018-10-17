using System.IO;
using System.Threading.Tasks;

namespace CPubLib.Internal
{
    internal class ImageFormat
    {
        public static ImageFormat Bmp { get; } = new ImageFormat(".bmp", "image/bmp");
        public static ImageFormat Jpeg { get; } = new ImageFormat(".jpg", "image/jpeg");
        public static ImageFormat Gif { get; } = new ImageFormat(".gif", "image/gif");
        public static ImageFormat Png { get; } = new ImageFormat(".png", "image/png");

        public string Extension { get; }
        public string MimeType { get; }

        private ImageFormat(string extension, string mimeType)
        {
            Extension = extension;
            MimeType = mimeType;
        }
    }

    internal struct ImageSize
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    internal interface IImageDecoder
    {
        Task<ImageFormat> DetectFormatAsync(Stream imageStream);
        Task<ImageSize> DetectSizeAsync(Stream imageStream);
    }
}
