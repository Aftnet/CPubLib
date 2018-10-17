using CPubLib.Internal;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CPubLib.Platform
{
    internal class ImageDecoder : IImageDecoder
    {
        private static IReadOnlyDictionary<string, ImageFormat> MimeTypeMapping { get; } = new Dictionary<string, ImageFormat>
        {
            { SixLabors.ImageSharp.ImageFormats.Bmp.DefaultMimeType, ImageFormat.Bmp },
            { SixLabors.ImageSharp.ImageFormats.Jpeg.DefaultMimeType, ImageFormat.Jpeg },
            { SixLabors.ImageSharp.ImageFormats.Gif.DefaultMimeType, ImageFormat.Gif },
            { SixLabors.ImageSharp.ImageFormats.Png.DefaultMimeType, ImageFormat.Png },
        };

        public Task<ImageFormat> DetectFormatAsync(Stream imageStream)
        {
            var output = default(ImageFormat);
            var format = SixLabors.ImageSharp.Image.DetectFormat(imageStream);
            if (MimeTypeMapping.ContainsKey(format.DefaultMimeType))
            {
                output = MimeTypeMapping[format.DefaultMimeType];
            }

            return Task.FromResult(output);
        }

        public Task<ImageSize> DetectSizeAsync(Stream imageStream)
        {
            var imageInfo = SixLabors.ImageSharp.Image.Identify(imageStream);
            var output = new ImageSize { Width = imageInfo.Width, Height = imageInfo.Height };
            return Task.FromResult(output);
        }
    }
}
