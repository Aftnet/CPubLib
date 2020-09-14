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
        private static IImageDecoder ImageDecoder { get; } = new ImageDecoder();
        private ZipArchive BackingArchive { get; }
        private bool StaticDataAdded { get; set; } = false;
        private bool DynamicDataAdded { get; set; } = false;

        private IList<ItemDescription> Contents { get; } = new List<ItemDescription>();
        private IList<PageDescription> Pages { get; } = new List<PageDescription>();
        private PageDescription CoverPage { get; set; }
        private PageDescription FirstAddedPage { get; set; }

        private int ChapterCounter = 0;
        private int PageCounter = 0;

        public Metadata Metadata { get; } = new Metadata();

        public EPUBWriter(Stream stream)
        {
            BackingArchive = new ZipArchive(stream, ZipArchiveMode.Create);
        }

        public void Dispose()
        {
            FinalizeAsync().Wait();
            BackingArchive.Dispose();
        }

        public async Task AddPageAsync(Stream imageData, string navigationLabel = null)
        {
            if (!string.IsNullOrEmpty(navigationLabel) || ChapterCounter == 0)
            {
                ChapterCounter++;
                PageCounter = 1;
            }
            else
            {
                PageCounter++;
            }

            var entries = await GenerateContentsFromImageAsync(imageData, null, navigationLabel, $"C{ChapterCounter:D4}P{PageCounter:D4}");
            var page = entries.Item2;

            Pages.Add(page);
            if (FirstAddedPage == null)
            {
                FirstAddedPage = page;
            }
        }

        public async Task SetCoverAsync(Stream imageData, bool setAsFirstPage = false)
        {
            if (CoverPage != null)
            {
                throw new InvalidOperationException("Cover can only be set once");
            }

            var entries = await GenerateContentsFromImageAsync(imageData, "cover-image", null, "Cover");
            CoverPage = entries.Item2;
            if (setAsFirstPage)
            {
                CoverPage.NavigationLabel = "Cover";
                Pages.Insert(0, CoverPage);
            }
        }

        private async Task<Tuple<ItemDescription, PageDescription>> GenerateContentsFromImageAsync(Stream imageData, string imageProperties, string pageNavLabel, string fileNameBase)
        {
            if (DynamicDataAdded)
            {
                throw new InvalidOperationException("Unable to add content after container has been finalized");
            }

            await AddStaticDataAsync();

            var sourceStream = imageData;
            try
            {
                if (!imageData.CanSeek)
                {
                    sourceStream = new MemoryStream();
                    await imageData.CopyToAsync(sourceStream);
                }

                var imageInfo = await ImageDecoder.DecodeAsync(sourceStream);
                sourceStream.Position = 0;
                if (imageInfo == null)
                {
                    throw new FormatException("Image data is of invalid or not recognized format");
                }

                var imageItem = new ItemDescription($"i_{fileNameBase}", $"{fileNameBase}{imageInfo.Extension}", imageInfo.MimeType, imageProperties);
                await AddBinaryEntryAsync($"{Strings.EpubContentRoot}{imageItem.Path}", imageData);
                Contents.Add(imageItem);

                var pageItem = new PageDescription($"p_{fileNameBase}", $"{fileNameBase}.xhtml", imageInfo.Width > imageInfo.Height, pageNavLabel);
                var html = EpubXmlWriter.GenerateContentPage(imageItem.Path);
                await AddTextEntryAsync($"{Strings.EpubContentRoot}{pageItem.Path}", html);
                Contents.Add(pageItem);

                return Tuple.Create(imageItem, pageItem);
            }
            finally
            {
                if (sourceStream != imageData)
                {
                    sourceStream.Dispose();
                }
            }
        }

        public async Task FinalizeAsync()
        {
            if (DynamicDataAdded)
            {
                return;
            }

            if (FirstAddedPage == null)
            {
                //Cannot create empty book
                return;
            }

            await AddTextEntryAsync(Strings.EpubContentEntryName, EpubXmlWriter.GenerateContentOPF(Metadata, Contents, Pages));

            if (!Pages.Where(d => d.NavigationLabel != null).Any())
            {
                FirstAddedPage.NavigationLabel = Metadata.Title;
            }

            await AddTextEntryAsync(Strings.EpubNavEntryName, EpubXmlWriter.GenerateNavXML(Pages));
            DynamicDataAdded = true;
        }

        private async Task AddStaticDataAsync()
        {
            if (StaticDataAdded)
            {
                return;
            }

            await AddTextEntryAsync("mimetype", "application/epub+zip", CompressionLevel.NoCompression);
            await AddTextEntryAsync("META-INF/container.xml", Strings.EpubContainerContent);

            Contents.Add(new ItemDescription("nav", "nav.xhtml", "application/xhtml+xml", "nav"));
            var item = new ItemDescription("css", "style.css", "text/css");
            Contents.Add(item);
            await AddTextEntryAsync($"{Strings.EpubContentRoot}{item.Path}", Strings.EpubPageCSS);

            StaticDataAdded = true;
        }

        private async Task AddTextEntryAsync(string entryName, string content, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            var entry = BackingArchive.CreateEntry(entryName, compressionLevel);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(content);
            }
        }

        private async Task AddBinaryEntryAsync(string entryName, Stream content, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            var entry = BackingArchive.CreateEntry(entryName, compressionLevel);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream))
            {
                await content.CopyToAsync(stream);
            }
        }
    }
}
