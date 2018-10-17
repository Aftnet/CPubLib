using CPubLib.Internal;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace CPubLib.Platform
{
    internal class ImageDecoder : IImageDecoder
    {
        public async Task<ImageFormat> DetectFormatAsync(Stream imageStream)
        {
            try
            {
                var decoder = await BitmapDecoder.CreateAsync(imageStream.AsRandomAccessStream());
                //decoder.DecoderInformation.CodecId == BitmapDecoder.BmpDecoderId
            }
            catch
            {
                return null;
            }

            return null;
        }

        public Task<ImageSize> DetectSizeAsync(Stream imageStream)
        {
            return null;
        }
    }
}
