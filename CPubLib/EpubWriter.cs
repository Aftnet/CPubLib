using CPubLib.Internal;
using CPubLib.Platform;
using CPubLib.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace CPubLib
{
    public class EPUBWriter : IDisposable
    {
        public const int FileFormatVersion = 1;

        private static IImageDecoder ImageDecoder { get; } = new ImageDecoder();
        private ZipArchive BackingArchive { get; }
        private bool StaticDataAdded { get; set; } = false;
        private bool DynamicDataAdded { get; set; } = false;

        private IList<ItemDescription> Contents { get; } = new List<ItemDescription>();
        private IList<PageDescription> Pages { get; } = new List<PageDescription>();

        private bool CoverSet = false;
        private int ChapterCounter = 0;
        private int PageCounter = 0;

        public Metadata Metadata { get; } = new Metadata();

        public EPUBWriter(Stream stream)
        {
            BackingArchive = new ZipArchive(stream, ZipArchiveMode.Create);
        }

        public void Dispose()
        {
            BackingArchive.Dispose();
        }

        public async Task SetCoverAsync(Stream imageData, bool setAsFirstPage = false, bool checkAspectRatio = true)
        {
            if (CoverSet)
            {
                throw new InvalidOperationException("Cover can only be set once");
            }

            await AddStaticDataAsync().ConfigureAwait(false);
            var image = await AddImageAsync(imageData, "S00-Cover", true, checkAspectRatio).ConfigureAwait(false);
            if (setAsFirstPage)
            {
                await AddPagesForImageAsync(image, null).ConfigureAwait(false);
            }

            CoverSet = true;
        }

        public async Task AddPageAsync(Stream imageData, string navigationLabel = null)
        {
            if (!CoverSet)
            {
                throw new InvalidOperationException("Cover needs to be set before adding pages");
            }

            if (!string.IsNullOrEmpty(navigationLabel) || ChapterCounter == 0)
            {
                ChapterCounter++;
                PageCounter = 0;
            }

            PageCounter++;
            var image = await AddImageAsync(imageData, $"S01-C{ChapterCounter:D6}P{PageCounter:D6}", false, false).ConfigureAwait(false);
            await AddPagesForImageAsync(image, navigationLabel).ConfigureAwait(false);
        }

        private async Task AddPagesForImageAsync(ImageDescription image, string pageNavLabel)
        {
            var fileNameBase = Path.GetFileNameWithoutExtension(image.Path);
            if (image.Width < image.Height)
            {
                await AddPageForImageWithFittingAsync(image, pageNavLabel, EpubXmlWriter.ImageFitting.Full).ConfigureAwait(false);
            }
            else if(Metadata.RightToLeftReading)
            {
                await AddPageForImageWithFittingAsync(image, pageNavLabel, EpubXmlWriter.ImageFitting.RightHalf).ConfigureAwait(false);
                await AddPageForImageWithFittingAsync(image, null, EpubXmlWriter.ImageFitting.LeftHalf).ConfigureAwait(false);
            }
            else
            {
                await AddPageForImageWithFittingAsync(image, pageNavLabel, EpubXmlWriter.ImageFitting.LeftHalf).ConfigureAwait(false);
                await AddPageForImageWithFittingAsync(image, null, EpubXmlWriter.ImageFitting.RightHalf).ConfigureAwait(false);
            }
        }

        private async Task AddPageForImageWithFittingAsync(ImageDescription image, string pageNavLabel, EpubXmlWriter.ImageFitting fitting)
        {
            var fileNameBase = Path.GetFileNameWithoutExtension(image.Path);
            var refProperties = default(string);
            switch(fitting)
            {
                case EpubXmlWriter.ImageFitting.LeftHalf:
                    fileNameBase += "_L";
                    refProperties = "page-spread-left";
                    break;
                case EpubXmlWriter.ImageFitting.RightHalf:
                    fileNameBase += "_R";
                    refProperties = "page-spread-right";
                    break;
            }

            var pageItem = new PageDescription($"p_{fileNameBase}", $"{fileNameBase}.xhtml", pageNavLabel, null, refProperties);
            var html = EpubXmlWriter.GenerateContentPage(image.Path, image.Width, image.Height, fitting);
            await AddTextEntryAsync($"{Strings.EpubContentRoot}{pageItem.Path}", html).ConfigureAwait(false);
            Contents.Add(pageItem);
            Pages.Add(pageItem);
        }

        public async Task FinalizeAsync()
        {
            if (DynamicDataAdded)
            {
                return;
            }

            if (!Pages.Any())
            {
                throw new InvalidOperationException("Unable to create book with no pages");
            }

            await AddTextEntryAsync(Strings.EpubContentEntryName, EpubXmlWriter.GenerateContentOPF(Metadata, Contents, Pages)).ConfigureAwait(false);

            if (!Pages.Where(d => d.NavigationLabel != null).Any())
            {
                Pages.First().NavigationLabel = Metadata.Title;
            }

            await AddTextEntryAsync(Strings.EpubNavEntryName, EpubXmlWriter.GenerateNavXML(Pages)).ConfigureAwait(false);
            DynamicDataAdded = true;
        }

        private async Task AddStaticDataAsync()
        {
            if (StaticDataAdded)
            {
                return;
            }

            await AddTextEntryAsync("mimetype", "application/epub+zip", CompressionLevel.NoCompression).ConfigureAwait(false);
            await AddTextEntryAsync("META-INF/container.xml", Strings.EpubContainerContent).ConfigureAwait(false);

            Contents.Add(new ItemDescription("nav", "nav.xhtml", "application/xhtml+xml", "nav"));

            StaticDataAdded = true;
        }

        private async Task<ImageDescription> AddImageAsync(Stream imageData, string fileNameBase, bool isCover, bool checkCoverAR)
        {
            var memStream = default(MemoryStream);
            var srcStream = imageData;

            if (!imageData.CanSeek)
            {
                memStream = new MemoryStream();
                await imageData.CopyToAsync(memStream).ConfigureAwait(false);
                memStream.Position = 0;
                srcStream = memStream;
            }

            var imageInfo = await ImageDecoder.DecodeAsync(srcStream).ConfigureAwait(false);
            srcStream.Position = 0;
            if (imageInfo == null)
            {
                throw new FormatException("Image data is of invalid or not recognized format");
            }

            const float coverARLimit = 3.0f / 4.0f;
            if (isCover && checkCoverAR)
            {
                if (((float)imageInfo.Width / (float)imageInfo.Height) > coverARLimit)
                {
                    throw new FormatException("Cover has unsuitable aspect ratio");
                }
            }

            var properties = isCover ? "cover-image" : null;
            var imageItem = new ImageDescription($"i_{fileNameBase}", $"{fileNameBase}{imageInfo.Extension}", imageInfo.Width, imageInfo.Height, imageInfo.MimeType, properties);
            await AddBinaryEntryAsync($"{Strings.EpubContentRoot}{imageItem.Path}", srcStream).ConfigureAwait(false);
            memStream?.Dispose();
            Contents.Add(imageItem);
            return imageItem;
        }

        private async Task AddTextEntryAsync(string entryName, string content, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            var entry = BackingArchive.CreateEntry(entryName, compressionLevel);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(content).ConfigureAwait(false);
            }
        }

        private async Task AddBinaryEntryAsync(string entryName, Stream content, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            var entry = BackingArchive.CreateEntry(entryName, compressionLevel);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream))
            {
                await content.CopyToAsync(stream).ConfigureAwait(false);
            }
        }
    }
}
