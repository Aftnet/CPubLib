using CPubLib.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CPubLib.Test
{
    public class Tests
    {
        private const string CoverImageName = "Cover.jpg";
        private static ISet<string> ValidExtensions { get; } = new HashSet<string> { ".jpg", ".png" };
        private static IEnumerable<PageDescription> TestItems { get; } = Enumerable.Range(0, 3).Select((d, e) => new PageDescription($"ItemId{d}", $"ItemId{d}.xhtml", e % 2 == 0, $"ItemId{d}NavLabel")).ToArray();

        private ITestOutputHelper OutputHelper { get; }

        public Tests(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        [Fact]
        public async Task GenerationWorks()
        {
            var sourceFolder = new DirectoryInfo(Path.Combine("..", "..", "..", "TestImages"));
            var sourceFiles = sourceFolder.EnumerateFiles().Where(d => ValidExtensions.Contains(d.Extension)).ToArray();

            var testFile = new FileInfo(@"Test.epub");
            using (var stream = testFile.Open(FileMode.Create))
            using (var writer = new EPUBWriter(stream))
            {
                foreach (var i in sourceFiles)
                {
                    using (var pageStream = i.OpenRead())
                    {
                        if(i.Name == CoverImageName)
                        {
                            await writer.SetCoverAsync(pageStream, true);
                        }
                        else
                        {
                            await writer.AddPageAsync(pageStream, i.Name);
                        }
                    }
                }

                SetMetadata(writer.Metadata);
            }
        }

        [Fact]
        public void ContentPageGenerationWorks()
        {
            var xml = EpubXmlWriter.GenerateContentPage("imagepath.jpg");
            Assert.NotEmpty(xml);
            OutputHelper.WriteLine(xml);
        }

        [Fact]
        public void ContentGenerationWorks()
        {
            var metadata = new Metadata();
            SetMetadata(metadata);

            var xml = EpubXmlWriter.GenerateContentOPF(metadata, TestItems, TestItems);
            Assert.NotEmpty(xml);
            OutputHelper.WriteLine(xml);
        }

        [Fact]
        public void NavGenerationWorks()
        {
            var xml = EpubXmlWriter.GenerateNavXML(TestItems);
            Assert.NotEmpty(xml);
            OutputHelper.WriteLine(xml);
        }

        private void SetMetadata(Metadata metadata)
        {
            metadata.Title = "Test book";

            metadata.Author = "John Smith";
            metadata.Publisher = "Roslyin inc";
            metadata.PublishingDate = new DateTime(1208, 12, 23, 0, 0, 0);
            metadata.Description = "Some description here";
            metadata.Source = "Source here";
            metadata.Relation = "Relation here";
            metadata.Copyright = "Copyright here";
            metadata.Tags.Add("Alpha");
            metadata.Tags.Add("Beta");
            metadata.Tags.Add("Gamma");
        }
    }
}
