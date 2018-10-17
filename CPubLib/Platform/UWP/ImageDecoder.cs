using CPubLib.Internal;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace CPubLib.Platform
{
    internal class ImageDecoder : IImageDecoder
    {
        public async Task<ImageInfo> DecodeAsync(Stream imageStream)
        {
            var output = default(ImageInfo);
            try
            {
                var decoder = await BitmapDecoder.CreateAsync(imageStream.AsRandomAccessStream());
                var codecId = decoder.DecoderInformation.CodecId;

                if (codecId == BitmapDecoder.BmpDecoderId)
                {
                    output = ImageInfo.Bmp((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                }
                else if (codecId == BitmapDecoder.GifDecoderId)
                {
                    output = ImageInfo.Gif((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                }
                else if (codecId == BitmapDecoder.JpegDecoderId)
                {
                    output = ImageInfo.Jpeg((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                }
                else if (codecId == BitmapDecoder.PngDecoderId)
                {
                    output = ImageInfo.Png((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                }
            }
            catch
            {
                output = null;
            }

            return output;
        }
    }
}
