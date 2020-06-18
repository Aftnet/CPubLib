using CPubLib.Internal;
using SixLabors.ImageSharp;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CPubLib.Platform
{
    internal class ImageDecoder : IImageDecoder
    {
        public Task<ImageInfo> DecodeAsync(Stream imageStream)
        {
            var output = default(ImageInfo);

            try
            {
                var format = Image.DetectFormat(imageStream);
                if (format != null)
                {
                    var imageInfo = Image.Identify(imageStream);

                    if (SixLabors.ImageSharp.Formats.Bmp.BmpFormat.Instance.MimeTypes.Contains(format.DefaultMimeType))
                    {
                        output = ImageInfo.Bmp(imageInfo.Width, imageInfo.Height);
                    }
                    else if (SixLabors.ImageSharp.Formats.Gif.GifFormat.Instance.MimeTypes.Contains(format.DefaultMimeType))
                    {
                        output = ImageInfo.Gif(imageInfo.Width, imageInfo.Height);
                    }
                    else if (SixLabors.ImageSharp.Formats.Png.PngFormat.Instance.MimeTypes.Contains(format.DefaultMimeType))
                    {
                        output = ImageInfo.Jpeg(imageInfo.Width, imageInfo.Height);
                    }
                    else if (SixLabors.ImageSharp.Formats.Jpeg.JpegFormat.Instance.MimeTypes.Contains(format.DefaultMimeType))
                    {
                        output = ImageInfo.Png(imageInfo.Width, imageInfo.Height);
                    }
                }
            }
            catch
            {
                output = null;
            }

            return Task.FromResult(output);
        }
    }
}
