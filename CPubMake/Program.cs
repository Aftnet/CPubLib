using CPubLib;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CPubMake
{
    [Command(Name = "cpubmake", Description = "Make fixed layout epubs from images")]
    [HelpOption("-?")]
    class Program
    {
        private static ISet<string> SupportedImageExtension { get; } = new HashSet<string> { ".jpg", ".jpeg", ".png", ".gif" };
        private const char TagsSeparator = ',';

        public static Task Main(string[] args) => CommandLineApplication.ExecuteAsync<Program>(args);

        [Option("-c|--cover", CommandOptionType.SingleValue, Description = "Path to image to include as cover")]
        [FileExists]
        public string CoverPath { get; }

        [Option("-i|--image", CommandOptionType.MultipleValue, Description = "Path to image to include in epub. Specify multiple times in the order in which images should be included")]
        [FileExists]
        public IReadOnlyList<string> InputImagePaths { get; }

        [Option("-d|--directory", CommandOptionType.MultipleValue, Description = "Path to image folder to include in epub. All files of supported format found inside (non recursively) will be included in the epub in alphabetical order")]
        [DirectoryExists]
        public IReadOnlyList<string> InputDirectoryPaths { get; }

        [Option("-o|--output", CommandOptionType.SingleValue, Description = "Path to output file")]
        [LegalFilePath]
        public string OutputPath { get; }

        [Option("--title", CommandOptionType.SingleValue)]
        public string Title { get; set; }

        [Option("--author", CommandOptionType.SingleValue)]
        public string Author { get; set; }

        [Option("--publisher", CommandOptionType.SingleValue)]
        public string Publisher { get; set; }

        [Option("--description", CommandOptionType.SingleValue)]
        public string Description { get; set; }

        [Option("--tag", CommandOptionType.MultipleValue)]
        public IReadOnlyList<string> Tags { get; set; }

        private async Task<int> OnExecuteAsync()
        {
            if (string.IsNullOrEmpty(OutputPath))
            {
                Console.WriteLine("Specify an output file");
            }

            var outputFile = new FileInfo(OutputPath);
            var targetFiles = GenerateTargetFilesList();
            try
            {
                using (var outStream = outputFile.Open(FileMode.Create))
                using (var writer = new EPUBWriter(outStream))
                {
                    var metadata = writer.Metadata;
                    metadata.Title = !string.IsNullOrEmpty(Title) ? Title : outputFile.Name;
                    metadata.Author = !string.IsNullOrEmpty(Author) ? Author : nameof(CPubMake);
                    metadata.Publisher = !string.IsNullOrEmpty(Publisher) ? Publisher : nameof(CPubMake);
                    metadata.Description = Description;
                    if (Tags != null)
                    {
                        foreach (var i in Tags.Distinct())
                        {
                            metadata.Tags.Add(i);
                        }
                    }

                    if (targetFiles.cover != null)
                    {
                        Console.WriteLine($"Adding {targetFiles.cover.Name} as cover");
                        await AddImageToEpub(writer, targetFiles.cover, true);
                    }

                    foreach (var i in targetFiles.pages)
                    {
                        Console.WriteLine($"Adding {i.Name} as page");
                        await AddImageToEpub(writer, i, false);
                    }

                    await writer.FinalizeAsync();
                }
            }
            catch
            {
                Console.WriteLine($"Error generating {outputFile.FullName}");
            }
            Console.WriteLine($"Hellolol!");
            return 0;
        }

        private void AutoGenerateMissingMetadata(FileInfo outputFile)
        {
            if (string.IsNullOrEmpty(Title))
            {
                Title = outputFile.Name;
            }

            if (string.IsNullOrEmpty(Author))
            {
                Author = nameof(CPubMake);
            }

            if (string.IsNullOrEmpty(Publisher))
            {
                Publisher = nameof(CPubMake);
            }
        }

        private (IList<FileInfo> pages, FileInfo cover) GenerateTargetFilesList()
        {
            var pages = new List<FileInfo>();
            if (InputImagePaths != null)
            {
                pages.AddRange(InputImagePaths.Select(d => new FileInfo(d)));
            }

            var directories = InputDirectoryPaths != null ? InputDirectoryPaths.Select(d => new DirectoryInfo(d)).Where(d => d.Exists) : Enumerable.Empty<DirectoryInfo>();
            foreach (var i in directories)
            {
                var files = i.EnumerateFiles();
                pages.AddRange(files.Where(d => SupportedImageExtension.Contains(d.Extension)).OrderBy(d => d.FullName));
            }
         
            var cover = default(FileInfo);
            if (!string.IsNullOrEmpty(CoverPath))
            {
                var testCover = new FileInfo(CoverPath);
                if (testCover.Exists)
                {
                    cover = testCover;
                }
            }

            //Remove cover from pages list if present
            if (cover != null)
            {
                pages = pages.Where(d => d.FullName != cover.FullName).ToList();
            }

            return (pages, cover);
        }

        private async Task AddImageToEpub(EPUBWriter writer, FileInfo imageFile, bool asCover)
        {
            if (!imageFile.Exists)
            {
                Console.WriteLine($"{imageFile.FullName} not found");
            }

            try
            {
                using (var imageStream = imageFile.OpenRead())
                {
                    if (asCover)
                    {
                        await writer.SetCoverAsync(imageStream, true);
                    }
                    else
                    {
                        await writer.AddPageAsync(imageStream);
                    }
                }
            }
            catch
            {
                Console.WriteLine($"Unable to add {imageFile.FullName} to epub");
            }
        }
    }
}
