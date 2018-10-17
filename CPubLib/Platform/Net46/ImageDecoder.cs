using CPubLib.Internal;
using System.Windows.Media.Imaging;
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
                var decoder = BitmapDecoder.Create(imageStream, BitmapCreateOptions.None, BitmapCacheOption.None);
                if (decoder != null)
                {
                    var frame = decoder.Frames[0];

                    if (decoder is BmpBitmapDecoder)
                    {
                        output = ImageInfo.Bmp(frame.PixelWidth, frame.PixelHeight);
                    }
                    else if (decoder is GifBitmapDecoder)
                    {
                        output = ImageInfo.Gif(frame.PixelWidth, frame.PixelHeight);
                    }
                    else if (decoder is JpegBitmapDecoder)
                    {
                        output = ImageInfo.Jpeg(frame.PixelWidth, frame.PixelHeight);
                    }
                    else if (decoder is PngBitmapDecoder)
                    {
                        output = ImageInfo.Png(frame.PixelWidth, frame.PixelHeight);
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
