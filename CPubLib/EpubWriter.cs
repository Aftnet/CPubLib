using CPubLib.Internal;
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
        private ZipArchive BackingArchive { get; }
        private bool StaticDataAdded { get; set; } = false;
        private bool DynamicDataAdded { get; set; } = false;

        private IList<ItemDescription> Contents { get; } = new List<ItemDescription>();
        private IList<PageDescription> Pages { get; } = new List<PageDescription>();
        private PageDescription CoverPage { get; set; }
        private PageDescription FirstAddedPage { get; set; }

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
            var entries = await GenerateContentsFromImageAsync(imageData, null, navigationLabel);
            var page = entries.pageDescription;

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

            var entries = await GenerateContentsFromImageAsync(imageData, "cover-image", null);
            CoverPage = entries.pageDescription;
            if (setAsFirstPage)
            {
                CoverPage.NavigationLabel = "Cover";
                Pages.Insert(0, CoverPage);
            }
        }

        private async Task<(ItemDescription imageDescription, PageDescription pageDescription)> GenerateContentsFromImageAsync(Stream imageData, string imageProperties, string pageNavLabel)
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

                var imageFormat = await Task.Run(() => SixLabors.ImageSharp.Image.DetectFormat(sourceStream));
                if (imageFormat == null)
                {
                    throw new FormatException("Image data is of invalid or not recognized format");
                }

                sourceStream.Position = 0;
                var imageInfo = await Task.Run(() => SixLabors.ImageSharp.Image.Identify(sourceStream));
                sourceStream.Position = 0;

                var uid = Guid.NewGuid().ToString();

                var imageItem = new ItemDescription($"i_{uid}", $"{uid}.{imageFormat.FileExtensions.First()}", imageFormat.MimeTypes.First(), imageProperties);
                await AddBinaryEntryAsync($"{Strings.EpubContentRoot}{imageItem.Path}", imageData);
                Contents.Add(imageItem);

                var pageItem = new PageDescription($"p_{uid}", $"{uid}.xhtml", imageInfo.Width > imageInfo.Height, pageNavLabel);
                var html = EpubXmlWriter.GenerateContentPage(imageItem.Path);
                await AddTextEntryAsync($"{Strings.EpubContentRoot}{pageItem.Path}", html);
                Contents.Add(pageItem);

                return (imageItem, pageItem);
            }
            finally
            {
                if (sourceStream != imageData)
                {
                    sourceStream.Dispose();
                }
            }
        }

        private async Task FinalizeAsync()
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
