using CPubLib.Internal;
using SixLabors.ImageSharp;
using System.IO;
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

                    if (format.DefaultMimeType == ImageFormats.Bmp.DefaultMimeType)
                    {
                        output = ImageInfo.Bmp(imageInfo.Width, imageInfo.Height);
                    }
                    else if (format.DefaultMimeType == ImageFormats.Gif.DefaultMimeType)
                    {
                        output = ImageInfo.Gif(imageInfo.Width, imageInfo.Height);
                    }
                    else if (format.DefaultMimeType == ImageFormats.Jpeg.DefaultMimeType)
                    {
                        output = ImageInfo.Jpeg(imageInfo.Width, imageInfo.Height);
                    }
                    else if (format.DefaultMimeType == ImageFormats.Png.DefaultMimeType)
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
